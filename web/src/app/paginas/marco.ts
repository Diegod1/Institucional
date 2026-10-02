import { Component, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { Sesion } from '../sesion';

@Component({
  selector: 'app-marco',
  imports: [RouterOutlet, MatButtonModule],
  template: `
    <header class="barra-superior">
      <div class="marca">
        <span class="sello">N</span>
        <span class="marca-texto">
          <span class="marca-nombre">NOTAS</span>
          <small>COLEGIO</small>
        </span>
      </div>
      <nav class="nav-pills">
        <span class="nav-pill activa">{{ rolVisible() }}</span>
        <span class="nav-pill">{{ titulo() }}</span>
      </nav>
      <div class="usuario-barra">
        <span>{{ sesion.nombre() }}</span>
        <button mat-stroked-button type="button" (click)="salir()">Salir</button>
      </div>
    </header>
    <router-outlet />
  `
})
export class Marco {
  sesion = inject(Sesion);
  private router = inject(Router);

  titulo() {
    return this.sesion.nombreColegio() || 'Plataforma de colegios';
  }

  rolVisible() {
    const roles: Record<string, string> = {
      AdministradorPlataforma: 'Colegios',
      Colegio: 'Panel',
      Profesor: 'Profesor',
      Estudiante: 'Estudiante'
    };
    return roles[this.sesion.rol()] ?? '';
  }

  salir() {
    this.sesion.salir();
    this.router.navigateByUrl('/entrar');
  }
}
