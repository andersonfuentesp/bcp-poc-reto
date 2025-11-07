using BCP_POC.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCP_POC.Services
{
    public interface IRateService
    {
        Task<ExchangeRate?> GetAsync(string from, string to, CancellationToken ct);
        Task<ExchangeRate> UpsertAsync(string from, string to, decimal rate, CancellationToken ct);
    }
}
