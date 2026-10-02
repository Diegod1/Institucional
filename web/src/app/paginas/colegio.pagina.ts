import { Component, inject, OnInit, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { Api } from '../api';
import { Anio, Configuracion, Grupo, mensajeDe, Nombrado, Persona } from '../modelos';

@Component({
  selector: 'app-colegio',
  imports: [FormsModule, MatButtonModule, MatCheckboxModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatTabsModule],
  templateUrl: './colegio.pagina.html'
})
export class ColegioPagina implements OnInit {
  private api = inject(Api);
  mensaje = signal('');
  error = signal('');
  configuracion: Configuracion | null = null;
  anios: Anio[] = [];
  anioSeleccionado = 0;
  nuevoAnio = new Date().getFullYear();
  cantidadPeriodos = 4;
  grados: Nombrado[] = [];
  materias: Nombrado[] = [];
  nombreGrado = '';
  nombreMateria = '';
  profesores: Persona[] = [];
  estudiantes: Persona[] = [];
  nombreProfesor = '';
  usuarioProfesor = '';
  contrasenaProfesor = '';
  nombreEstudiante = '';
  usuarioEstudiante = '';
  contrasenaEstudiante = '';
  grupos: Grupo[] = [];
  nombreGrupo = '';
  gradoId = 0;
  orientadorId = 0;
  grupoId = 0;
  estudianteId = 0;
  materiaId = 0;
  profesorId = 0;

  ngOnInit() {
    this.api.configuracion().subscribe(datos => this.configuracion = datos);
    this.recargarCatalogo();
    this.api.anios().subscribe(anios => {
      this.anios = anios;
      if (anios.length > 0) {
        this.anioSeleccionado = anios[0].id;
        this.cargarGrupos();
      }
    });
  }

  anioActual() {
    return this.anios.find(anio => anio.id === this.anioSeleccionado) ?? null;
  }

  grupoActual() {
    return this.grupos.find(grupo => grupo.id === this.grupoId) ?? null;
  }

  guardarConfiguracion() {
    if (!this.configuracion) return;
    this.actuar(this.api.guardarConfiguracion(this.configuracion), 'Configuración guardada.');
  }

  crearAnio() {
    this.actuar(this.api.crearAnio(Number(this.nuevoAnio), Number(this.cantidadPeriodos)), 'Año lectivo creado.', anio => {
      this.anios = [anio, ...this.anios];
      this.anioSeleccionado = anio.id;
      this.cargarGrupos();
    });
  }

  guardarPesos() {
    const anio = this.anioActual();
    if (!anio) return;
    const pesos = anio.periodos.map(periodo => ({ periodoId: periodo.id, peso: Number(periodo.peso) }));
    this.actuar(this.api.guardarPesos(anio.id, pesos), 'Pesos guardados.');
  }

  cerrar(periodoId: number) {
    if (!confirm('Al cerrar el periodo, los profesores ya no podrán cambiar notas ni subir la planilla.')) return;
    this.actuar(this.api.cerrarPeriodo(periodoId), 'Periodo cerrado.', () => {
      const periodo = this.anioActual()?.periodos.find(item => item.id === periodoId);
      if (periodo) periodo.cerrado = true;
    });
  }

  crearGrado() {
    this.actuar(this.api.crearGrado(this.nombreGrado), 'Grado creado.', grado => {
      this.grados = [...this.grados, grado];
      this.nombreGrado = '';
    });
  }

  crearMateria() {
    this.actuar(this.api.crearMateria(this.nombreMateria), 'Materia creada.', materia => {
      this.materias = [...this.materias, materia];
      this.nombreMateria = '';
    });
  }

  crearProfesor() {
    this.actuar(
      this.api.crearProfesor(this.nombreProfesor, this.usuarioProfesor, this.contrasenaProfesor),
      'Profesor creado.',
      profesor => {
        this.profesores = [...this.profesores, profesor];
        this.nombreProfesor = this.usuarioProfesor = this.contrasenaProfesor = '';
      }
    );
  }

  crearEstudiante() {
    this.actuar(
      this.api.crearEstudiante(this.nombreEstudiante, this.usuarioEstudiante, this.contrasenaEstudiante),
      'Estudiante creado.',
      estudiante => {
        this.estudiantes = [...this.estudiantes, estudiante];
        this.nombreEstudiante = this.usuarioEstudiante = this.contrasenaEstudiante = '';
      }
    );
  }

  cargarGrupos() {
    if (!this.anioSeleccionado) return;
    this.api.grupos(this.anioSeleccionado).subscribe(grupos => {
      this.grupos = grupos;
      this.grupoId = grupos[0]?.id ?? 0;
    });
  }

  crearGrupo() {
    this.actuar(
      this.api.crearGrupo(this.anioSeleccionado, Number(this.gradoId), this.nombreGrupo, Number(this.orientadorId)),
      'Grupo creado.',
      () => {
        this.nombreGrupo = '';
        this.cargarGrupos();
      }
    );
  }

  matricular() {
    this.actuar(this.api.matricular(this.grupoId, Number(this.estudianteId)), 'Estudiante matriculado.', () => this.cargarGrupos());
  }

  asignar() {
    this.actuar(
      this.api.asignar(this.grupoId, Number(this.materiaId), Number(this.profesorId)),
      'Profesor asignado a la materia.',
      () => this.cargarGrupos()
    );
  }

  private recargarCatalogo() {
    this.api.grados().subscribe(grados => this.grados = grados);
    this.api.materias().subscribe(materias => this.materias = materias);
    this.api.profesores().subscribe(profesores => this.profesores = profesores);
    this.api.estudiantes().subscribe(estudiantes => this.estudiantes = estudiantes);
  }

  private actuar<T>(llamada: Observable<T>, aviso: string, despues?: (valor: T) => void) {
    this.error.set('');
    this.mensaje.set('');
    llamada.subscribe({
      next: valor => {
        this.mensaje.set(aviso);
        despues?.(valor);
      },
      error: error => this.error.set(mensajeDe(error))
    });
  }
}
