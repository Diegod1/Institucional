import { Routes } from '@angular/router';
import { EntrarPagina } from './paginas/entrar.pagina';
import { Marco } from './paginas/marco';
import { PlataformaPagina } from './paginas/plataforma.pagina';
import { ColegioPagina } from './paginas/colegio.pagina';
import { ProfesorPagina } from './paginas/profesor.pagina';
import { EstudiantePagina } from './paginas/estudiante.pagina';
import { exigirRol, sesionActiva } from './sesion';

export const routes: Routes = [
  { path: 'entrar', component: EntrarPagina },
  {
    path: '',
    component: Marco,
    canActivate: [sesionActiva],
    children: [
      { path: 'plataforma', component: PlataformaPagina, canActivate: [exigirRol('AdministradorPlataforma')] },
      { path: 'colegio', component: ColegioPagina, canActivate: [exigirRol('Colegio')] },
      { path: 'profesor', component: ProfesorPagina, canActivate: [exigirRol('Profesor')] },
      { path: 'estudiante', component: EstudiantePagina, canActivate: [exigirRol('Estudiante')] }
    ]
  },
  { path: '**', redirectTo: 'entrar' }
];
