using Colegio.Api.Datos;
using Colegio.Api.Modelos;
using Colegio.Api.Reglas;
using Colegio.Api.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace Colegio.Api.Servicios;

public record AnioVista(int Id, int Anio, List<PeriodoVista> Periodos);
public record PeriodoVista(int Id, int Numero, string Nombre, decimal Peso, bool Cerrado);
public record NombradoVista(int Id, string Nombre);
public record PersonaVista(int Id, string Nombre, string NombreUsuario);
public record GrupoVista(int Id, string Nombre, string Grado, int GradoId, int? OrientadorId, string? Orientador, List<PersonaVista> Estudiantes, List<AsignacionVista> Asignaciones);
public record AsignacionVista(int Id, string Materia, int MateriaId, string Profesor, int ProfesorId);

public class ServicioAcademico(ColegioDbContext db, SesionActual sesion, Contrasenas contrasenas)
{
    public async Task<List<AnioVista>> ListarAnios()
    {
        return await db.AniosLectivos
            .Where(anio => anio.ColegioId == sesion.ColegioId)
            .OrderByDescending(anio => anio.Anio)
            .Select(anio => new AnioVista(
                anio.Id,
                anio.Anio,
                anio.Periodos
                    .OrderBy(periodo => periodo.Numero)
                    .Select(periodo => new PeriodoVista(periodo.Id, periodo.Numero, periodo.Nombre, periodo.Peso, periodo.Cerrado))
                    .ToList()))
            .ToListAsync();
    }

    public async Task<AnioVista> CrearAnio(CrearAnio datos)
    {
        CalculadoraDeNotas.ExigirCantidadDePeriodos(datos.CantidadPeriodos);
        if (datos.Anio < 2000 || datos.Anio > 2100)
            throw new ReglaDeNegocioException("Escribe un año lectivo válido.");

        if (await db.AniosLectivos.AnyAsync(anio => anio.ColegioId == sesion.ColegioId && anio.Anio == datos.Anio))
            throw new ReglaDeNegocioException("Ese año lectivo ya existe.");

        var pesos = CalculadoraDeNotas.PesosIguales(datos.CantidadPeriodos);
        var anio = new AnioLectivo { ColegioId = sesion.ColegioId, Anio = datos.Anio };
        for (var numero = 1; numero <= datos.CantidadPeriodos; numero++)
        {
            anio.Periodos.Add(new Periodo
            {
                Numero = numero,
                Nombre = $"Periodo {numero}",
                Peso = pesos[numero - 1]
            });
        }

        db.AniosLectivos.Add(anio);
        await db.SaveChangesAsync();
        return (await ListarAnios()).First(item => item.Id == anio.Id);
    }

    public async Task GuardarPesos(int anioId, List<PesoDePeriodo> pesos)
    {
        var anio = await ExigirAnio(anioId);
        if (pesos.Count != anio.Periodos.Count || pesos.Any(peso => anio.Periodos.All(periodo => periodo.Id != peso.PeriodoId)))
            throw new ReglaDeNegocioException("Indica el peso de cada periodo de este año.");

        if (anio.Periodos.Any(periodo => periodo.Cerrado))
            throw new ReglaDeNegocioException("No se pueden cambiar los pesos cuando un periodo ya está cerrado.");

        CalculadoraDeNotas.ExigirPesos(pesos.Select(peso => peso.Peso).ToList());
        foreach (var peso in pesos)
            anio.Periodos.First(periodo => periodo.Id == peso.PeriodoId).Peso = CalculadoraDeNotas.Redondear(peso.Peso);

        await db.SaveChangesAsync();
    }

    public async Task CerrarPeriodo(int periodoId)
    {
        var periodo = await db.Periodos
            .Include(item => item.AnioLectivo)
            .FirstOrDefaultAsync(item => item.Id == periodoId && item.AnioLectivo.ColegioId == sesion.ColegioId)
            ?? throw new NoEncontradoException("No existe ese periodo.");

        if (periodo.Cerrado)
            throw new ReglaDeNegocioException("Ese periodo ya está cerrado.");

        periodo.Cerrado = true;
        await db.SaveChangesAsync();
    }

