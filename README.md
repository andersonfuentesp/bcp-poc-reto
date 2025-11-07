# BCP – POC (Azure Functions .NET 6)

Prueba de concepto para una casa de cambio: API mínima para convertir montos entre monedas y administrar el tipo de cambio.

Corre 100% local con Visual Studio 2022 + Azurite.

## Stack

* Azure Functions v4 – .NET 6 (modelo aislado)
* HTTP Trigger
* EF Core InMemory (DB en memoria para la POC)
* Token Bearer opcional vía middleware

## Requisitos

* Visual Studio 2022 (workloads: Desarrollo de Azure y .NET)
* .NET 6 Targeting Pack instalado en VS
* Node.js LTS (para Azurite)
* Azurite global

Estructura del proyecto
```
bcp-poc-reto/
├─ api/                               # Azure Functions (.NET 6, aislado)
│  ├─ BCP_POC.csproj
│  ├─ Program.cs
│  ├─ host.json
│  ├─ local.settings.json             # SOLO local
│  ├─ Services/
│  │  ├─ IRateService.cs
│  │  └─ RateService.cs
│  ├─ Storage/
│  │  ├─ ExchangeRate.cs
│  │  └─ RatesDbContext.cs
│  └─ Web/
│     ├─ AuthMiddleware.cs            # Token opcional (API_TOKEN)
│     ├─ ConvertFunction.cs           # GET  /api/convert
│     └─ UpsertRateFunction.cs        # POST /api/rates
│
├─ client/                            # Angular (frontend opcional)
│  ├─ src/
│  │  └─ app/
│  │     ├─ app.component.ts
│  │     └─ exchange.service.ts
│  ├─ angular.json
│  ├─ package.json
│  ├─ tsconfig.json
│  └─ proxy.conf.json                 # Proxy a Functions para evitar CORS
│
├─ BCP_POC.sln                        # Solución que referencia api/BCP_POC.csproj
├─ .gitignore
└─ README.md

```

**Instalar Azurite (PowerShell):**

* npm i -g azurite
* Arranca Azurite (consola aparte)
* mkdir C:\azurite -> cd C:\azurite -> azurite
* (escucha en 10000/10001/10002)

Ejecutar la solución en VS2022 y presionar F5.

Endpoints disponibles (el puerto puede variar según VS):

* GET http://localhost:7103/api/convert
* POST http://localhost:7103/api/rates

Seguridad: Controlada por la clave API_TOKEN en local.settings.json.

Todas las llamadas HTTP requieren: Authorization: Bearer <API_TOKEN>

Ejemplo local.settings.json:
```
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "API_TOKEN": "admin"
  }
}
```
Para desactivar el token, borramos la línea API_TOKEN y vuelve a ejecutar la Function.

## Endpoints
1) GET /api/convert
Convierte un monto usando la tasa registrada.

* Query params
* monto (decimal, requerido)
* monedaOrigen (string, requerido)
* monedaDestino (string, requerido)

Ejemplo

GET /api/convert?monto=120&monedaOrigen=PEN&monedaDestino=USD
```
{
  "monto": 120,
  "monedaOrigen": "PEN",
  "monedaDestino": "USD",
  "tipoCambio": 0.26,
  "montoConvertido": 31.2
}
```
Errores
* 400 – parámetros faltantes/incorrectos
* 404 – no existe el par monedaOrigen -> monedaDestino
* 401 – falta/incorrecto el token (si API_TOKEN está configurado)

2) POST /api/rates
Crea o actualiza una tasa de cambio (upsert).
```
{ "from": "PEN", "to": "USD", "rate": 0.2595 }
```
Respuesta
```
{ "from":"PEN", "to":"USD", "rate":0.2595, "updatedAt":"2025-11-07T03:21:45Z" }
```
Errores
* 400 – body inválido o rate <= 0
* 401 – falta/incorrecto el token (si API_TOKEN está configurado)

Pruebas (Postman) - Agregamos:

Authorization: Bearer admin
Requests:

GET http://localhost:7103/api/convert?monto=100&monedaOrigen=PEN&monedaDestino=USD

POST http://localhost:7103/api/rates
```
{"from":"PEN","to":"USD","rate":0.2595}
```

```
curl "http://localhost:7071/api/convert?monto=100&monedaOrigen=PEN&monedaDestino=USD" `
  -H "Authorization: Bearer admin"
```

```
curl -X POST "http://localhost:7071/api/rates" `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer supersecreto" `
  -d "{\"from\":\"PEN\",\"to\":\"USD\",\"rate\":0.2595}"
