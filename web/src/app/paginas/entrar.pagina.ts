import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Api } from '../api';
import { mensajeDe } from '../modelos';
import { destinoDe, Sesion } from '../sesion';

@Component({
  selector: 'app-entrar',
  imports: [FormsModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <main class="entrar">
      <header class="barra-superior">
        <div class="marca">
          <span class="sello">N</span>
          <span class="marca-texto">
            <span class="marca-nombre">NOTAS</span>
            <small>COLEGIO</small>
          </span>
        </div>
      </header>
      <section class="entrar-form">
        <div class="entrar-caja">
          <h2>Ingresar</h2>
          <p>Usa el usuario y la contraseña que te asignaron.</p>
          @if (error()) { <p class="error">{{ error() }}</p> }
          <form (ngSubmit)="entrar()">
            <mat-form-field appearance="outline" style="width: 100%">
              <mat-label>Usuario</mat-label>
              <input matInput name="usuario" [(ngModel)]="usuario" autocomplete="username" required />
            </mat-form-field>
            <mat-form-field appearance="outline" style="width: 100%">
              <mat-label>Contraseña</mat-label>
              <input matInput type="password" name="contrasena" [(ngModel)]="contrasena" autocomplete="current-password" required />
            </mat-form-field>
            <button mat-flat-button color="primary" type="submit">Entrar</button>
          </form>
        </div>
      </section>
    </main>
  `
})
export class EntrarPagina {
  private api = inject(Api);
  private sesion = inject(Sesion);
  private router = inject(Router);
  usuario = '';
  contrasena = '';
  error = signal('');

  entrar() {
    this.error.set('');
    this.api.entrar(this.usuario, this.contrasena).subscribe({
      next: datos => {
        this.sesion.guardar(datos);
        this.router.navigateByUrl(destinoDe(datos.rol));
      },
      error: error => this.error.set(mensajeDe(error))
    });
  }
}
