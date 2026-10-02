using Colegio.Api.Datos;
using Colegio.Api.Modelos;
using Colegio.Api.Notas;
using Colegio.Api.Reglas;
using Colegio.Api.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace Colegio.Api.Servicios;

public record MateriaDelPeriodo(
    string Materia,
    decimal? Nota,
    string? Desempeno,
    bool? Aprueba,
    string Comentario,
    decimal? PromedioDelGrupo,
    decimal? DistanciaAlPromedio,
    decimal? NotaMasAlta,
    decimal? DistanciaALaNotaMasAlta,
    IReadOnlyList<NotaPuesta> Actividades,
    IReadOnlyList<NivelacionHecha> Nivelaciones);

public record BoletinPeriodo(
    int PeriodoId,
    string NombrePeriodo,
    int Anio,
    string Grupo,
    string Grado,
    string Colegio,
    string Estudiante,
    bool MostrarDistanciaAlPromedio,
    bool MostrarDistanciaALaNotaMasAlta,
    string ComentarioOrientador,
    IReadOnlyList<MateriaDelPeriodo> Materias);

public record MateriaDelAnio(
    string Materia,
    decimal? Nota,
    string? Desempeno,
    bool? Aprueba,
    decimal? PromedioDelGrupo,
    decimal? DistanciaAlPromedio,
    decimal? NotaMasAlta,
    decimal? DistanciaALaNotaMasAlta);

public record PeriodoDelAnio(int Id, string Nombre, bool Cerrado, string ComentarioOrientador, IReadOnlyList<MateriaDelPeriodo> Materias);

public record BoletinAnio(
    int AnioLectivoId,
    int Anio,
    string Grupo,
    string Grado,
    string Colegio,
    string Estudiante,
    bool AnioCerrado,
    bool MostrarDistanciaAlPromedio,
    bool MostrarDistanciaALaNotaMasAlta,
    IReadOnlyList<PeriodoDelAnio> Periodos,
    IReadOnlyList<MateriaDelAnio> Materias);

public record AnioDelEstudiante(int Id, int Anio, string Grupo, List<PeriodoVista> Periodos);

public class ServicioDeBoletin(ColegioDbContext db, SesionActual sesion, LectorDeNotas lector, IConfiguration configuracion)
{
    public async Task<List<AnioDelEstudiante>> MisAnios()
    {
        return await db.Matriculas
            .Where(matricula => matricula.EstudianteId == sesion.UsuarioId)
            .OrderByDescending(matricula => matricula.Grupo.AnioLectivo.Anio)
            .Select(matricula => new AnioDelEstudiante(
                matricula.Grupo.AnioLectivo.Id,
                matricula.Grupo.AnioLectivo.Anio,
                matricula.Grupo.Nombre,
                matricula.Grupo.AnioLectivo.Periodos
                    .OrderBy(periodo => periodo.Numero)
                    .Select(periodo => new PeriodoVista(periodo.Id, periodo.Numero, periodo.Nombre, periodo.Peso, periodo.Cerrado))
                    .ToList()))
            .ToListAsync();
    }

    public async Task<BoletinPeriodo> Periodo(int periodoId) =>
        await ArmarPeriodo(sesion.UsuarioId, periodoId);

    public async Task<BoletinAnio> Anio(int anioId) =>
        await ArmarAnio(sesion.UsuarioId, anioId);

    public async Task<byte[]> PdfDelPeriodo(int periodoId)
    {
        var boletin = await ArmarPeriodo(sesion.UsuarioId, periodoId);
        var periodo = await db.Periodos.FirstAsync(item => item.Id == periodoId);
        return await Archivo(RutaPeriodo(sesion.UsuarioId, periodoId), periodo.Cerrado, () => GeneradorDeBoletin.Periodo(boletin));
    }

    public async Task<byte[]> PdfDelAnio(int anioId)
    {
        var boletin = await ArmarAnio(sesion.UsuarioId, anioId);
        return await Archivo(RutaAnio(sesion.UsuarioId, anioId), boletin.AnioCerrado, () => GeneradorDeBoletin.Anio(boletin));
    }

    private async Task<BoletinPeriodo> ArmarPeriodo(int estudianteId, int periodoId)
    {
        var contexto = await Contexto(estudianteId, periodoId: periodoId, anioId: null);
        var materias = new List<MateriaDelPeriodo>();
        foreach (var asignacion in contexto.Asignaciones)
        {
            var planilla = await lector.Leer(asignacion.Id, periodoId);
            materias.Add(Materia(planilla, estudianteId, contexto));
        }

        return new BoletinPeriodo(
            periodoId,
            contexto.Periodo!.Nombre,
            contexto.Anio.Anio,
            contexto.Grupo.Nombre,
            contexto.Grupo.Grado.Nombre,
            contexto.Colegio.Nombre,
            contexto.Estudiante.Nombre,
            contexto.Colegio.MostrarDistanciaAlPromedio,
            contexto.Colegio.MostrarDistanciaALaNotaMasAlta,
            await ComentarioOrientador(contexto.Grupo.Id, periodoId, estudianteId),
            materias);
    }

