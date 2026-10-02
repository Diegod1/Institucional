using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Colegio.Api.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Colegio.Api.Seguridad;

public class Contrasenas
{
    private readonly PasswordHasher<Usuario> hasher = new();

    public string CrearHash(Usuario usuario, string contrasena) =>
        hasher.HashPassword(usuario, contrasena);

    public bool Coincide(Usuario usuario, string contrasena) =>
        hasher.VerifyHashedPassword(usuario, usuario.ContrasenaHash, contrasena)
            is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}

public class TokenDeAcceso(IConfiguration configuracion)
{
    public string Crear(Usuario usuario)
    {
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracion["Jwt:Clave"]!));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Role, usuario.Rol.ToString()),
            new("usuario", usuario.NombreUsuario)
        };

        if (usuario.ColegioId is int colegioId)
            claims.Add(new Claim("colegioId", colegioId.ToString()));

        var token = new JwtSecurityToken(
            issuer: configuracion["Jwt:Emisor"],
            audience: configuracion["Jwt:Audiencia"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class SesionActual(IHttpContextAccessor accesoHttp)
{
    public int UsuarioId => int.Parse(Claim(ClaimTypes.NameIdentifier));
    public string Nombre => Claim(ClaimTypes.Name);
    public Rol Rol => Enum.Parse<Rol>(Claim(ClaimTypes.Role));

    public int ColegioId =>
        int.TryParse(accesoHttp.HttpContext?.User.FindFirstValue("colegioId"), out var colegioId)
            ? colegioId
            : throw new Reglas.ReglaDeNegocioException("Esta cuenta no pertenece a un colegio.");

    private string Claim(string tipo) =>
        accesoHttp.HttpContext?.User.FindFirstValue(tipo)
        ?? throw new Reglas.ReglaDeNegocioException("La sesión no es válida.");
}
