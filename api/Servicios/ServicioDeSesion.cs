using Colegio.Api.Datos;
using Colegio.Api.Modelos;
using Colegio.Api.Reglas;
using Colegio.Api.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace Colegio.Api.Servicios;

public record Entrar(string NombreUsuario, string Contrasena);

public record SesionIniciada(string Token, string Nombre, string NombreUsuario, Rol Rol, int? ColegioId, string? NombreColegio);

public class ServicioDeSesion(ColegioDbContext db, Contrasenas contrasenas, TokenDeAcceso tokens, SesionActual sesion)
{
    public async Task<SesionIniciada> Entrar(Entrar datos)
    {
        var usuario = Textos.Usuario(datos.NombreUsuario);
        var persona = await db.Usuarios.Include(item => item.Colegio).FirstOrDefaultAsync(item => item.NombreUsuario == usuario);
        if (persona is null || !contrasenas.Coincide(persona, datos.Contrasena ?? ""))
            throw new ReglaDeNegocioException("Usuario o contraseña incorrectos.");

        if (persona.Colegio is { Activo: false })
            throw new ReglaDeNegocioException("Este colegio está inactivo.");

        return Vista(persona, tokens.Crear(persona));
    }

    public async Task<SesionIniciada> Actual()
    {
        var persona = await db.Usuarios.Include(item => item.Colegio).FirstAsync(item => item.Id == sesion.UsuarioId);
        var token = tokens.Crear(persona);
        return Vista(persona, token);
    }

    private static SesionIniciada Vista(Usuario persona, string token) =>
        new(token, persona.Nombre, persona.NombreUsuario, persona.Rol, persona.ColegioId, persona.Colegio?.Nombre);
}