    public async Task<List<NombradoVista>> ListarGrados() =>
        await db.Grados.Where(grado => grado.ColegioId == sesion.ColegioId)
            .OrderBy(grado => grado.Nombre)
            .Select(grado => new NombradoVista(grado.Id, grado.Nombre))
            .ToListAsync();

    public async Task<NombradoVista> CrearGrado(string nombre)
    {
        var limpio = Textos.Exigir(nombre, "Escribe el nombre del grado.");
        if (await db.Grados.AnyAsync(grado => grado.ColegioId == sesion.ColegioId && grado.Nombre == limpio))
            throw new ReglaDeNegocioException("Ese grado ya existe.");

        var grado = new Grado { ColegioId = sesion.ColegioId, Nombre = limpio };
        db.Grados.Add(grado);
        await db.SaveChangesAsync();
        return new NombradoVista(grado.Id, grado.Nombre);
    }

    public async Task<List<NombradoVista>> ListarMaterias() =>
        await db.Materias.Where(materia => materia.ColegioId == sesion.ColegioId)
            .OrderBy(materia => materia.Nombre)
            .Select(materia => new NombradoVista(materia.Id, materia.Nombre))
            .ToListAsync();

    public async Task<NombradoVista> CrearMateria(string nombre)
    {
        var limpio = Textos.Exigir(nombre, "Escribe el nombre de la materia.");
        if (await db.Materias.AnyAsync(materia => materia.ColegioId == sesion.ColegioId && materia.Nombre == limpio))
            throw new ReglaDeNegocioException("Esa materia ya existe.");

        var materia = new Materia { ColegioId = sesion.ColegioId, Nombre = limpio };
        db.Materias.Add(materia);
        await db.SaveChangesAsync();
        return new NombradoVista(materia.Id, materia.Nombre);
    }

    public Task<List<PersonaVista>> ListarProfesores() => ListarPersonas(Rol.Profesor);

    public Task<PersonaVista> CrearProfesor(CrearPersona datos) => CrearPersona(datos, Rol.Profesor);

    public Task ActualizarProfesor(int id, ActualizarPersona datos) => ActualizarPersona(id, datos, Rol.Profesor);

    public Task<List<PersonaVista>> ListarEstudiantes() => ListarPersonas(Rol.Estudiante);

    public Task<PersonaVista> CrearEstudiante(CrearPersona datos) => CrearPersona(datos, Rol.Estudiante);

    public Task ActualizarEstudiante(int id, ActualizarPersona datos) => ActualizarPersona(id, datos, Rol.Estudiante);

    public async Task<List<GrupoVista>> ListarGrupos(int anioId)
    {
        await ExigirAnio(anioId);
        return await db.Grupos
            .Where(grupo => grupo.AnioLectivoId == anioId)
            .OrderBy(grupo => grupo.Nombre)
            .Select(grupo => new GrupoVista(
                grupo.Id,
                grupo.Nombre,
                grupo.Grado.Nombre,
                grupo.GradoId,
                grupo.OrientadorId,
                grupo.Orientador != null ? grupo.Orientador.Nombre : null,
                grupo.Matriculas
                    .OrderBy(matricula => matricula.Estudiante.Nombre)
                    .Select(matricula => new PersonaVista(matricula.Estudiante.Id, matricula.Estudiante.Nombre, matricula.Estudiante.NombreUsuario))
                    .ToList(),
                grupo.Asignaciones
                    .OrderBy(asignacion => asignacion.Materia.Nombre)
                    .Select(asignacion => new AsignacionVista(asignacion.Id, asignacion.Materia.Nombre, asignacion.MateriaId, asignacion.Profesor.Nombre, asignacion.ProfesorId))
                    .ToList()))
            .ToListAsync();
    }

