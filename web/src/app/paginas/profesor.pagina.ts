import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Api } from '../api';
import { AsignacionDelProfesor, GrupoOrientador, mensajeDe, nota, Periodo, Planilla, VistaOrientacion } from '../modelos';

@Component({
  selector: 'app-profesor',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './profesor.pagina.html'
})
export class ProfesorPagina implements OnInit {
  private api = inject(Api);
  nota = nota;
  mensaje = signal('');
  error = signal('');
  asignaciones: AsignacionDelProfesor[] = [];
  asignacionId = 0;
  periodos: Periodo[] = [];
  periodoId = 0;
  planilla: Planilla | null = null;
  actividades: { nombre: string; porcentaje: number }[] = [];
  comentarioGeneral = '';
  nivelaciones: Record<number, number | null> = {};
  grupos: GrupoOrientador[] = [];
  grupoId = 0;
  periodoOrientacionId = 0;
  orientacion: VistaOrientacion | null = null;
  comentarioGrupo = '';

  ngOnInit() {
    this.api.asignaciones().subscribe(asignaciones => this.asignaciones = asignaciones);
    this.api.gruposDeOrientacion().subscribe(grupos => this.grupos = grupos);
  }

  elegirAsignacion() {
    this.planilla = null;
    this.api.periodosDeAsignacion(this.asignacionId).subscribe(periodos => {
      this.periodos = periodos;
      this.periodoId = periodos[0]?.id ?? 0;
      if (this.periodoId) this.cargarPlanilla();
    });
  }

  cargarPlanilla() {
    if (!this.asignacionId || !this.periodoId) return;
    this.api.planilla(this.asignacionId, this.periodoId).subscribe(planilla => {
      this.planilla = planilla;
      this.actividades = planilla.actividades.map(actividad => ({ nombre: actividad.nombre, porcentaje: actividad.porcentaje }));
      if (this.actividades.length === 0) this.actividades = [{ nombre: '', porcentaje: 100 }];
    });
  }

  agregarActividad() {
    this.actividades = [...this.actividades, { nombre: '', porcentaje: 0 }];
  }

  guardarActividades() {
    this.actuar(
      this.api.guardarActividades(this.asignacionId, this.periodoId, this.actividades.map(actividad => ({
        nombre: actividad.nombre,
        porcentaje: Number(actividad.porcentaje)
      }))),
      'Actividades guardadas. Deben sumar 100.',
      () => this.cargarPlanilla()
    );
  }

  guardarNotas() {
    if (!this.planilla) return;
    const notas = this.planilla.estudiantes.flatMap(estudiante =>
      estudiante.notas
        .filter(item => item.valor !== null && item.valor !== undefined)
        .map(item => ({ estudianteId: estudiante.estudianteId, actividadId: item.actividadId, valor: Number(item.valor) }))
    );
    this.actuar(this.api.guardarNotas(this.asignacionId, this.periodoId, notas), 'Notas guardadas.', () => this.cargarPlanilla());
  }

  guardarComentario(estudianteId: number | null, texto: string) {
    this.actuar(this.api.guardarComentario(this.asignacionId, this.periodoId, texto, estudianteId), 'Comentario guardado.', () => this.cargarPlanilla());
  }

  registrarNivelacion(estudianteId: number) {
    const valor = this.nivelaciones[estudianteId];
    if (valor === null || valor === undefined) return;
    this.actuar(this.api.nivelacion(this.asignacionId, this.periodoId, estudianteId, Number(valor)), 'Nivelación registrada.', () => this.cargarPlanilla());
  }

  descargarPlanilla() {
    this.api.descargar(`/api/profesor/asignaciones/${this.asignacionId}/periodos/${this.periodoId}/planilla`, 'planilla.xlsx');
  }

  descargarListado() {
    this.api.descargar('/api/profesor/listado', 'estudiantes.xlsx');
  }

  subir(evento: Event) {
    const archivo = (evento.target as HTMLInputElement).files?.[0];
    if (!archivo) return;
    this.actuar(this.api.subirPlanilla(this.asignacionId, this.periodoId, archivo), 'Planilla cargada.', () => this.cargarPlanilla());
  }

  periodosDelGrupo() {
    return this.grupos.find(grupo => grupo.id === this.grupoId)?.periodos ?? [];
  }

  cargarOrientacion() {
    if (!this.grupoId || !this.periodoOrientacionId) return;
    this.api.orientacion(this.grupoId, this.periodoOrientacionId).subscribe(vista => this.orientacion = vista);
  }

  guardarComentarioGrupo(estudianteId: number | null, texto: string) {
    this.actuar(
      this.api.comentarioOrientador(this.grupoId, this.periodoOrientacionId, texto, estudianteId),
      'Comentario del orientador guardado.',
      () => this.cargarOrientacion()
    );
  }

  private actuar(llamada: { subscribe: (observer: { next: (valor: unknown) => void; error: (error: unknown) => void }) => void }, aviso: string, despues?: () => void) {
    this.error.set('');
    this.mensaje.set('');
    llamada.subscribe({
      next: () => {
        this.mensaje.set(aviso);
        despues?.();
      },
      error: error => this.error.set(mensajeDe(error))
    });
  }
}
