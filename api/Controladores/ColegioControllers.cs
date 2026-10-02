using Colegio.Api.Modelos;
using Colegio.Api.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colegio.Api.Controladores;

[ApiController]
[Route("api/sesion")]
public class SesionController(ServicioDeSesion sesion) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<SesionIniciada>> Entrar(Entrar datos) =>
        Ok(await sesion.Entrar(datos));

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<SesionIniciada>> Actual() =>
        Ok(await sesion.Actual());
}

[ApiController]
[Authorize(Roles = nameof(Rol.AdministradorPlataforma))]
[Route("api/colegios")]
public class PlataformaController(ServicioDePlataforma plataforma) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ColegioCreado>>> Listar() => Ok(await plataforma.Listar());

    [HttpPost]
    public async Task<ActionResult<ColegioCreado>> Crear(CrearColegio datos) =>
        Ok(await plataforma.Crear(datos));

    [HttpPut("{id:int}/activo")]
    public async Task<ActionResult<ColegioCreado>> CambiarActivo(int id, CambiarActivo datos) =>
        Ok(await plataforma.CambiarActivo(id, datos.Activo));
}

[ApiController]
[Authorize(Roles = nameof(Rol.Colegio))]
[Route("api/configuracion")]
public class ConfiguracionController(ServicioDeConfiguracion configuracion) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GuardarConfiguracion>> Leer() => Ok(await configuracion.Leer());

    [HttpPut]
    public async Task<IActionResult> Guardar(GuardarConfiguracion datos)
    {
        await configuracion.Guardar(datos);
        return NoContent();
    }
}

[ApiController]
[Authorize(Roles = nameof(Rol.Colegio))]
[Route("api")]
public class EstructuraController(ServicioAcademico academico) : ControllerBase
{
    [HttpGet("anios")]
    public async Task<List<AnioVista>> Anios() => await academico.ListarAnios();

    [HttpPost("anios")]
    public async Task<AnioVista> CrearAnio(CrearAnio datos) => await academico.CrearAnio(datos);

    [HttpPut("anios/{anioId:int}/pesos")]
    public async Task<IActionResult> Pesos(int anioId, List<PesoDePeriodo> pesos)
    {
        await academico.GuardarPesos(anioId, pesos);
        return NoContent();
    }

    [HttpPost("periodos/{periodoId:int}/cerrar")]
    public async Task<IActionResult> Cerrar(int periodoId)
    {
        await academico.CerrarPeriodo(periodoId);
        return NoContent();
    }

    [HttpGet("grados")]
    public async Task<List<NombradoVista>> Grados() => await academico.ListarGrados();

    [HttpPost("grados")]
    public async Task<NombradoVista> CrearGrado(CrearNombrado datos) => await academico.CrearGrado(datos.Nombre);

    [HttpGet("materias")]
    public async Task<List<NombradoVista>> Materias() => await academico.ListarMaterias();

    [HttpPost("materias")]
    public async Task<NombradoVista> CrearMateria(CrearNombrado datos) => await academico.CrearMateria(datos.Nombre);

    [HttpGet("profesores")]
    public async Task<List<PersonaVista>> Profesores() => await academico.ListarProfesores();

    [HttpPost("profesores")]
    public async Task<PersonaVista> CrearProfesor(CrearPersona datos) => await academico.CrearProfesor(datos);

    [HttpPut("profesores/{id:int}")]
    public async Task<IActionResult> ActualizarProfesor(int id, ActualizarPersona datos)
    {
        await academico.ActualizarProfesor(id, datos);
        return NoContent();
    }

    [HttpGet("estudiantes")]
    public async Task<List<PersonaVista>> Estudiantes() => await academico.ListarEstudiantes();

    [HttpPost("estudiantes")]
    public async Task<PersonaVista> CrearEstudiante(CrearPersona datos) => await academico.CrearEstudiante(datos);

    [HttpPut("estudiantes/{id:int}")]
    public async Task<IActionResult> ActualizarEstudiante(int id, ActualizarPersona datos)
    {
        await academico.ActualizarEstudiante(id, datos);
        return NoContent();
    }

    [HttpGet("anios/{anioId:int}/grupos")]
    public async Task<List<GrupoVista>> Grupos(int anioId) => await academico.ListarGrupos(anioId);

    [HttpPost("grupos")]
    public async Task<GrupoVista> CrearGrupo(CrearGrupo datos) => await academico.CrearGrupo(datos);

    [HttpPost("grupos/{grupoId:int}/estudiantes")]
    public async Task<IActionResult> Matricular(int grupoId, Matricular datos)
    {
        await academico.Matricular(grupoId, datos.EstudianteId);
        return NoContent();
    }

    [HttpDelete("grupos/{grupoId:int}/estudiantes/{estudianteId:int}")]
    public async Task<IActionResult> QuitarEstudiante(int grupoId, int estudianteId)
    {
        await academico.QuitarEstudiante(grupoId, estudianteId);
        return NoContent();
    }

    [HttpPost("grupos/{grupoId:int}/asignaciones")]
    public async Task<AsignacionVista> Asignar(int grupoId, CrearAsignacion datos) =>
        await academico.Asignar(grupoId, datos);
}

[ApiController]
[Authorize(Roles = nameof(Rol.Profesor))]
[Route("api/profesor")]
public class ProfesorController(ServicioDeCalificaciones calificaciones) : ControllerBase
{
    [HttpGet("asignaciones")]
    public async Task<List<AsignacionDelProfesor>> Asignaciones() => await calificaciones.MisAsignaciones();

