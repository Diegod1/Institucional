using Colegio.Api.Datos;
using Colegio.Api.Modelos;
using Colegio.Api.Notas;
using Colegio.Api.Reglas;
using Colegio.Api.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace Colegio.Api.Servicios;

public record ActividadRecibida(string Nombre, decimal Porcentaje);
public record NotaRecibida(int EstudianteId, int ActividadId, decimal Valor);
public record ComentarioRecibido(string Texto, int? EstudianteId);
public record NivelacionRecibida(int EstudianteId, decimal Nota);

public record AsignacionDelProfesor(int Id, string Grupo, string Grado, string Materia, int Anio, int AnioLectivoId);
public record GrupoDelOrientador(int Id, string Nombre, string Grado, int Anio, int AnioLectivoId, List<PeriodoVista> Periodos);
public record EstudianteDelListado(string Grupo, string Nombre, string NombreUsuario);

public class ServicioDeCalificaciones(ColegioDbContext db, SesionActual sesion, LectorDeNotas lector)
{
    public async Task<List<AsignacionDelProfesor>> MisAsignaciones() =>
        await db.Asignaciones
            .Where(item => item.ProfesorId == sesion.UsuarioId)
            .OrderBy(item => item.Grupo.Nombre).ThenBy(item => item.Materia.Nombre)
            .Select(item => new AsignacionDelProfesor(
                item.Id,
                item.Grupo.Nombre,
                item.Grupo.Grado.Nombre,
                item.Materia.Nombre,
                item.Grupo.AnioLectivo.Anio,
                item.Grupo.AnioLectivoId))
            .ToListAsync();

    public async Task<List<PeriodoVista>> PeriodosDeLaAsignacion(int asignacionId)
    {
        var asignacion = await ExigirAsignacionPropia(asignacionId);
        return await db.Periodos
            .Where(periodo => periodo.AnioLectivoId == asignacion.Grupo.AnioLectivoId)
            .OrderBy(periodo => periodo.Numero)
            .Select(periodo => new PeriodoVista(periodo.Id, periodo.Numero, periodo.Nombre, periodo.Peso, periodo.Cerrado))
            .ToListAsync();
    }

    public async Task<Planilla> LeerPlanilla(int asignacionId, int periodoId)
    {
        await ExigirAsignacionPropia(asignacionId);
        return await lector.Leer(asignacionId, periodoId);
    }

