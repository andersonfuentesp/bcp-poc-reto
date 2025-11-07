using BCP_POC.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BCP_POC.Web
{
    public class UpsertRateFunction
    {
        private readonly IRateService _rates;
        public UpsertRateFunction(IRateService rates) => _rates = rates;

        public record RateDto([property: JsonPropertyName("from")] string From,
                              [property: JsonPropertyName("to")] string To,
                              [property: JsonPropertyName("rate")] decimal Rate);

        [Function("UpsertRate")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rates")]
            HttpRequestData req)
        {
            var dto = await JsonSerializer.DeserializeAsync<RateDto>(req.Body);
            if (dto is null || string.IsNullOrWhiteSpace(dto.From) ||
                string.IsNullOrWhiteSpace(dto.To) || dto.Rate <= 0)
            {
                var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await bad.WriteAsJsonAsync(new { error = "Body: {\"from\":\"PEN\",\"to\":\"USD\",\"rate\":0.26}" });
                return bad;
            }

            var entity = await _rates.UpsertAsync(dto.From, dto.To, dto.Rate, req.FunctionContext.CancellationToken);

            var ok = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(new
            {
                from = entity.From,
                to = entity.To,
                rate = entity.Rate,
                updatedAt = entity.UpdatedAt
            });
            return ok;
        }
    }
}