    public async Task<GrupoVista> CrearGrupo(CrearGrupo datos)
    {
        await ExigirAnio(datos.AnioLectivoId);
        var nombre = Textos.Exigir(datos.Nombre, "Escribe el nombre del grupo, por ejemplo 6A.");
        if (await db.Grupos.AnyAsync(grupo => grupo.AnioLectivoId == datos.AnioLectivoId && grupo.Nombre == nombre))
            throw new ReglaDeNegocioException("Ya hay un grupo con ese nombre en este año.");

        var grado = await db.Grados.FirstOrDefaultAsync(item => item.Id == datos.GradoId && item.ColegioId == sesion.ColegioId)
            ?? throw new NoEncontradoException("No existe ese grado.");
        await ExigirProfesor(datos.OrientadorId);

        var grupo = new Grupo
        {
            AnioLectivoId = datos.AnioLectivoId,
            GradoId = grado.Id,
            Nombre = nombre,
            OrientadorId = datos.OrientadorId
        };
        db.Grupos.Add(grupo);
        await db.SaveChangesAsync();
        return (await ListarGrupos(datos.AnioLectivoId)).First(item => item.Id == grupo.Id);
    }

    public async Task Matricular(int grupoId, int estudianteId)
    {
        var grupo = await ExigirGrupo(grupoId);
        var estudiante = await ExigirEstudiante(estudianteId);
        var yaEsta = await db.Matriculas.AnyAsync(matricula =>
            matricula.EstudianteId == estudiante.Id && matricula.Grupo.AnioLectivoId == grupo.AnioLectivoId);
        if (yaEsta)
            throw new ReglaDeNegocioException("Ese estudiante ya está matriculado en un grupo de este año.");

        db.Matriculas.Add(new Matricula { GrupoId = grupo.Id, EstudianteId = estudiante.Id });
        await db.SaveChangesAsync();
    }

    public async Task QuitarEstudiante(int grupoId, int estudianteId)
    {
        var grupo = await ExigirGrupo(grupoId);
        var matricula = await db.Matriculas.FirstOrDefaultAsync(item => item.GrupoId == grupo.Id && item.EstudianteId == estudianteId)
            ?? throw new NoEncontradoException("Ese estudiante no está en el grupo.");

        var asignaciones = await db.Asignaciones.Where(item => item.GrupoId == grupo.Id).Select(item => item.Id).ToListAsync();
        var notas = await db.NotasActividad.Where(nota => nota.EstudianteId == estudianteId && asignaciones.Contains(nota.Actividad.AsignacionId)).ToListAsync();
        var nivelaciones = await db.Nivelaciones.Where(item => item.EstudianteId == estudianteId && asignaciones.Contains(item.AsignacionId)).ToListAsync();
        var comentarios = await db.ComentariosMateria.Where(item => item.EstudianteId == estudianteId && asignaciones.Contains(item.AsignacionId)).ToListAsync();
        var delOrientador = await db.ComentariosOrientador.Where(item => item.EstudianteId == estudianteId && item.GrupoId == grupo.Id).ToListAsync();

        db.RemoveRange(notas);
        db.RemoveRange(nivelaciones);
        db.RemoveRange(comentarios);
        db.RemoveRange(delOrientador);
        db.Matriculas.Remove(matricula);
        await db.SaveChangesAsync();
    }

    public async Task<AsignacionVista> Asignar(int grupoId, CrearAsignacion datos)
    {
        var grupo = await ExigirGrupo(grupoId);
        var materia = await db.Materias.FirstOrDefaultAsync(item => item.Id == datos.MateriaId && item.ColegioId == sesion.ColegioId)
            ?? throw new NoEncontradoException("No existe esa materia.");
        var profesor = await ExigirProfesor(datos.ProfesorId);
        if (await db.Asignaciones.AnyAsync(item => item.GrupoId == grupo.Id && item.MateriaId == materia.Id))
            throw new ReglaDeNegocioException("Esa materia ya tiene profesor en este grupo.");

        var asignacion = new Asignacion { GrupoId = grupo.Id, MateriaId = materia.Id, ProfesorId = profesor.Id };
        db.Asignaciones.Add(asignacion);
        await db.SaveChangesAsync();
        return new AsignacionVista(asignacion.Id, materia.Nombre, materia.Id, profesor.Nombre, profesor.Id);
    }

