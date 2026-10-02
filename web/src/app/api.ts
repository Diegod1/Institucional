import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import {
  Anio,
  AnioEstudiante,
  Asignacion,
  AsignacionDelProfesor,
  BoletinAnio,
  BoletinPeriodo,
  ColegioCreado,
  Configuracion,
  Grupo,
  GrupoOrientador,
  Nombrado,
  Periodo,
  Persona,
  Planilla,
  SesionIniciada,
  VistaOrientacion
} from './modelos';

@Injectable({ providedIn: 'root' })
export class Api {
  private http = inject(HttpClient);

  entrar(nombreUsuario: string, contrasena: string) {
    return this.http.post<SesionIniciada>('/api/sesion', { nombreUsuario, contrasena });
  }

  colegios() {
    return this.http.get<ColegioCreado[]>('/api/colegios');
  }

  crearColegio(nombre: string, nombreResponsable: string, nombreUsuario: string, contrasena: string) {
    return this.http.post<ColegioCreado>('/api/colegios', { nombre, nombreResponsable, nombreUsuario, contrasena });
  }

  cambiarActivo(id: number, activo: boolean) {
    return this.http.put<ColegioCreado>(`/api/colegios/${id}/activo`, { activo });
  }

  configuracion() {
    return this.http.get<Configuracion>('/api/configuracion');
  }

  guardarConfiguracion(datos: Configuracion) {
    return this.http.put('/api/configuracion', datos);
  }

  anios() {
    return this.http.get<Anio[]>('/api/anios');
  }

  crearAnio(anio: number, cantidadPeriodos: number) {
    return this.http.post<Anio>('/api/anios', { anio, cantidadPeriodos });
  }

  guardarPesos(anioId: number, pesos: { periodoId: number; peso: number }[]) {
    return this.http.put(`/api/anios/${anioId}/pesos`, pesos);
  }

  cerrarPeriodo(periodoId: number) {
    return this.http.post(`/api/periodos/${periodoId}/cerrar`, {});
  }

  grados() {
    return this.http.get<Nombrado[]>('/api/grados');
  }

  crearGrado(nombre: string) {
    return this.http.post<Nombrado>('/api/grados', { nombre });
  }

  materias() {
    return this.http.get<Nombrado[]>('/api/materias');
  }

  crearMateria(nombre: string) {
    return this.http.post<Nombrado>('/api/materias', { nombre });
  }

  profesores() {
    return this.http.get<Persona[]>('/api/profesores');
  }

  crearProfesor(nombre: string, nombreUsuario: string, contrasena: string) {
    return this.http.post<Persona>('/api/profesores', { nombre, nombreUsuario, contrasena });
  }

  estudiantes() {
    return this.http.get<Persona[]>('/api/estudiantes');
  }

  crearEstudiante(nombre: string, nombreUsuario: string, contrasena: string) {
    return this.http.post<Persona>('/api/estudiantes', { nombre, nombreUsuario, contrasena });
  }

  grupos(anioId: number) {
    return this.http.get<Grupo[]>(`/api/anios/${anioId}/grupos`);
  }

  crearGrupo(anioLectivoId: number, gradoId: number, nombre: string, orientadorId: number) {
    return this.http.post<Grupo>('/api/grupos', { anioLectivoId, gradoId, nombre, orientadorId });
  }

  matricular(grupoId: number, estudianteId: number) {
    return this.http.post(`/api/grupos/${grupoId}/estudiantes`, { estudianteId });
  }

  asignar(grupoId: number, materiaId: number, profesorId: number) {
    return this.http.post<Asignacion>(`/api/grupos/${grupoId}/asignaciones`, { materiaId, profesorId });
  }

  asignaciones() {
    return this.http.get<AsignacionDelProfesor[]>('/api/profesor/asignaciones');
  }

  periodosDeAsignacion(asignacionId: number) {
    return this.http.get<Periodo[]>(`/api/profesor/asignaciones/${asignacionId}/periodos`);
  }

  planilla(asignacionId: number, periodoId: number) {
    return this.http.get<Planilla>(`/api/profesor/asignaciones/${asignacionId}/periodos/${periodoId}`);
  }

  guardarActividades(asignacionId: number, periodoId: number, actividades: { nombre: string; porcentaje: number }[]) {
    return this.http.put(`/api/profesor/asignaciones/${asignacionId}/periodos/${periodoId}/actividades`, actividades);
  }

  guardarNotas(asignacionId: number, periodoId: number, notas: { estudianteId: number; actividadId: number; valor: number }[]) {
    return this.http.put(`/api/profesor/asignaciones/${asignacionId}/periodos/${periodoId}/notas`, notas);
  }

  guardarComentario(asignacionId: number, periodoId: number, texto: string, estudianteId: number | null) {
    return this.http.put(`/api/profesor/asignaciones/${asignacionId}/periodos/${periodoId}/comentario`, { texto, estudianteId });
  }

  nivelacion(asignacionId: number, periodoId: number, estudianteId: number, nota: number) {
    return this.http.post(`/api/profesor/asignaciones/${asignacionId}/periodos/${periodoId}/nivelacion`, { estudianteId, nota });
  }

  subirPlanilla(asignacionId: number, periodoId: number, archivo: File) {
    const datos = new FormData();
    datos.append('archivo', archivo);
    return this.http.post(`/api/profesor/asignaciones/${asignacionId}/periodos/${periodoId}/planilla`, datos);
  }

  gruposDeOrientacion() {
    return this.http.get<GrupoOrientador[]>('/api/profesor/orientacion');
  }

  orientacion(grupoId: number, periodoId: number) {
    return this.http.get<VistaOrientacion>(`/api/profesor/orientacion/${grupoId}/periodos/${periodoId}`);
  }

  comentarioOrientador(grupoId: number, periodoId: number, texto: string, estudianteId: number | null) {
    return this.http.put(`/api/profesor/orientacion/${grupoId}/periodos/${periodoId}/comentario`, { texto, estudianteId });
  }

  aniosEstudiante() {
    return this.http.get<AnioEstudiante[]>('/api/estudiante/anios');
  }

  boletinPeriodo(periodoId: number) {
    return this.http.get<BoletinPeriodo>(`/api/estudiante/periodos/${periodoId}`);
  }

  boletinAnio(anioId: number) {
    return this.http.get<BoletinAnio>(`/api/estudiante/anios/${anioId}`);
  }

  descargar(url: string, nombre: string) {
    this.http.get(url, { responseType: 'blob' }).subscribe(blob => {
      const enlace = document.createElement('a');
      enlace.href = URL.createObjectURL(blob);
      enlace.download = nombre;
      enlace.click();
      URL.revokeObjectURL(enlace.href);
    });
  }
}
