using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BCP_POC.Web
{
    // Exige Authorization: Bearer <API_TOKEN> si API_TOKEN existe en settings
    public class AuthMiddleware : IFunctionsWorkerMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var expected = Environment.GetEnvironmentVariable("API_TOKEN");

            // Si no hay token configurado, no aplicamos seguridad (modo demo)
            if (string.IsNullOrWhiteSpace(expected))
            {
                await next(context);
                return;
            }

            // Obtiene la HttpRequestData de esta invocación (si la hay)
            var req = await context.GetHttpRequestDataAsync();
            if (req is null)
            {
                // No es una función HTTP → continuar
                await next(context);
                return;
            }

            // Validar header Authorization: Bearer <token>
            if (!req.Headers.TryGetValues("Authorization", out var values) ||
                !values.Any(v => v.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)))
            {
                var res = req.CreateResponse(HttpStatusCode.Unauthorized);
                await res.WriteAsJsonAsync(new { error = "Missing or invalid token" });
                context.GetInvocationResult().Value = res;
                return;
            }

            var token = values.First().Substring("Bearer ".Length).Trim();
            if (!string.Equals(token, expected, StringComparison.Ordinal))
            {
                var res = req.CreateResponse(HttpStatusCode.Unauthorized);
                await res.WriteAsJsonAsync(new { error = "Missing or invalid token" });
                context.GetInvocationResult().Value = res;
                return;
            }

            await next(context);
        }
    }
}
