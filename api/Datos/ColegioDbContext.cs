using Colegio.Api.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Colegio.Api.Datos;

public class ColegioDbContext(DbContextOptions<ColegioDbContext> opciones) : DbContext(opciones)
{
    public DbSet<Modelos.Colegio> Colegios => Set<Modelos.Colegio>();
    public DbSet<RangoDesempeno> RangosDesempeno => Set<RangoDesempeno>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<AnioLectivo> AniosLectivos => Set<AnioLectivo>();
    public DbSet<Periodo> Periodos => Set<Periodo>();
    public DbSet<Grado> Grados => Set<Grado>();
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();
    public DbSet<Asignacion> Asignaciones => Set<Asignacion>();
    public DbSet<Actividad> Actividades => Set<Actividad>();
    public DbSet<NotaActividad> NotasActividad => Set<NotaActividad>();
    public DbSet<Nivelacion> Nivelaciones => Set<Nivelacion>();
    public DbSet<ComentarioMateria> ComentariosMateria => Set<ComentarioMateria>();
    public DbSet<ComentarioOrientador> ComentariosOrientador => Set<ComentarioOrientador>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(5, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(usuario =>
        {
            usuario.HasIndex(item => item.NombreUsuario).IsUnique();
            usuario.Property(item => item.Nombre).HasMaxLength(120);
            usuario.Property(item => item.NombreUsuario).HasMaxLength(60);
            usuario.HasOne(item => item.Colegio).WithMany(colegio => colegio.Usuarios).HasForeignKey(item => item.ColegioId);
        });

        modelBuilder.Entity<Modelos.Colegio>(colegio =>
        {
            colegio.Property(item => item.Nombre).HasMaxLength(160);
        });

        modelBuilder.Entity<RangoDesempeno>(rango =>
        {
            rango.Property(item => item.Nombre).HasMaxLength(40);
            rango.HasOne(item => item.Colegio).WithMany(colegio => colegio.Rangos).HasForeignKey(item => item.ColegioId);
        });

        modelBuilder.Entity<AnioLectivo>(anio =>
        {
            anio.HasIndex(item => new { item.ColegioId, item.Anio }).IsUnique();
            anio.HasOne(item => item.Colegio).WithMany().HasForeignKey(item => item.ColegioId);
        });

        modelBuilder.Entity<Periodo>(periodo =>
        {
            periodo.HasIndex(item => new { item.AnioLectivoId, item.Numero }).IsUnique();
            periodo.Property(item => item.Nombre).HasMaxLength(40);
            periodo.HasOne(item => item.AnioLectivo).WithMany(anio => anio.Periodos).HasForeignKey(item => item.AnioLectivoId);
        });

        modelBuilder.Entity<Grado>(grado =>
        {
            grado.HasIndex(item => new { item.ColegioId, item.Nombre }).IsUnique();
            grado.Property(item => item.Nombre).HasMaxLength(80);
            grado.HasOne(item => item.Colegio).WithMany().HasForeignKey(item => item.ColegioId);
        });

        modelBuilder.Entity<Materia>(materia =>
        {
            materia.HasIndex(item => new { item.ColegioId, item.Nombre }).IsUnique();
            materia.Property(item => item.Nombre).HasMaxLength(80);
            materia.HasOne(item => item.Colegio).WithMany().HasForeignKey(item => item.ColegioId);
        });

        modelBuilder.Entity<Grupo>(grupo =>
        {
            grupo.HasIndex(item => new { item.AnioLectivoId, item.Nombre }).IsUnique();
            grupo.Property(item => item.Nombre).HasMaxLength(40);
            grupo.HasOne(item => item.AnioLectivo).WithMany(anio => anio.Grupos).HasForeignKey(item => item.AnioLectivoId);
            grupo.HasOne(item => item.Grado).WithMany().HasForeignKey(item => item.GradoId);
            grupo.HasOne(item => item.Orientador).WithMany().HasForeignKey(item => item.OrientadorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Matricula>(matricula =>
        {
            matricula.HasIndex(item => new { item.GrupoId, item.EstudianteId }).IsUnique();
            matricula.HasOne(item => item.Grupo).WithMany(grupo => grupo.Matriculas).HasForeignKey(item => item.GrupoId);
            matricula.HasOne(item => item.Estudiante).WithMany().HasForeignKey(item => item.EstudianteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Asignacion>(asignacion =>
        {
            asignacion.HasIndex(item => new { item.GrupoId, item.MateriaId }).IsUnique();
            asignacion.HasOne(item => item.Grupo).WithMany(grupo => grupo.Asignaciones).HasForeignKey(item => item.GrupoId);
            asignacion.HasOne(item => item.Materia).WithMany().HasForeignKey(item => item.MateriaId);
            asignacion.HasOne(item => item.Profesor).WithMany().HasForeignKey(item => item.ProfesorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Actividad>(actividad =>
        {
            actividad.Property(item => item.Nombre).HasMaxLength(80);
            actividad.HasOne(item => item.Asignacion).WithMany().HasForeignKey(item => item.AsignacionId);
            actividad.HasOne(item => item.Periodo).WithMany().HasForeignKey(item => item.PeriodoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NotaActividad>(nota =>
        {
            nota.HasIndex(item => new { item.ActividadId, item.EstudianteId }).IsUnique();
            nota.HasOne(item => item.Actividad).WithMany(actividad => actividad.Notas).HasForeignKey(item => item.ActividadId);
            nota.HasOne(item => item.Estudiante).WithMany().HasForeignKey(item => item.EstudianteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Nivelacion>(nivelacion =>
        {
            nivelacion.HasOne(item => item.Asignacion).WithMany().HasForeignKey(item => item.AsignacionId);
            nivelacion.HasOne(item => item.Periodo).WithMany().HasForeignKey(item => item.PeriodoId).OnDelete(DeleteBehavior.Restrict);
            nivelacion.HasOne(item => item.Estudiante).WithMany().HasForeignKey(item => item.EstudianteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ComentarioMateria>(comentario =>
        {
            comentario.HasIndex(item => new { item.AsignacionId, item.PeriodoId, item.EstudianteId }).IsUnique();
            comentario.Property(item => item.Texto).HasMaxLength(2000);
            comentario.HasOne(item => item.Asignacion).WithMany().HasForeignKey(item => item.AsignacionId);
            comentario.HasOne(item => item.Periodo).WithMany().HasForeignKey(item => item.PeriodoId).OnDelete(DeleteBehavior.Restrict);
            comentario.HasOne(item => item.Estudiante).WithMany().HasForeignKey(item => item.EstudianteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ComentarioOrientador>(comentario =>
        {
            comentario.HasIndex(item => new { item.GrupoId, item.PeriodoId, item.EstudianteId }).IsUnique();
            comentario.Property(item => item.Texto).HasMaxLength(2000);
            comentario.HasOne(item => item.Grupo).WithMany().HasForeignKey(item => item.GrupoId);
            comentario.HasOne(item => item.Periodo).WithMany().HasForeignKey(item => item.PeriodoId).OnDelete(DeleteBehavior.Restrict);
            comentario.HasOne(item => item.Estudiante).WithMany().HasForeignKey(item => item.EstudianteId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
