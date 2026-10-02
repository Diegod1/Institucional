import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Api } from '../api';
import { ColegioCreado, mensajeDe } from '../modelos';

@Component({
  selector: 'app-plataforma',
  imports: [FormsModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <main class="pagina">
      <header class="encabezado">
        <div>
          <p class="kicker">Plataforma</p>
          <h1>Colegios</h1>
          <p>Crea cada colegio y decide si puede entrar.</p>
        </div>
      </header>
      @if (mensaje()) { <p class="aviso">{{ mensaje() }}</p> }
      @if (error()) { <p class="error">{{ error() }}</p> }
      <section class="tarjeta">
        <h2>Nuevo colegio</h2>
        <p>El responsable entra con el usuario que definas aquí.</p>
        <form class="rejilla" (ngSubmit)="crear()">
          <mat-form-field appearance="outline"><mat-label>Colegio</mat-label><input matInput name="nombre" [(ngModel)]="nombre" required /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Responsable</mat-label><input matInput name="responsable" [(ngModel)]="responsable" required /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Usuario</mat-label><input matInput name="usuario" [(ngModel)]="usuario" required /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Contraseña</mat-label><input matInput type="password" name="contrasena" [(ngModel)]="contrasena" required /></mat-form-field>
          <button mat-flat-button color="primary" type="submit">Crear colegio</button>
        </form>
      </section>
      @if (colegios().length === 0) {
        <p class="vacio">Todavía no hay colegios. El primero aparece aquí apenas lo crees.</p>
      } @else {
        <label class="buscador">
          <span>Buscar colegio</span>
          <input
            class="campo"
            type="search"
            name="busqueda"
            placeholder="Nombre o usuario"
            [ngModel]="busqueda()"
            (ngModelChange)="busqueda.set($event)" />
        </label>
        @if (visibles().length === 0) {
          <p class="vacio">Ningún colegio coincide con «{{ busqueda() }}».</p>
        } @else {
          <section class="cuadricula">
            @for (colegio of visibles(); track colegio.id) {
              <article class="ficha">
                <div class="ficha-marca">{{ inicial(colegio.nombre) }}</div>
                <h2>{{ colegio.nombre }}</h2>
                <p class="precio">{{ colegio.nombreUsuario }}</p>
                <p class="categoria">{{ colegio.activo ? 'Activo' : 'Inactivo' }}</p>
                <div class="ficha-acciones">
                  <button mat-stroked-button type="button" (click)="cambiar(colegio)">{{ colegio.activo ? 'Inactivar' : 'Activar' }}</button>
                </div>
              </article>
            }
          </section>
        }
      }
    </main>
  `
})
export class PlataformaPagina implements OnInit {
  private api = inject(Api);
  colegios = signal<ColegioCreado[]>([]);
  busqueda = signal('');
  visibles = computed(() => this.colegios().filter(colegio => coincide(colegio, this.busqueda())));
  nombre = '';
  responsable = '';
  usuario = '';
  contrasena = '';
  mensaje = signal('');
  error = signal('');

  ngOnInit() {
    this.cargar();
  }

  inicial(nombre: string) {
    return nombre.trim().charAt(0).toUpperCase();
  }

  cambiar(colegio: ColegioCreado) {
    const activo = !colegio.activo;
    this.error.set('');
    this.mensaje.set('');
    this.api.cambiarActivo(colegio.id, activo).subscribe({
      next: () => {
        this.mensaje.set(activo
          ? 'Colegio activado. Sus usuarios ya pueden entrar.'
          : 'Colegio inactivado. Sus usuarios ya no pueden entrar.');
        this.cargar();
      },
      error: error => this.error.set(mensajeDe(error))
    });
  }

  cargar() {
    this.api.colegios().subscribe({
      next: colegios => this.colegios.set(colegios),
      error: error => this.error.set(mensajeDe(error))
    });
  }

  crear() {
    this.error.set('');
    this.mensaje.set('');
    this.api.crearColegio(this.nombre, this.responsable, this.usuario, this.contrasena).subscribe({
      next: () => {
        this.mensaje.set('Colegio creado. Ya puede entrar con su usuario.');
        this.nombre = this.responsable = this.usuario = this.contrasena = '';
        this.cargar();
      },
      error: error => this.error.set(mensajeDe(error))
    });
  }
}

function coincide(colegio: ColegioCreado, busqueda: string) {
  const texto = plano(busqueda);
  if (!texto) return true;
  return plano(colegio.nombre).includes(texto) || plano(colegio.nombreUsuario).includes(texto);
}

function plano(valor: string) {
  return valor.normalize('NFD').replace(/\p{M}/gu, '').toLocaleLowerCase();
}
