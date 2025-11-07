import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ExchangeService } from './exchange.service';

type FxResp = {
  monto: number;
  monedaOrigen: string;
  monedaDestino: string;
  tipoCambio: number;
  montoConvertido: number;
};

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
  <div class="container">
    <div class="h1">
      BCP – FX Demo
      <span class="badge">Azure Functions • .NET 6</span>
      <span class="state" [class.ok]="backendOk" [class.err]="!backendOk">
        {{ backendOk ? 'backend listo' : backendMsg }}
      </span>
    </div>
    <div class="subtle" style="margin-top:4px;">Conversión de moneda y mantenimiento de tipo de cambio (POC local)</div>

    <div class="grid" style="margin-top:18px;">
      <!-- Conversión -->
      <div class="card">
        <h3>Conversión</h3>
        <div class="row">
          <div>
            <label>Monto</label>
            <input class="input" type="number" [(ngModel)]="monto" name="monto" min="0" step="0.01" placeholder="Ej: 120">
          </div>
          <div>
            <label>Tipo de cambio (ref.)</label>
            <div class="kbd">{{ hintPair }}</div>
          </div>
        </div>

        <div class="row-3" style="margin-top:10px;">
          <div>
            <label>Moneda origen</label>
            <select class="input" [(ngModel)]="from">
              <option *ngFor="let c of currencies" [value]="c">{{ c }}</option>
            </select>
          </div>
          <div>
            <label>Moneda destino</label>
            <select class="input" [(ngModel)]="to">
              <option *ngFor="let c of currencies" [value]="c">{{ c }}</option>
            </select>
          </div>
          <div style="align-self:end;">
            <button class="btn" (click)="go()">Convertir</button>
          </div>
        </div>

        <div *ngIf="loadingConvert" class="subtle loading" style="margin-top:10px;">Convirtiendo…</div>
        <div *ngIf="resp && !loadingConvert" style="margin-top:12px;">
          <div class="kbd">Resultado</div>
          <pre class="pre">{{ resp | json }}</pre>
        </div>
        <div *ngIf="errConvert && !loadingConvert" style="margin-top:12px;">
          <div class="kbd" style="color:#fda4af;border-color:#3b1b23;background:#1a0f14;">Error</div>
          <pre class="pre" style="border-color:#3b1b23;color:#fca5a5">{{ errConvert | json }}</pre>
        </div>
      </div>

      <!-- Mantenimiento de tasa -->
      <div class="card">
        <h3>Administrar tipo de cambio</h3>
        <div class="row-3">
          <div>
            <label>From</label>
            <select class="input" [(ngModel)]="ufrom">
              <option *ngFor="let c of currencies" [value]="c">{{ c }}</option>
            </select>
          </div>
          <div>
            <label>To</label>
            <select class="input" [(ngModel)]="uto">
              <option *ngFor="let c of currencies" [value]="c">{{ c }}</option>
            </select>
          </div>
          <div>
            <label>Rate</label>
            <input class="input" type="number" step="0.0001" [(ngModel)]="urate" placeholder="Ej: 0.2595">
          </div>
        </div>

        <div style="margin-top:10px;">
          <button class="btn" (click)="save()">Guardar tasa</button>
        </div>

        <div *ngIf="loadingSave" class="subtle loading" style="margin-top:10px;">Guardando…</div>
        <div *ngIf="saved && !loadingSave" style="margin-top:12px;">
          <div class="kbd">Guardado</div>
          <pre class="pre">{{ saved | json }}</pre>
        </div>
        <div *ngIf="errSave && !loadingSave" style="margin-top:12px;">
          <div class="kbd" style="color:#fda4af;border-color:#3b1b23;background:#1a0f14;">Error</div>
          <pre class="pre" style="border-color:#3b1b23;color:#fca5a5">{{ errSave | json }}</pre>
        </div>
      </div>
    </div>

    <div class="footer">
      Pruebas vía Postman/Angular · Seguridad por token
    </div>
  </div>
  `
})
export class AppComponent {
  // UI state
  backendOk = false;
  backendMsg = 'conectando…';
  currencies = ['PEN','USD','EUR'];

  // Conversión
  monto = 120;
  from  = 'PEN';
  to    = 'USD';
  resp: FxResp | any = null;
  errConvert: any = null;
  loadingConvert = false;

  // Upsert rate
  ufrom = 'PEN';
  uto   = 'USD';
  urate = 0.2595;
  saved: any = null;
  errSave: any = null;
  loadingSave = false;

  constructor(private fx: ExchangeService) {
    // Health-check simple (intenta una conversión con seed)
    setTimeout(() => this.go(true), 200);
  }

  get hintPair(){ return `${this.from} → ${this.to}`; }

  go(isHealth=false) {
    this.loadingConvert = !isHealth;
    this.errConvert = null;
    if (!isHealth) this.resp = null;

    this.fx.convert(this.monto, this.from, this.to).subscribe({
      next: r => {
        this.resp = r; 
        this.backendOk = true; 
        this.backendMsg = 'backend listo';
        if (isHealth) this.loadingConvert = false;
        console.log('convert OK', r);
      },
      error: e => {
        const err = (e && e.error) ? e.error : e.message || e;
        this.errConvert = err;
        this.backendOk = false;
        this.backendMsg = 'sin conexión o 401';
        console.error('convert ERR', e);
      },
      complete: () => { if (!isHealth) this.loadingConvert = false; }
    });
  }

  save() {
    this.loadingSave = true;
    this.errSave = null;
    this.saved = null;

    this.fx.upsert(this.ufrom, this.uto, this.urate).subscribe({
      next: r => { this.saved = r; console.log('upsert OK', r); },
      error: e => {
        this.errSave = (e && e.error) ? e.error : e.message || e;
        console.error('upsert ERR', e);
      },
      complete: () => { this.loadingSave = false; }
    });
  }
}