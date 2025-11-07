using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using BCP_POC.Storage;
using BCP_POC.Services;
using BCP_POC.Web;

var builder = FunctionsApplication.CreateBuilder(args);

// Habilita HTTP en el worker (requerido por la plantilla nueva)
builder.ConfigureFunctionsWebApplication();

// Servicios de app (EF InMemory + capa de dominio)
builder.Services.AddDbContext<RatesDbContext>(opt => opt.UseInMemoryDatabase("Rates"));
builder.Services.AddScoped<IRateService, RateService>();

// Middleware de token (opcional: se activa si existe API_TOKEN)
builder.UseMiddleware<AuthMiddleware>();

// Si quisieras APM: habilítalo aquí (opcional)
// builder.Services.AddApplicationInsightsTelemetryWorkerService()
//        .ConfigureFunctionsApplicationInsights();

var host = builder.Build();

// Seed inicial de tipos de cambio (una sola vez al arrancar)
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RatesDbContext>();
    if (!db.Rates.Any())
    {
        db.Rates.AddRange(
            new ExchangeRate { From = "PEN", To = "USD", Rate = 0.26m },
            new ExchangeRate { From = "USD", To = "PEN", Rate = 3.85m },
            new ExchangeRate { From = "USD", To = "EUR", Rate = 0.92m },
            new ExchangeRate { From = "EUR", To = "USD", Rate = 1.09m }
        );
        db.SaveChanges();
    }
}

host.Run();