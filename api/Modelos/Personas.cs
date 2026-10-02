namespace Colegio.Api.Modelos;

public enum Rol
{
    AdministradorPlataforma,
    Colegio,
    Profesor,
    Estudiante
}

public class Colegio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal NotaMinimaAprobacion { get; set; } = 3.00m;
    public bool MostrarDistanciaAlPromedio { get; set; } = true;
    public bool MostrarDistanciaALaNotaMasAlta { get; set; } = true;
    public bool Activo { get; set; } = true;

    public List<RangoDesempeno> Rangos { get; set; } = [];
    public List<Usuario> Usuarios { get; set; } = [];
}

public class RangoDesempeno
{
    public int Id { get; set; }
    public int ColegioId { get; set; }
    public Colegio Colegio { get; set; } = null!;
    public string Nombre { get; set; } = "";
    public decimal Desde { get; set; }
    public decimal Hasta { get; set; }
}

public class Usuario
{
    public int Id { get; set; }
    public int? ColegioId { get; set; }
    public Colegio? Colegio { get; set; }
    public string Nombre { get; set; } = "";
    public string NombreUsuario { get; set; } = "";
    public string ContrasenaHash { get; set; } = "";
    public Rol Rol { get; set; }
}
