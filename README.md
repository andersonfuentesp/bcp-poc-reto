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

**Instalar Azurite (PowerShell):**

* npm i -g azurite
* Ejecutar en local
* Arranca Azurite (consola aparte):

* mkdir C:\azurite
* cd C:\azurite
* azurite
* Déjalo abierto (escucha en 10000/10001/10002).

Ejecutar la solución en VS2022 y presionar F5.

Endpoints disponibles (el puerto puede variar según VS):

* GET http://localhost:7071/api/convert

* POST http://localhost:7071/api/rates

Seguridad (opcional)
Controlada por la clave API_TOKEN en local.settings.json.

Todas las llamadas HTTP requieren:

Authorization: Bearer <API_TOKEN>

Ejemplo local.settings.json:
```
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "API_TOKEN": "supersecreto"
  }
}
```
Para desactivar el token, borra la línea API_TOKEN y vuelve a ejecutar la Function.

Endpoints
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

Pruebas rápidas
Postman
Agregamos:

Authorization: Bearer supersecreto
Requests:

GET http://localhost:7071/api/convert?monto=100&monedaOrigen=PEN&monedaDestino=USD

POST http://localhost:7071/api/rates
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

Semilla de datos (seed)
Al iniciar la app se cargan pares comunes en memoria:

* PEN -> USD = 0.26
* USD -> PEN = 3.85
* USD -> EUR = 0.92
* EUR -> USD = 1.09

Puedes modificarlos vía POST /api/rates.

La DB no persiste entre reinicios (intencional para la POC).

Estructura del proyecto
```
BCP_POC/
  Storage/
    ExchangeRate.cs         (entidad)
    RatesDbContext.cs       (EF InMemory)
  Services/
    IRateService.cs         (contrato)
    RateService.cs          (lógica de tasas)
  Web/
    AuthMiddleware.cs       (token opcional)
    ConvertFunction.cs      (GET /convert)
    UpsertRateFunction.cs   (POST /rates)
  Program.cs                (DI, middleware, seed)
  host.json
  local.settings.json       (solo local)
  BCP_POC.csproj

```
Azure Function (v4, .NET 6, aislada) con dos endpoints: GET /convert y POST /rates.

Persistencia en EF Core InMemory (cumple “in memory database”).

Seguridad por token opcional activada con API_TOKEN.

Demo en Postman, 100% local con Azurite.

Escalable sin cambiar contratos: Function App + APIM + Key Vault (nube) o ASP.NET Core Web API on-prem.

Problemas comunes
401 Unauthorized
Falta el header Authorization o el token no coincide con API_TOKEN.

Solución rápida: quitar API_TOKEN y reiniciar la app.

Error de Storage / no inicia
Azurite no está corriendo. Abre consola y ejecuta azurite.

No aparecen rutas
Verifica los atributos [Function("Convert")] y [Function("UpsertRate")] y que el proyecto sea dotnet-isolated.