    public async Task GuardarActividades(int asignacionId, int periodoId, List<ActividadRecibida> recibidas)
    {
        var asignacion = await ExigirAsignacionPropia(asignacionId);
        var periodo = await ExigirPeriodoAbierto(periodoId, asignacion.Grupo.AnioLectivoId);
        var actividades = recibidas.Select(item => new ActividadRecibida(Textos.Exigir(item.Nombre, "Cada actividad necesita un nombre."), item.Porcentaje)).ToList();
        if (actividades.Select(item => item.Nombre.ToLowerInvariant()).Distinct().Count() != actividades.Count)
            throw new ReglaDeNegocioException("No repitas el nombre de una actividad.");

        CalculadoraDeNotas.ExigirPorcentajes(actividades.Select(item => item.Porcentaje).ToList());

        var actuales = await db.Actividades
            .Where(item => item.AsignacionId == asignacion.Id && item.PeriodoId == periodo.Id)
            .ToListAsync();

        var nombres = actividades.Select(item => item.Nombre).ToHashSet(StringComparer.OrdinalIgnoreCase);
        db.Actividades.RemoveRange(actuales.Where(item => !nombres.Contains(item.Nombre)));

        foreach (var recibida in actividades)
        {
            var existente = actuales.FirstOrDefault(item => item.Nombre.Equals(recibida.Nombre, StringComparison.OrdinalIgnoreCase));
            if (existente is null)
            {
                db.Actividades.Add(new Actividad
                {
                    AsignacionId = asignacion.Id,
                    PeriodoId = periodo.Id,
                    Nombre = recibida.Nombre,
                    Porcentaje = CalculadoraDeNotas.Redondear(recibida.Porcentaje)
                });
            }
            else
            {
                existente.Nombre = recibida.Nombre;
                existente.Porcentaje = CalculadoraDeNotas.Redondear(recibida.Porcentaje);
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task GuardarNotas(int asignacionId, int periodoId, List<NotaRecibida> notas)
    {
        var planilla = await ExigirPlanillaAbierta(asignacionId, periodoId);
        var estudiantes = planilla.Estudiantes.Select(item => item.EstudianteId).ToHashSet();
        var actividades = planilla.Actividades.Select(item => item.Id).ToHashSet();

        foreach (var nota in notas)
        {
            CalculadoraDeNotas.ExigirNota(nota.Valor);
            if (!estudiantes.Contains(nota.EstudianteId) || !actividades.Contains(nota.ActividadId))
                throw new ReglaDeNegocioException("Hay una nota que no corresponde a este grupo o a estas actividades.");
        }

        var actuales = await db.NotasActividad
            .Where(item => item.Actividad.AsignacionId == asignacionId && item.Actividad.PeriodoId == periodoId)
            .ToListAsync();

        foreach (var nota in notas)
        {
            var actual = actuales.FirstOrDefault(item => item.ActividadId == nota.ActividadId && item.EstudianteId == nota.EstudianteId);
            if (actual is null)
                db.NotasActividad.Add(new NotaActividad { ActividadId = nota.ActividadId, EstudianteId = nota.EstudianteId, Valor = nota.Valor });
            else
                actual.Valor = nota.Valor;
        }

        await db.SaveChangesAsync();
    }

    public async Task GuardarComentario(int asignacionId, int periodoId, ComentarioRecibido datos)
    {
        var planilla = await ExigirPlanillaAbierta(asignacionId, periodoId);
        var texto = datos.Texto?.Trim() ?? "";
        if (texto.Length > 2000)
            throw new ReglaDeNegocioException("El comentario puede tener hasta 2000 caracteres.");

        var destinatarios = datos.EstudianteId is int estudianteId
            ? [ExigirEstudiante(planilla, estudianteId)]
            : planilla.Estudiantes.Select(item => item.EstudianteId).ToList();

        var actuales = await db.ComentariosMateria
            .Where(item => item.AsignacionId == asignacionId && item.PeriodoId == periodoId && destinatarios.Contains(item.EstudianteId))
            .ToListAsync();

        foreach (var destinatario in destinatarios)
        {
            var actual = actuales.FirstOrDefault(item => item.EstudianteId == destinatario);
            if (actual is null)
                db.ComentariosMateria.Add(new ComentarioMateria { AsignacionId = asignacionId, PeriodoId = periodoId, EstudianteId = destinatario, Texto = texto });
            else
                actual.Texto = texto;
        }

        await db.SaveChangesAsync();
    }

    public async Task RegistrarNivelacion(int asignacionId, int periodoId, NivelacionRecibida datos)
    {
        var planilla = await ExigirPlanillaAbierta(asignacionId, periodoId);
        CalculadoraDeNotas.ExigirNota(datos.Nota);
        var estudiante = planilla.Estudiantes.FirstOrDefault(item => item.EstudianteId == datos.EstudianteId)
            ?? throw new ReglaDeNegocioException("Ese estudiante no está en el grupo.");

        db.Nivelaciones.Add(new Nivelacion
        {
            AsignacionId = asignacionId,
            PeriodoId = periodoId,
            EstudianteId = datos.EstudianteId,
            NotaAnterior = estudiante.NotaDelPeriodo,
            NotaNueva = datos.Nota,
            Fecha = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<EstudianteDelListado>> Listado()
    {
        var porClase = await db.Asignaciones
            .Where(item => item.ProfesorId == sesion.UsuarioId)
            .SelectMany(item => item.Grupo.Matriculas.Select(matricula => new EstudianteDelListado(
                item.Grupo.Nombre,
                matricula.Estudiante.Nombre,
                matricula.Estudiante.NombreUsuario)))
            .ToListAsync();

        var comoOrientador = await db.Grupos
            .Where(grupo => grupo.OrientadorId == sesion.UsuarioId)
            .SelectMany(grupo => grupo.Matriculas.Select(matricula => new EstudianteDelListado(
                grupo.Nombre,
                matricula.Estudiante.Nombre,
                matricula.Estudiante.NombreUsuario)))
            .ToListAsync();

        return porClase.Concat(comoOrientador)
            .DistinctBy(item => $"{item.Grupo}|{item.NombreUsuario}")
            .OrderBy(item => item.Grupo).ThenBy(item => item.Nombre)
            .ToList();
    }

    public async Task<List<GrupoDelOrientador>> MisGruposDeOrientacion() =>
        await db.Grupos
            .Where(grupo => grupo.OrientadorId == sesion.UsuarioId)
            .OrderBy(grupo => grupo.Nombre)
            .Select(grupo => new GrupoDelOrientador(
                grupo.Id,
                grupo.Nombre,
                grupo.Grado.Nombre,
                grupo.AnioLectivo.Anio,
                grupo.AnioLectivoId,
                grupo.AnioLectivo.Periodos
                    .OrderBy(periodo => periodo.Numero)
                    .Select(periodo => new PeriodoVista(periodo.Id, periodo.Numero, periodo.Nombre, periodo.Peso, periodo.Cerrado))
                    .ToList()))
            .ToListAsync();

    public async Task<VistaDeOrientacion> LeerOrientacion(int grupoId, int periodoId)
    {
        var grupo = await ExigirGrupoDeOrientacion(grupoId);
        var periodo = await db.Periodos.FirstOrDefaultAsync(item => item.Id == periodoId && item.AnioLectivoId == grupo.AnioLectivoId)
            ?? throw new NoEncontradoException("Ese periodo no pertenece a este grupo.");

        var colegio = await db.Colegios.Include(item => item.Rangos).FirstAsync(item => item.Id == sesion.ColegioId);
        var rangos = colegio.Rangos.Select(rango => (rango.Nombre, rango.Desde, rango.Hasta)).ToList();
        var asignaciones = await db.Asignaciones.Where(item => item.GrupoId == grupo.Id).Select(item => item.Id).ToListAsync();
        var planillas = new List<Planilla>();
        foreach (var asignacionId in asignaciones)
            planillas.Add(await lector.Leer(asignacionId, periodoId));

        var comentarios = await db.ComentariosOrientador
            .Where(item => item.GrupoId == grupo.Id && item.PeriodoId == periodoId)
            .ToListAsync();

        var estudiantes = grupo.Matriculas
            .OrderBy(matricula => matricula.Estudiante.Nombre)
            .Select(matricula =>
            {
                var materias = planillas.Select(planilla =>
                {
                    var fila = planilla.Estudiantes.First(item => item.EstudianteId == matricula.EstudianteId);
                    return new MateriaOrientada(
                        planilla.Materia,
                        fila.NotaDelPeriodo,
                        fila.NotaDelPeriodo is decimal nota ? CalculadoraDeNotas.Desempeno(nota, rangos) : null,
                        fila.Comentario);
                }).ToList();

                return new EstudianteOrientado(
                    matricula.EstudianteId,
                    matricula.Estudiante.Nombre,
                    comentarios.FirstOrDefault(item => item.EstudianteId == matricula.EstudianteId)?.Texto ?? "",
                    materias);
            }).ToList();

        return new VistaDeOrientacion(grupo.Nombre, periodo.Nombre, periodo.Cerrado, estudiantes);
    }

    public async Task GuardarComentarioDeOrientador(int grupoId, int periodoId, ComentarioRecibido datos)
    {
        var grupo = await ExigirGrupoDeOrientacion(grupoId);
        var periodo = await db.Periodos.FirstOrDefaultAsync(item => item.Id == periodoId && item.AnioLectivoId == grupo.AnioLectivoId)
            ?? throw new NoEncontradoException("Ese periodo no pertenece a este grupo.");
        if (periodo.Cerrado)
            throw new ReglaDeNegocioException("El periodo está cerrado. Ya no se puede cambiar el comentario.");

        var texto = datos.Texto?.Trim() ?? "";
        if (texto.Length > 2000)
            throw new ReglaDeNegocioException("El comentario puede tener hasta 2000 caracteres.");

        var estudiantes = grupo.Matriculas.Select(item => item.EstudianteId).ToList();
        List<int> destinatarios;
        if (datos.EstudianteId is int estudianteId)
        {
            if (!estudiantes.Contains(estudianteId))
                throw new ReglaDeNegocioException("Ese estudiante no está en el grupo.");
            destinatarios = [estudianteId];
        }
        else
        {
            destinatarios = estudiantes;
        }

        var actuales = await db.ComentariosOrientador
            .Where(item => item.GrupoId == grupo.Id && item.PeriodoId == periodo.Id && destinatarios.Contains(item.EstudianteId))
            .ToListAsync();

        foreach (var destinatario in destinatarios)
        {
            var actual = actuales.FirstOrDefault(item => item.EstudianteId == destinatario);
            if (actual is null)
                db.ComentariosOrientador.Add(new ComentarioOrientador { GrupoId = grupo.Id, PeriodoId = periodo.Id, EstudianteId = destinatario, Texto = texto });
            else
                actual.Texto = texto;
        }

        await db.SaveChangesAsync();
    }

    private async Task<Planilla> ExigirPlanillaAbierta(int asignacionId, int periodoId)
    {
        await ExigirAsignacionPropia(asignacionId);
        var planilla = await lector.Leer(asignacionId, periodoId);
        if (planilla.PeriodoCerrado)
            throw new ReglaDeNegocioException("El periodo está cerrado. Ya no se pueden cambiar las notas, la nivelación ni la planilla.");
        return planilla;
    }

    private async Task<Asignacion> ExigirAsignacionPropia(int asignacionId)
    {
        var asignacion = await db.Asignaciones
            .Include(item => item.Grupo)
            .FirstOrDefaultAsync(item => item.Id == asignacionId && item.ProfesorId == sesion.UsuarioId)
            ?? throw new NoEncontradoException("No tienes esa materia asignada.");
        return asignacion;
    }

    private async Task<Periodo> ExigirPeriodoAbierto(int periodoId, int anioLectivoId)
    {
        var periodo = await db.Periodos.FirstOrDefaultAsync(item => item.Id == periodoId && item.AnioLectivoId == anioLectivoId)
            ?? throw new NoEncontradoException("Ese periodo no pertenece a este grupo.");
        if (periodo.Cerrado)
            throw new ReglaDeNegocioException("El periodo está cerrado. Ya no se pueden cambiar las actividades ni las notas.");
        return periodo;
    }

    private async Task<Grupo> ExigirGrupoDeOrientacion(int grupoId) =>
        await db.Grupos.Include(grupo => grupo.Matriculas).ThenInclude(matricula => matricula.Estudiante)
            .FirstOrDefaultAsync(grupo => grupo.Id == grupoId && grupo.OrientadorId == sesion.UsuarioId)
        ?? throw new NoEncontradoException("No eres orientador de ese grupo.");

    private static int ExigirEstudiante(Planilla planilla, int estudianteId) =>
        planilla.Estudiantes.Any(item => item.EstudianteId == estudianteId)
            ? estudianteId
            : throw new ReglaDeNegocioException("Ese estudiante no está en el grupo.");
}

public record VistaDeOrientacion(string Grupo, string NombrePeriodo, bool Cerrado, IReadOnlyList<EstudianteOrientado> Estudiantes);
public record EstudianteOrientado(int EstudianteId, string Nombre, string Comentario, IReadOnlyList<MateriaOrientada> Materias);
public record MateriaOrientada(string Materia, decimal? Nota, string? Desempeno, string ComentarioProfesor);
