using Colegio.Api.Datos;
using Colegio.Api.Modelos;
using Colegio.Api.Reglas;
using Colegio.Api.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace Colegio.Api.Servicios;

public record CrearColegio(string Nombre, string NombreResponsable, string NombreUsuario, string Contrasena);
public record ColegioCreado(int Id, string Nombre, string NombreUsuario, bool Activo);
public record CambiarActivo(bool Activo);

public record GuardarConfiguracion(
    decimal NotaMinimaAprobacion,
    bool MostrarDistanciaAlPromedio,
    bool MostrarDistanciaALaNotaMasAlta,
    List<RangoRecibido> Rangos);

public record RangoRecibido(string Nombre, decimal Desde, decimal Hasta);

public record CrearAnio(int Anio, int CantidadPeriodos);
public record PesoDePeriodo(int PeriodoId, decimal Peso);
public record CrearNombrado(string Nombre);
public record CrearPersona(string Nombre, string NombreUsuario, string Contrasena);
public record ActualizarPersona(string Nombre, string? Contrasena);
public record CrearGrupo(int AnioLectivoId, int GradoId, string Nombre, int OrientadorId);
public record Matricular(int EstudianteId);
public record CrearAsignacion(int MateriaId, int ProfesorId);

public class ServicioDePlataforma(ColegioDbContext db, Contrasenas contrasenas)
{
    public async Task<List<ColegioCreado>> Listar()
    {
        var colegios = await db.Colegios
            .OrderBy(colegio => colegio.Nombre)
            .Select(colegio => new
            {
                colegio.Id,
                colegio.Nombre,
                colegio.Activo,
                Usuario = colegio.Usuarios
                    .Where(usuario => usuario.Rol == Rol.Colegio)
                    .Select(usuario => usuario.NombreUsuario)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return colegios
            .Select(colegio => new ColegioCreado(colegio.Id, colegio.Nombre, colegio.Usuario ?? "", colegio.Activo))
            .ToList();
    }

    public async Task<ColegioCreado> Crear(CrearColegio datos)
    {
        var nombre = Textos.Exigir(datos.Nombre, "Escribe el nombre del colegio.");
        var responsable = Textos.Exigir(datos.NombreResponsable, "Escribe el nombre de quien administra el colegio.");
        var usuario = Textos.Usuario(datos.NombreUsuario);
        Textos.ExigirContrasena(datos.Contrasena);
        await ExigirUsuarioLibre(usuario);

        var colegio = new Modelos.Colegio { Nombre = nombre };
        colegio.Rangos = RangosIniciales();
        db.Colegios.Add(colegio);

        var acceso = new Usuario
        {
            Colegio = colegio,
            Nombre = responsable,
            NombreUsuario = usuario,
            Rol = Rol.Colegio
        };
        acceso.ContrasenaHash = contrasenas.CrearHash(acceso, datos.Contrasena.Trim());
        db.Usuarios.Add(acceso);
        await db.SaveChangesAsync();

        return new ColegioCreado(colegio.Id, colegio.Nombre, acceso.NombreUsuario, colegio.Activo);
    }

    public static List<RangoDesempeno> RangosIniciales() =>
    [
        new() { Nombre = "Bajo", Desde = 0m, Hasta = 2.99m },
        new() { Nombre = "Básico", Desde = 3.00m, Hasta = 3.99m },
        new() { Nombre = "Alto", Desde = 4.00m, Hasta = 4.59m },
        new() { Nombre = "Superior", Desde = 4.60m, Hasta = 5.00m }
    ];

    public async Task<ColegioCreado> CambiarActivo(int id, bool activo)
    {
        var colegio = await db.Colegios.Include(item => item.Usuarios).FirstOrDefaultAsync(item => item.Id == id)
            ?? throw new NoEncontradoException("No existe ese colegio.");

        colegio.Activo = activo;
        await db.SaveChangesAsync();

        var usuario = colegio.Usuarios.FirstOrDefault(item => item.Rol == Rol.Colegio)?.NombreUsuario ?? "";
        return new ColegioCreado(colegio.Id, colegio.Nombre, usuario, colegio.Activo);
    }

    private async Task ExigirUsuarioLibre(string usuario)
    {
        if (await db.Usuarios.AnyAsync(item => item.NombreUsuario == usuario))
            throw new ReglaDeNegocioException("Ese nombre de usuario ya existe.");
    }
}

public class ServicioDeConfiguracion(ColegioDbContext db, SesionActual sesion)
{
    public async Task<GuardarConfiguracion> Leer()
    {
        var colegio = await ColegioActual();
        return new GuardarConfiguracion(
            colegio.NotaMinimaAprobacion,
            colegio.MostrarDistanciaAlPromedio,
            colegio.MostrarDistanciaALaNotaMasAlta,
            colegio.Rangos
                .OrderBy(rango => rango.Desde)
                .Select(rango => new RangoRecibido(rango.Nombre, rango.Desde, rango.Hasta))
                .ToList());
    }

    public async Task Guardar(GuardarConfiguracion datos)
    {
        CalculadoraDeNotas.ExigirNota(datos.NotaMinimaAprobacion);
        var rangos = NormalizarRangos(datos.Rangos);
        var colegio = await ColegioActual();
        colegio.NotaMinimaAprobacion = datos.NotaMinimaAprobacion;
        colegio.MostrarDistanciaAlPromedio = datos.MostrarDistanciaAlPromedio;
        colegio.MostrarDistanciaALaNotaMasAlta = datos.MostrarDistanciaALaNotaMasAlta;

        foreach (var rango in rangos)
        {
            var actual = colegio.Rangos.First(item => item.Nombre == rango.Nombre);
            actual.Desde = rango.Desde;
            actual.Hasta = rango.Hasta;
        }

        await db.SaveChangesAsync();
    }

    public static List<RangoRecibido> NormalizarRangos(IReadOnlyList<RangoRecibido> recibidos)
    {
        if (recibidos.Count != 4)
            throw new ReglaDeNegocioException("Define los cuatro desempeños: Bajo, Básico, Alto y Superior.");

        var rangos = recibidos.Select(rango =>
        {
            CalculadoraDeNotas.ExigirNota(rango.Desde);
            CalculadoraDeNotas.ExigirNota(rango.Hasta);
            if (rango.Desde > rango.Hasta)
                throw new ReglaDeNegocioException($"En {rango.Nombre}, el valor inicial no puede ser mayor que el final.");

            return new RangoRecibido(NombreCanonico(rango.Nombre), rango.Desde, rango.Hasta);
        }).ToList();

        if (rangos.Select(rango => rango.Nombre).Distinct().Count() != 4)
            throw new ReglaDeNegocioException("Cada desempeño (Bajo, Básico, Alto y Superior) se define una sola vez.");

        var ordenados = rangos.OrderBy(rango => rango.Desde).ToList();
        for (var i = 1; i < ordenados.Count; i++)
        {
            if (ordenados[i].Desde <= ordenados[i - 1].Hasta)
                throw new ReglaDeNegocioException("Los rangos de desempeño no se pueden cruzar.");
        }

        return ordenados;
    }

    public static string NombreCanonico(string nombre) =>
        nombre.Trim().ToLowerInvariant() switch
        {
            "bajo" => "Bajo",
            "basico" or "básico" => "Básico",
            "alto" => "Alto",
            "superior" => "Superior",
            _ => throw new ReglaDeNegocioException("Los desempeños son Bajo, Básico, Alto y Superior.")
        };

    private async Task<Modelos.Colegio> ColegioActual() =>
        await db.Colegios.Include(colegio => colegio.Rangos).FirstAsync(colegio => colegio.Id == sesion.ColegioId);
}

public static class Textos
{
    public static string Exigir(string? valor, string mensaje)
    {
        var limpio = valor?.Trim() ?? "";
        if (limpio.Length == 0)
            throw new ReglaDeNegocioException(mensaje);
        return limpio;
    }

    public static string Usuario(string? valor)
    {
        var limpio = Exigir(valor, "Escribe un nombre de usuario.").ToLowerInvariant();
        if (limpio.Any(char.IsWhiteSpace))
            throw new ReglaDeNegocioException("El nombre de usuario no puede tener espacios.");
        return limpio;
    }

    public static void ExigirContrasena(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Trim().Length < 4)
            throw new ReglaDeNegocioException("La contraseña debe tener al menos 4 caracteres.");
    }
}