```

## Semilla de datos (seed)

Al iniciar la app se cargan pares comunes en memoria:

* PEN → USD = 0.26
* USD → PEN = 3.85
* USD → EUR = 0.92
* EUR → USD = 1.09

Puede modificarse vía POST /api/rates.
La DB no persiste entre reinicios (para la POC).

## Frontend (Angular opcional)

Este front minimal consume los 2 endpoints y corre en http://localhost:4200, proxyeando a la Function local para evitar CORS.

Requisitos (Node 18 → Angular 17):

* npx -y @angular/cli@17 new client --standalone --routing=false --style=scss --ssr=false --skip-git
* 
Proxy a la Function (ajusta el puerto si no es 7103):
Archivo client/proxy.conf.json:
```
{
  "/api": {
    "target": "http://localhost:7103",
    "secure": false,
    "changeOrigin": true
  }
}
```
En client/package.json agrega scripts:

```
"scripts": {
  "start": "ng serve --proxy-config proxy.conf.json",
  "build": "ng build"
}
```
Habilitar HttpClient (Angular ≥16):
client/src/main.ts

```
import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { AppComponent } from './app/app.component';
bootstrapApplication(AppComponent, { providers: [provideHttpClient()] });
```

Servicio que consume la API:
client/src/app/exchange.service.ts
```
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
@Injectable({ providedIn: 'root' })
export class ExchangeService {
  private base = '/api';
  private token = ''; // si la Function exige API_TOKEN, colócalo aquí
  constructor(private http: HttpClient) {}
  convert(monto: number, from: string, to: string) {
    const h = this.token ? new HttpHeaders().set('Authorization', `Bearer ${this.token}`) : undefined;
    return this.http.get<any>(`${this.base}/convert`, { headers: h, params: { monto, monedaOrigen: from, monedaDestino: to } });
  }
  upsert(from: string, to: string, rate: number) {
    const h = (this.token ? new HttpHeaders().set('Authorization', `Bearer ${this.token}`) : new HttpHeaders())
      .set('Content-Type','application/json');
    return this.http.post<any>(`${this.base}/rates`, { from, to, rate }, { headers: h });
  }
}
```

UI mínima (form de conversión y de tasa):
client/src/app/app.component.ts
```
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ExchangeService } from './exchange.service';
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h2>Conversor FX</h2>
    <form (ngSubmit)="go()" style="display:flex;gap:.5rem;flex-wrap:wrap">
      <input type="number" [(ngModel)]="monto" name="monto" placeholder="Monto" required>
      <input [(ngModel)]="from" name="from" placeholder="Origen (PEN)" required>
      <input [(ngModel)]="to" name="to" placeholder="Destino (USD)" required>
      <button type="submit">Convertir</button>
    </form>
    <pre *ngIf="resp">{{ resp | json }}</pre>
    <h3 style="margin-top:1rem">Actualizar tasa</h3>
    <form (ngSubmit)="save()" style="display:flex;gap:.5rem;flex-wrap:wrap">
      <input [(ngModel)]="ufrom" name="ufrom" placeholder="From (PEN)" required>
      <input [(ngModel)]="uto" name="uto" placeholder="To (USD)" required>
      <input type="number" step="0.0001" [(ngModel)]="urate" name="urate" placeholder="Rate" required>
      <button type="submit">Guardar</button>
    </form>
    <pre *ngIf="saved">{{ saved | json }}</pre>
  `
})
export class AppComponent {
  monto=120; from='PEN'; to='USD'; resp:any;
  ufrom='PEN'; uto='USD'; urate=0.2595; saved:any;
  constructor(private fx: ExchangeService) {}
  go(){ this.fx.convert(this.monto, this.from, this.to).subscribe({ next:r=>this.resp=r, error:e=>this.resp=e.error||e.message }); }
  save(){ this.fx.upsert(this.ufrom, this.uto, this.urate).subscribe({ next:r=>this.saved=r, error:e=>this.saved=e.error||e.message }); }
}
```

Cómo correr el front (con Azurite y backend):
```
cd client
npm install
npm run start
(abrir) http://localhost:4200
```

## Resumen técnico

Azure Function (v4, .NET 6, aislada) con dos endpoints:

* GET /api/convert
* POST /api/rates

- Persistencia en EF Core InMemory (cumple “in memory database”).
- Seguridad por token opcional activada con API_TOKEN.

## Problemas comunes

* 401 Unauthorized

Falta el header Authorization o el token no coincide con API_TOKEN.
* Solución rápida: quitar API_TOKEN y reiniciar la app.

Error de Storage / no inicia

* Azurite no está corriendo. Abre consola y ejecuta azurite.

No aparecen rutas

Atributos [Function("Convert")] y [Function("UpsertRate")]
