export interface SesionIniciada {
  token: string;
  nombre: string;
  nombreUsuario: string;
  rol: string;
  colegioId: number | null;
  nombreColegio: string | null;
}

export interface ColegioCreado {
  id: number;
  nombre: string;
  nombreUsuario: string;
  activo: boolean;
}

export interface Rango {
  nombre: string;
  desde: number;
  hasta: number;
}

export interface Configuracion {
  notaMinimaAprobacion: number;
  mostrarDistanciaAlPromedio: boolean;
  mostrarDistanciaALaNotaMasAlta: boolean;
  rangos: Rango[];
}

export interface Periodo {
  id: number;
  numero: number;
  nombre: string;
  peso: number;
  cerrado: boolean;
}

export interface Anio {
  id: number;
  anio: number;
  periodos: Periodo[];
}

export interface Nombrado {
  id: number;
  nombre: string;
}

export interface Persona {
  id: number;
  nombre: string;
  nombreUsuario: string;
}

export interface Asignacion {
  id: number;
  materia: string;
  materiaId: number;
  profesor: string;
  profesorId: number;
}

export interface Grupo {
  id: number;
  nombre: string;
  grado: string;
  gradoId: number;
  orientadorId: number | null;
  orientador: string | null;
  estudiantes: Persona[];
  asignaciones: Asignacion[];
}

export interface AsignacionDelProfesor {
  id: number;
  grupo: string;
  grado: string;
  materia: string;
  anio: number;
  anioLectivoId: number;
}

export interface NotaPuesta {
  actividadId: number;
  nombre: string;
  porcentaje: number;
  valor: number | null;
}

export interface Nivelacion {
  notaAnterior: number | null;
  notaNueva: number;
  fecha: string;
}

export interface FilaEstudiante {
  estudianteId: number;
  nombre: string;
  nombreUsuario: string;
  notas: NotaPuesta[];
  notaDeActividades: number | null;
  notaDelPeriodo: number | null;
  nivelaciones: Nivelacion[];
  comentario: string;
}

export interface Planilla {
  asignacionId: number;
  periodoId: number;
  periodoCerrado: boolean;
  materia: string;
  grupo: string;
  grado: string;
  anio: number;
  nombrePeriodo: string;
  actividades: { id: number; nombre: string; porcentaje: number }[];
  estudiantes: FilaEstudiante[];
}

export interface GrupoOrientador {
  id: number;
  nombre: string;
  grado: string;
  anio: number;
  anioLectivoId: number;
  periodos: Periodo[];
}

export interface VistaOrientacion {
  grupo: string;
  nombrePeriodo: string;
  cerrado: boolean;
  estudiantes: {
    estudianteId: number;
    nombre: string;
    comentario: string;
    materias: { materia: string; nota: number | null; desempeno: string | null; comentarioProfesor: string }[];
  }[];
}

export interface AnioEstudiante {
  id: number;
  anio: number;
  grupo: string;
  periodos: Periodo[];
}

export interface MateriaPeriodo {
  materia: string;
  nota: number | null;
  desempeno: string | null;
  aprueba: boolean | null;
  comentario: string;
  promedioDelGrupo: number | null;
  distanciaAlPromedio: number | null;
  notaMasAlta: number | null;
  distanciaALaNotaMasAlta: number | null;
  actividades: NotaPuesta[];
  nivelaciones: Nivelacion[];
}

export interface BoletinPeriodo {
  periodoId: number;
  nombrePeriodo: string;
  anio: number;
  grupo: string;
  grado: string;
  colegio: string;
  estudiante: string;
  mostrarDistanciaAlPromedio: boolean;
  mostrarDistanciaALaNotaMasAlta: boolean;
  comentarioOrientador: string;
  materias: MateriaPeriodo[];
}

export interface BoletinAnio {
  anioLectivoId: number;
  anio: number;
  grupo: string;
  grado: string;
  colegio: string;
  estudiante: string;
  anioCerrado: boolean;
  mostrarDistanciaAlPromedio: boolean;
  mostrarDistanciaALaNotaMasAlta: boolean;
  periodos: { id: number; nombre: string; cerrado: boolean; comentarioOrientador: string; materias: MateriaPeriodo[] }[];
  materias: {
    materia: string;
    nota: number | null;
    desempeno: string | null;
    aprueba: boolean | null;
    promedioDelGrupo: number | null;
    distanciaAlPromedio: number | null;
    notaMasAlta: number | null;
    distanciaALaNotaMasAlta: number | null;
  }[];
}

export function nota(valor: number | null | undefined): string {
  return valor === null || valor === undefined ? '—' : valor.toFixed(2);
}

export function mensajeDe(error: unknown): string {
  const cuerpo = (error as { error?: { mensaje?: string } }).error;
  return cuerpo?.mensaje ?? 'No se pudo completar la acción.';
}