    [HttpGet("asignaciones/{asignacionId:int}/periodos")]
    public async Task<List<PeriodoVista>> Periodos(int asignacionId) =>
        await calificaciones.PeriodosDeLaAsignacion(asignacionId);

    [HttpGet("asignaciones/{asignacionId:int}/periodos/{periodoId:int}")]
    public async Task<Notas.Planilla> Planilla(int asignacionId, int periodoId) =>
        await calificaciones.LeerPlanilla(asignacionId, periodoId);

    [HttpPut("asignaciones/{asignacionId:int}/periodos/{periodoId:int}/actividades")]
    public async Task<IActionResult> Actividades(int asignacionId, int periodoId, List<ActividadRecibida> actividades)
    {
        await calificaciones.GuardarActividades(asignacionId, periodoId, actividades);
        return NoContent();
    }

    [HttpPut("asignaciones/{asignacionId:int}/periodos/{periodoId:int}/notas")]
    public async Task<IActionResult> Notas(int asignacionId, int periodoId, List<NotaRecibida> notas)
    {
        await calificaciones.GuardarNotas(asignacionId, periodoId, notas);
        return NoContent();
    }

    [HttpPut("asignaciones/{asignacionId:int}/periodos/{periodoId:int}/comentario")]
    public async Task<IActionResult> Comentario(int asignacionId, int periodoId, ComentarioRecibido datos)
    {
        await calificaciones.GuardarComentario(asignacionId, periodoId, datos);
        return NoContent();
    }

    [HttpPost("asignaciones/{asignacionId:int}/periodos/{periodoId:int}/nivelacion")]
    public async Task<IActionResult> Nivelacion(int asignacionId, int periodoId, NivelacionRecibida datos)
    {
        await calificaciones.RegistrarNivelacion(asignacionId, periodoId, datos);
        return NoContent();
    }

    [HttpGet("asignaciones/{asignacionId:int}/periodos/{periodoId:int}/planilla")]
    public async Task<IActionResult> DescargarPlanilla(int asignacionId, int periodoId)
    {
        var planilla = await calificaciones.LeerPlanilla(asignacionId, periodoId);
        return File(PlanillaExcel.Crear(planilla), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "planilla.xlsx");
    }

    [HttpPost("asignaciones/{asignacionId:int}/periodos/{periodoId:int}/planilla")]
    public async Task<IActionResult> SubirPlanilla(int asignacionId, int periodoId, IFormFile archivo)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { mensaje = "Selecciona la planilla de Excel." });

        var planilla = await calificaciones.LeerPlanilla(asignacionId, periodoId);
        if (planilla.PeriodoCerrado)
            return BadRequest(new { mensaje = "El periodo está cerrado. Ya no se puede subir la planilla." });

        await using var contenido = archivo.OpenReadStream();
        var cargadas = PlanillaExcel.Leer(contenido, planilla);
        var notas = cargadas.Select(nota =>
        {
            var estudiante = planilla.Estudiantes.First(item => item.NombreUsuario.Equals(nota.NombreUsuario, StringComparison.OrdinalIgnoreCase));
            var actividad = planilla.Actividades.First(item => item.Nombre.Equals(nota.Actividad, StringComparison.OrdinalIgnoreCase));
            return new NotaRecibida(estudiante.EstudianteId, actividad.Id, nota.Valor);
        }).ToList();

        await calificaciones.GuardarNotas(asignacionId, periodoId, notas);
        return NoContent();
    }

    [HttpGet("listado")]
    public async Task<IActionResult> Listado()
    {
        var estudiantes = await calificaciones.Listado();
        return File(PlanillaExcel.CrearListado(estudiantes), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "estudiantes.xlsx");
    }

    [HttpGet("orientacion")]
    public async Task<List<GrupoDelOrientador>> Orientacion() => await calificaciones.MisGruposDeOrientacion();

    [HttpGet("orientacion/{grupoId:int}/periodos/{periodoId:int}")]
    public async Task<VistaDeOrientacion> VerOrientacion(int grupoId, int periodoId) =>
        await calificaciones.LeerOrientacion(grupoId, periodoId);

    [HttpPut("orientacion/{grupoId:int}/periodos/{periodoId:int}/comentario")]
    public async Task<IActionResult> ComentarioDeOrientador(int grupoId, int periodoId, ComentarioRecibido datos)
    {
        await calificaciones.GuardarComentarioDeOrientador(grupoId, periodoId, datos);
        return NoContent();
    }
}

[ApiController]
[Authorize(Roles = nameof(Rol.Estudiante))]
[Route("api/estudiante")]
public class EstudianteController(ServicioDeBoletin boletin) : ControllerBase
{
    [HttpGet("anios")]
    public async Task<List<AnioDelEstudiante>> Anios() => await boletin.MisAnios();

    [HttpGet("periodos/{periodoId:int}")]
    public async Task<BoletinPeriodo> Periodo(int periodoId) => await boletin.Periodo(periodoId);

    [HttpGet("anios/{anioId:int}")]
    public async Task<BoletinAnio> Anio(int anioId) => await boletin.Anio(anioId);

    [HttpGet("periodos/{periodoId:int}/pdf")]
    public async Task<IActionResult> PdfPeriodo(int periodoId) =>
        File(await boletin.PdfDelPeriodo(periodoId), "application/pdf", "boletin-periodo.pdf");

    [HttpGet("anios/{anioId:int}/pdf")]
    public async Task<IActionResult> PdfAnio(int anioId) =>
        File(await boletin.PdfDelAnio(anioId), "application/pdf", "boletin-anio.pdf");
}
