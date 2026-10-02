using Colegio.Api.Modelos;
using Colegio.Api.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace Colegio.Api.Datos;

public static class DatosIniciales
{
    public static async Task Cargar(IServiceProvider servicios)
    {
        using var alcance = servicios.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<ColegioDbContext>();
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "Colegios" ADD COLUMN IF NOT EXISTS "Activo" boolean NOT NULL DEFAULT TRUE;""");

        if (await db.Usuarios.AnyAsync(usuario => usuario.Rol == Rol.AdministradorPlataforma))
            return;

        var configuracion = alcance.ServiceProvider.GetRequiredService<IConfiguration>();
        var contrasenas = alcance.ServiceProvider.GetRequiredService<Contrasenas>();
        var admin = new Usuario
        {
            Nombre = configuracion["AdminInicial:Nombre"] ?? "Administrador",
            NombreUsuario = (configuracion["AdminInicial:Usuario"] ?? "admin").Trim().ToLowerInvariant(),
            Rol = Rol.AdministradorPlataforma
        };
        admin.ContrasenaHash = contrasenas.CrearHash(admin, configuracion["AdminInicial:Contrasena"] ?? "admin123");
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
    }
}
