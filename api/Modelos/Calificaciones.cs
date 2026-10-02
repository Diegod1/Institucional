namespace Colegio.Api.Modelos;

public class Actividad
{
    public int Id { get; set; }
    public int AsignacionId { get; set; }
    public Asignacion Asignacion { get; set; } = null!;
    public int PeriodoId { get; set; }
    public Periodo Periodo { get; set; } = null!;
    public string Nombre { get; set; } = "";
    public decimal Porcentaje { get; set; }

    public List<NotaActividad> Notas { get; set; } = [];
}

public class NotaActividad
{
    public int Id { get; set; }
    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;
    public int EstudianteId { get; set; }
    public Usuario Estudiante { get; set; } = null!;
    public decimal Valor { get; set; }
}

public class Nivelacion
{
    public int Id { get; set; }
    public int AsignacionId { get; set; }
    public Asignacion Asignacion { get; set; } = null!;
    public int PeriodoId { get; set; }
    public Periodo Periodo { get; set; } = null!;
    public int EstudianteId { get; set; }
    public Usuario Estudiante { get; set; } = null!;
    public decimal? NotaAnterior { get; set; }
    public decimal NotaNueva { get; set; }
    public DateTime Fecha { get; set; }
}

public class ComentarioMateria
{
    public int Id { get; set; }
    public int AsignacionId { get; set; }
    public Asignacion Asignacion { get; set; } = null!;
    public int PeriodoId { get; set; }
    public Periodo Periodo { get; set; } = null!;
    public int EstudianteId { get; set; }
    public Usuario Estudiante { get; set; } = null!;
    public string Texto { get; set; } = "";
}

public class ComentarioOrientador
{
    public int Id { get; set; }
    public int GrupoId { get; set; }
    public Grupo Grupo { get; set; } = null!;
    public int PeriodoId { get; set; }
    public Periodo Periodo { get; set; } = null!;
    public int EstudianteId { get; set; }
    public Usuario Estudiante { get; set; } = null!;
    public string Texto { get; set; } = "";
}
