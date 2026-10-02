import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { SesionIniciada } from './modelos';

@Injectable({ providedIn: 'root' })
export class Sesion {
  token = signal(localStorage.getItem('token') ?? '');
  nombre = signal(localStorage.getItem('nombre') ?? '');
  rol = signal(localStorage.getItem('rol') ?? '');
  nombreColegio = signal(localStorage.getItem('nombreColegio') ?? '');

  guardar(datos: SesionIniciada) {
    this.token.set(datos.token);
    this.nombre.set(datos.nombre);
    this.rol.set(datos.rol);
    this.nombreColegio.set(datos.nombreColegio ?? '');
    localStorage.setItem('token', datos.token);
    localStorage.setItem('nombre', datos.nombre);
    localStorage.setItem('rol', datos.rol);
    localStorage.setItem('nombreColegio', datos.nombreColegio ?? '');
  }

  salir() {
    localStorage.removeItem('token');
    localStorage.removeItem('nombre');
    localStorage.removeItem('rol');
    localStorage.removeItem('nombreColegio');
    this.token.set('');
    this.nombre.set('');
    this.rol.set('');
    this.nombreColegio.set('');
  }
}

export function destinoDe(rol: string): string {
  if (rol === 'AdministradorPlataforma') return '/plataforma';
  if (rol === 'Colegio') return '/colegio';
  if (rol === 'Profesor') return '/profesor';
  if (rol === 'Estudiante') return '/estudiante';
  return '/entrar';
}

export const tokenInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('token');
  const peticion = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
  return next(peticion).pipe(
    catchError((error: HttpErrorResponse) => {
      const mensaje = (error.error as { mensaje?: string } | null)?.mensaje;
      if (error.status === 403 && mensaje?.includes('inactivo')) {
        inject(Sesion).salir();
        inject(Router).navigateByUrl('/entrar');
      }
      return throwError(() => error);
    })
  );
};

export const sesionActiva: CanActivateFn = () => {
  const sesion = inject(Sesion);
  const router = inject(Router);
  return sesion.token() ? true : router.parseUrl('/entrar');
};

export const exigirRol = (rol: string): CanActivateFn => () => {
  const sesion = inject(Sesion);
  const router = inject(Router);
  return sesion.rol() === rol ? true : router.parseUrl(destinoDe(sesion.rol()));
};