    private async Task<BoletinAnio> ArmarAnio(int estudianteId, int anioId)
    {
        var contexto = await Contexto(estudianteId, periodoId: null, anioId: anioId);
        var periodos = new List<PeriodoDelAnio>();
        var planillasPorAsignacion = contexto.Asignaciones.ToDictionary(asignacion => asignacion.Id, _ => new List<Planilla>());

        foreach (var periodo in contexto.Anio.Periodos.OrderBy(item => item.Numero))
        {
            var materias = new List<MateriaDelPeriodo>();
            foreach (var asignacion in contexto.Asignaciones)
            {
                var planilla = await lector.Leer(asignacion.Id, periodo.Id);
                planillasPorAsignacion[asignacion.Id].Add(planilla);
                materias.Add(Materia(planilla, estudianteId, contexto));
            }

            periodos.Add(new PeriodoDelAnio(
                periodo.Id,
                periodo.Nombre,
                periodo.Cerrado,
                await ComentarioOrientador(contexto.Grupo.Id, periodo.Id, estudianteId),
                materias));
        }

        var materiasDelAnio = new List<MateriaDelAnio>();
        foreach (var asignacion in contexto.Asignaciones)
        {
            var planillas = planillasPorAsignacion[asignacion.Id];
            var fila = NotaDelAnio(planillas, estudianteId, contexto.Anio.Periodos);
            var delGrupo = contexto.Grupo.Matriculas
                .Select(matricula => NotaDelAnio(planillas, matricula.EstudianteId, contexto.Anio.Periodos))
                .Where(nota => nota is not null)
                .Select(nota => nota!.Value)
                .ToList();

            var (promedio, masAlta) = Resumen(delGrupo);
            materiasDelAnio.Add(new MateriaDelAnio(
                asignacion.Materia.Nombre,
                fila,
                fila is decimal nota ? CalculadoraDeNotas.Desempeno(nota, contexto.Rangos) : null,
                fila is decimal valor ? CalculadoraDeNotas.Aprueba(valor, contexto.Colegio.NotaMinimaAprobacion) : null,
                Distancia(contexto.Colegio.MostrarDistanciaAlPromedio, promedio),
                Distancia(contexto.Colegio.MostrarDistanciaAlPromedio, fila, promedio),
                Distancia(contexto.Colegio.MostrarDistanciaALaNotaMasAlta, masAlta),
                Distancia(contexto.Colegio.MostrarDistanciaALaNotaMasAlta, masAlta, fila)));
        }

        return new BoletinAnio(
            contexto.Anio.Id,
            contexto.Anio.Anio,
            contexto.Grupo.Nombre,
            contexto.Grupo.Grado.Nombre,
            contexto.Colegio.Nombre,
            contexto.Estudiante.Nombre,
            contexto.Anio.Periodos.All(periodo => periodo.Cerrado),
            contexto.Colegio.MostrarDistanciaAlPromedio,
            contexto.Colegio.MostrarDistanciaALaNotaMasAlta,
            periodos,
            materiasDelAnio);
    }

    private MateriaDelPeriodo Materia(Planilla planilla, int estudianteId, ContextoBoletin contexto)
    {
        var fila = planilla.Estudiantes.First(item => item.EstudianteId == estudianteId);
        var notasDelGrupo = planilla.Estudiantes.Select(item => item.NotaDelPeriodo).Where(nota => nota is not null).Select(nota => nota!.Value).ToList();
        var (promedio, masAlta) = Resumen(notasDelGrupo);
        return new MateriaDelPeriodo(
            planilla.Materia,
            fila.NotaDelPeriodo,
            fila.NotaDelPeriodo is decimal nota ? CalculadoraDeNotas.Desempeno(nota, contexto.Rangos) : null,
            fila.NotaDelPeriodo is decimal valor ? CalculadoraDeNotas.Aprueba(valor, contexto.Colegio.NotaMinimaAprobacion) : null,
            fila.Comentario,
            Distancia(contexto.Colegio.MostrarDistanciaAlPromedio, promedio),
            Distancia(contexto.Colegio.MostrarDistanciaAlPromedio, fila.NotaDelPeriodo, promedio),
            Distancia(contexto.Colegio.MostrarDistanciaALaNotaMasAlta, masAlta),
            Distancia(contexto.Colegio.MostrarDistanciaALaNotaMasAlta, masAlta, fila.NotaDelPeriodo),
            fila.Notas,
            fila.Nivelaciones);
    }