    private async Task<List<PersonaVista>> ListarPersonas(Rol rol) =>
        await db.Usuarios
            .Where(usuario => usuario.ColegioId == sesion.ColegioId && usuario.Rol == rol)
            .OrderBy(usuario => usuario.Nombre)
            .Select(usuario => new PersonaVista(usuario.Id, usuario.Nombre, usuario.NombreUsuario))
            .ToListAsync();

    private async Task<PersonaVista> CrearPersona(CrearPersona datos, Rol rol)
    {
        var nombre = Textos.Exigir(datos.Nombre, "Escribe el nombre.");
        var usuario = Textos.Usuario(datos.NombreUsuario);
        Textos.ExigirContrasena(datos.Contrasena);
        if (await db.Usuarios.AnyAsync(item => item.NombreUsuario == usuario))
            throw new ReglaDeNegocioException("Ese nombre de usuario ya existe.");

        var persona = new Usuario
        {
            ColegioId = sesion.ColegioId,
            Nombre = nombre,
            NombreUsuario = usuario,
            Rol = rol
        };
        persona.ContrasenaHash = contrasenas.CrearHash(persona, datos.Contrasena.Trim());
        db.Usuarios.Add(persona);
        await db.SaveChangesAsync();
        return new PersonaVista(persona.Id, persona.Nombre, persona.NombreUsuario);
    }

    private async Task ActualizarPersona(int id, ActualizarPersona datos, Rol rol)
    {
        var persona = await db.Usuarios.FirstOrDefaultAsync(item => item.Id == id && item.ColegioId == sesion.ColegioId && item.Rol == rol)
            ?? throw new NoEncontradoException("No existe esa persona.");

        persona.Nombre = Textos.Exigir(datos.Nombre, "Escribe el nombre.");
        if (!string.IsNullOrWhiteSpace(datos.Contrasena))
        {
            Textos.ExigirContrasena(datos.Contrasena);
            persona.ContrasenaHash = contrasenas.CrearHash(persona, datos.Contrasena.Trim());
        }

        await db.SaveChangesAsync();
    }

    private async Task<AnioLectivo> ExigirAnio(int anioId) =>
        await db.AniosLectivos.Include(anio => anio.Periodos)
            .FirstOrDefaultAsync(anio => anio.Id == anioId && anio.ColegioId == sesion.ColegioId)
        ?? throw new NoEncontradoException("No existe ese año lectivo.");

    private async Task<Grupo> ExigirGrupo(int grupoId) =>
        await db.Grupos.Include(grupo => grupo.AnioLectivo)
            .FirstOrDefaultAsync(grupo => grupo.Id == grupoId && grupo.AnioLectivo.ColegioId == sesion.ColegioId)
        ?? throw new NoEncontradoException("No existe ese grupo.");

    private async Task<Usuario> ExigirProfesor(int id) =>
        await db.Usuarios.FirstOrDefaultAsync(usuario => usuario.Id == id && usuario.ColegioId == sesion.ColegioId && usuario.Rol == Rol.Profesor)
        ?? throw new NoEncontradoException("No existe ese profesor.");

    private async Task<Usuario> ExigirEstudiante(int id) =>
        await db.Usuarios.FirstOrDefaultAsync(usuario => usuario.Id == id && usuario.ColegioId == sesion.ColegioId && usuario.Rol == Rol.Estudiante)
        ?? throw new NoEncontradoException("No existe ese estudiante.");
}
