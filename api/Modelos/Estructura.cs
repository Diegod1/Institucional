namespace Colegio.Api.Modelos;

public class AnioLectivo
{
    public int Id { get; set; }
    public int ColegioId { get; set; }
    public Colegio Colegio { get; set; } = null!;
    public int Anio { get; set; }

    public List<Periodo> Periodos { get; set; } = [];
    public List<Grupo> Grupos { get; set; } = [];
}

public class Periodo
{
    public int Id { get; set; }
    public int AnioLectivoId { get; set; }
    public AnioLectivo AnioLectivo { get; set; } = null!;
    public int Numero { get; set; }
    public string Nombre { get; set; } = "";
    public decimal Peso { get; set; }
    public bool Cerrado { get; set; }
}

public class Grado
{
    public int Id { get; set; }
    public int ColegioId { get; set; }
    public Colegio Colegio { get; set; } = null!;
    public string Nombre { get; set; } = "";
}

public class Materia
{
    public int Id { get; set; }
    public int ColegioId { get; set; }
    public Colegio Colegio { get; set; } = null!;
    public string Nombre { get; set; } = "";
}

public class Grupo
{
    public int Id { get; set; }
    public int AnioLectivoId { get; set; }
    public AnioLectivo AnioLectivo { get; set; } = null!;
    public int GradoId { get; set; }
    public Grado Grado { get; set; } = null!;
    public string Nombre { get; set; } = "";
    public int? OrientadorId { get; set; }
    public Usuario? Orientador { get; set; }

    public List<Matricula> Matriculas { get; set; } = [];
    public List<Asignacion> Asignaciones { get; set; } = [];
}

public class Matricula
{
    public int Id { get; set; }
    public int GrupoId { get; set; }
    public Grupo Grupo { get; set; } = null!;
    public int EstudianteId { get; set; }
    public Usuario Estudiante { get; set; } = null!;
}

public class Asignacion
{
    public int Id { get; set; }
    public int GrupoId { get; set; }
    public Grupo Grupo { get; set; } = null!;
    public int MateriaId { get; set; }
    public Materia Materia { get; set; } = null!;
    public int ProfesorId { get; set; }
    public Usuario Profesor { get; set; } = null!;
}
