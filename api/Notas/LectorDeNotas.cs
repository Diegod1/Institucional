using Colegio.Api.Datos;
using Colegio.Api.Modelos;
using Colegio.Api.Reglas;
using Microsoft.EntityFrameworkCore;

namespace Colegio.Api.Notas;

public record ActividadVista(int Id, string Nombre, decimal Porcentaje);

public record NotaPuesta(int ActividadId, string Nombre, decimal Porcentaje, decimal? Valor);

public record NivelacionHecha(decimal? NotaAnterior, decimal NotaNueva, DateTime Fecha);

public record FilaDeEstudiante(
    int EstudianteId,
    string Nombre,
    string NombreUsuario,
    IReadOnlyList<NotaPuesta> Notas,
    decimal? NotaDeActividades,
    decimal? NotaDelPeriodo,
    IReadOnlyList<NivelacionHecha> Nivelaciones,
    string Comentario);

public record Planilla(
    int AsignacionId,
    int PeriodoId,
    bool PeriodoCerrado,
    string Materia,
    string Grupo,
    string Grado,
    int Anio,
    string NombrePeriodo,
    IReadOnlyList<ActividadVista> Actividades,
    IReadOnlyList<FilaDeEstudiante> Estudiantes);

public class LectorDeNotas(ColegioDbContext db)
{
    public async Task<Planilla> Leer(int asignacionId, int periodoId)
    {
        var asignacion = await db.Asignaciones
            .Include(item => item.Materia)
            .Include(item => item.Grupo).ThenInclude(grupo => grupo.Grado)
            .Include(item => item.Grupo).ThenInclude(grupo => grupo.AnioLectivo)
            .Include(item => item.Grupo).ThenInclude(grupo => grupo.Matriculas).ThenInclude(matricula => matricula.Estudiante)
            .FirstOrDefaultAsync(item => item.Id == asignacionId)
            ?? throw new NoEncontradoException("No existe esa asignación.");

        var periodo = await db.Periodos.FirstOrDefaultAsync(item =>
            item.Id == periodoId && item.AnioLectivoId == asignacion.Grupo.AnioLectivoId)
            ?? throw new NoEncontradoException("Ese periodo no pertenece a este grupo.");

        var actividades = await db.Actividades
            .Include(item => item.Notas)
            .Where(item => item.AsignacionId == asignacionId && item.PeriodoId == periodoId)
            .OrderBy(item => item.Id)
            .ToListAsync();

        var nivelaciones = await db.Nivelaciones
            .Where(item => item.AsignacionId == asignacionId && item.PeriodoId == periodoId)
            .OrderBy(item => item.Fecha)
            .ToListAsync();

        var comentarios = await db.ComentariosMateria
            .Where(item => item.AsignacionId == asignacionId && item.PeriodoId == periodoId)
            .ToListAsync();

        var filas = asignacion.Grupo.Matriculas
            .OrderBy(matricula => matricula.Estudiante.Nombre)
            .Select(matricula => Fila(matricula.Estudiante, actividades, nivelaciones, comentarios))
            .ToList();

        return new Planilla(
            asignacion.Id,
            periodo.Id,
            periodo.Cerrado,
            asignacion.Materia.Nombre,
            asignacion.Grupo.Nombre,
            asignacion.Grupo.Grado.Nombre,
            asignacion.Grupo.AnioLectivo.Anio,
            periodo.Nombre,
            actividades.Select(item => new ActividadVista(item.Id, item.Nombre, item.Porcentaje)).ToList(),
            filas);
    }

    public static FilaDeEstudiante Fila(
        Usuario estudiante,
        IReadOnlyList<Actividad> actividades,
        IReadOnlyList<Nivelacion> nivelaciones,
        IReadOnlyList<ComentarioMateria> comentarios)
    {
        var notas = actividades.Select(actividad =>
        {
            var nota = actividad.Notas.FirstOrDefault(item => item.EstudianteId == estudiante.Id);
            return new NotaPuesta(actividad.Id, actividad.Nombre, actividad.Porcentaje, nota?.Valor);
        }).ToList();

        var notaDeActividades = CalculadoraDeNotas.NotaDeActividades(
            notas.Select(nota => nota.Valor).ToList(),
            notas.Select(nota => nota.Porcentaje).ToList());

        var historial = nivelaciones
            .Where(item => item.EstudianteId == estudiante.Id)
            .Select(item => new NivelacionHecha(item.NotaAnterior, item.NotaNueva, item.Fecha))
            .ToList();

        var notaDelPeriodo = CalculadoraDeNotas.NotaDelPeriodo(
            notaDeActividades,
            historial.Count == 0 ? null : historial[^1].NotaNueva);

        var comentario = comentarios.FirstOrDefault(item => item.EstudianteId == estudiante.Id)?.Texto ?? "";

        return new FilaDeEstudiante(
            estudiante.Id,
            estudiante.Nombre,
            estudiante.NombreUsuario,
            notas,
            notaDeActividades,
            notaDelPeriodo,
            historial,
            comentario);
    }
}
