import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { Api } from '../api';
import { AnioEstudiante, BoletinAnio, BoletinPeriodo, mensajeDe, nota } from '../modelos';

@Component({
  selector: 'app-estudiante',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatSelectModule],
  templateUrl: './estudiante.pagina.html'
})
export class EstudiantePagina implements OnInit {
  private api = inject(Api);
  nota = nota;
  error = signal('');
  anios: AnioEstudiante[] = [];
  anioId = 0;
  periodoId = 0;
  boletin: BoletinPeriodo | null = null;
  boletinAnio: BoletinAnio | null = null;

  ngOnInit() {
    this.api.aniosEstudiante().subscribe({
      next: anios => {
        this.anios = anios;
        this.anioId = anios[0]?.id ?? 0;
        this.periodoId = anios[0]?.periodos[0]?.id ?? 0;
        if (this.periodoId) this.verPeriodo();
      },
      error: error => this.error.set(mensajeDe(error))
    });
  }

  anioActual() {
    return this.anios.find(anio => anio.id === this.anioId) ?? null;
  }

  verPeriodo() {
    this.boletinAnio = null;
    this.api.boletinPeriodo(this.periodoId).subscribe({
      next: boletin => this.boletin = boletin,
      error: error => this.error.set(mensajeDe(error))
    });
  }

  verAnio() {
    this.boletin = null;
    this.api.boletinAnio(this.anioId).subscribe({
      next: boletin => this.boletinAnio = boletin,
      error: error => this.error.set(mensajeDe(error))
    });
  }

  pdfPeriodo() {
    this.api.descargar(`/api/estudiante/periodos/${this.periodoId}/pdf`, 'boletin-periodo.pdf');
  }

  pdfAnio() {
    this.api.descargar(`/api/estudiante/anios/${this.anioId}/pdf`, 'boletin-anio.pdf');
  }

  distanciaAlPromedio(valor: number | null) {
    if (valor === null) return '—';
    if (valor > 0) return `${valor.toFixed(2)} por encima del promedio`;
    if (valor < 0) return `${Math.abs(valor).toFixed(2)} por debajo del promedio`;
    return 'Igual al promedio';
  }

  distanciaALaAlta(valor: number | null) {
    if (valor === null) return '—';
    if (valor === 0) return 'Es la nota más alta';
    return `A ${valor.toFixed(2)} de la nota más alta`;
  }
}