    private static decimal? NotaDelAnio(IReadOnlyList<Planilla> planillas, int estudianteId, IEnumerable<Periodo> periodos)
    {
        var pares = new List<(decimal nota, decimal peso)>();
        foreach (var periodo in periodos)
        {
            var planilla = planillas.First(item => item.PeriodoId == periodo.Id);
            var nota = planilla.Estudiantes.First(item => item.EstudianteId == estudianteId).NotaDelPeriodo;
            if (nota is null)
                return null;
            pares.Add((nota.Value, periodo.Peso));
        }

        return CalculadoraDeNotas.NotaDelAnio(pares);
    }

    private static (decimal? promedio, decimal? masAlta) Resumen(List<decimal> notas)
    {
        if (notas.Count == 0)
            return (null, null);
        return (CalculadoraDeNotas.Redondear(notas.Average()), notas.Max());
    }

    private static decimal? Distancia(bool mostrar, decimal? valor) => mostrar ? valor : null;

    private static decimal? Distancia(bool mostrar, decimal? izquierda, decimal? derecha)
    {
        if (!mostrar || izquierda is null || derecha is null)
            return null;

        return CalculadoraDeNotas.Redondear(izquierda.Value - derecha.Value);
    }

    private async Task<string> ComentarioOrientador(int grupoId, int periodoId, int estudianteId) =>
        await db.ComentariosOrientador
            .Where(item => item.GrupoId == grupoId && item.PeriodoId == periodoId && item.EstudianteId == estudianteId)
            .Select(item => item.Texto)
            .FirstOrDefaultAsync() ?? "";

    private async Task<ContextoBoletin> Contexto(int estudianteId, int? periodoId, int? anioId)
    {
        var periodo = periodoId is int idPeriodo
            ? await db.Periodos.Include(item => item.AnioLectivo).FirstOrDefaultAsync(item => item.Id == idPeriodo)
                ?? throw new NoEncontradoException("No existe ese periodo.")
            : null;

        var anioBuscado = periodo?.AnioLectivoId ?? anioId
            ?? throw new ReglaDeNegocioException("Indica el año o el periodo.");

        var matricula = await db.Matriculas
            .Include(item => item.Estudiante)
            .Include(item => item.Grupo).ThenInclude(grupo => grupo.Grado)
            .Include(item => item.Grupo).ThenInclude(grupo => grupo.Matriculas)
            .Include(item => item.Grupo).ThenInclude(grupo => grupo.AnioLectivo).ThenInclude(anio => anio.Periodos)
            .FirstOrDefaultAsync(item => item.EstudianteId == estudianteId && item.Grupo.AnioLectivoId == anioBuscado)
            ?? throw new NoEncontradoException("No estás matriculado en ese año.");

        if (matricula.Grupo.AnioLectivo.ColegioId != sesion.ColegioId || estudianteId != sesion.UsuarioId)
            throw new NoEncontradoException("No se encontró el boletín.");

        var colegio = await db.Colegios.Include(item => item.Rangos).FirstAsync(item => item.Id == sesion.ColegioId);
        var asignaciones = await db.Asignaciones.Include(item => item.Materia)
            .Where(item => item.GrupoId == matricula.GrupoId)
            .OrderBy(item => item.Materia.Nombre)
            .ToListAsync();

        return new ContextoBoletin(
            colegio,
            matricula.Estudiante,
            matricula.Grupo,
            matricula.Grupo.AnioLectivo,
            periodo,
            asignaciones,
            colegio.Rangos.Select(rango => (rango.Nombre, rango.Desde, rango.Hasta)).ToList());
    }

    private async Task<byte[]> Archivo(string ruta, bool definitivo, Func<byte[]> generar)
    {
        if (definitivo && File.Exists(ruta))
            return await File.ReadAllBytesAsync(ruta);

        var pdf = generar();
        if (!definitivo)
            return pdf;

        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        await File.WriteAllBytesAsync(ruta, pdf);
        return pdf;
    }

    private string RutaPeriodo(int estudianteId, int periodoId) =>
        Path.Combine(Carpeta, $"estudiante-{estudianteId}-periodo-{periodoId}.pdf");

    private string RutaAnio(int estudianteId, int anioId) =>
        Path.Combine(Carpeta, $"estudiante-{estudianteId}-anio-{anioId}.pdf");

    private string Carpeta => configuracion["RutaBoletines"] ?? "boletines";

    private sealed record ContextoBoletin(
        Modelos.Colegio Colegio,
        Usuario Estudiante,
        Grupo Grupo,
        AnioLectivo Anio,
        Periodo? Periodo,
        List<Asignacion> Asignaciones,
        List<(string Nombre, decimal Desde, decimal Hasta)> Rangos);
}
