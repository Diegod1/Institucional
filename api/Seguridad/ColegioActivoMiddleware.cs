using System.Security.Claims;
using Colegio.Api.Datos;
using Microsoft.EntityFrameworkCore;

namespace Colegio.Api.Seguridad;

public class ColegioActivoMiddleware(RequestDelegate siguiente)
{
    public async Task Invoke(HttpContext contexto, ColegioDbContext db)
    {
        if (contexto.User.Identity?.IsAuthenticated == true
            && int.TryParse(contexto.User.FindFirstValue("colegioId"), out var colegioId))
        {
            var activo = await db.Colegios
                .Where(colegio => colegio.Id == colegioId)
                .Select(colegio => (bool?)colegio.Activo)
                .FirstOrDefaultAsync();

            if (activo == false)
            {
                contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                await contexto.Response.WriteAsJsonAsync(new { mensaje = "Este colegio está inactivo." });
                return;
            }
        }

        await siguiente(contexto);
    }
}
