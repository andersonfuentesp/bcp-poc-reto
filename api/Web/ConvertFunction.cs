using BCP_POC.Services;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCP_POC.Web
{
    public class ConvertFunction
    {
        private readonly ILogger _logger;
        private readonly IRateService _rates;

        public ConvertFunction(ILoggerFactory lf, IRateService rates)
        {
            _logger = lf.CreateLogger<ConvertFunction>();
            _rates = rates;
        }

        [Function("Convert")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "convert")]
            HttpRequestData req)
        {
            var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            if (!decimal.TryParse(q.Get("monto"), out var monto) ||
                string.IsNullOrWhiteSpace(q.Get("monedaOrigen")) ||
                string.IsNullOrWhiteSpace(q.Get("monedaDestino")))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteAsJsonAsync(new { error = "Parámetros: monto, monedaOrigen, monedaDestino" });
                return bad;
            }

            var from = q.Get("monedaOrigen")!;
            var to = q.Get("monedaDestino")!;

            var rate = await _rates.GetAsync(from, to, req.FunctionContext.CancellationToken);
            if (rate is null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteAsJsonAsync(new { error = $"No existe tipo de cambio {from}->{to}" });
                return notFound;
            }

            var result = new
            {
                monto,
                monedaOrigen = rate.From,
                monedaDestino = rate.To,
                tipoCambio = rate.Rate,
                montoConvertido = Math.Round(monto * rate.Rate, 4)
            };

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(result);
            return ok;
        }
    }
}
