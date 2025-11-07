using BCP_POC.Storage;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCP_POC.Services
{
    public class RateService : IRateService
    {
        private readonly RatesDbContext _db;
        public RateService(RatesDbContext db) => _db = db;

        public async Task<ExchangeRate?> GetAsync(string from, string to, CancellationToken ct)
        {
            from = from.ToUpperInvariant();
            to = to.ToUpperInvariant();
            return await _db.Rates.FirstOrDefaultAsync(r => r.From == from && r.To == to, ct);
        }

        public async Task<ExchangeRate> UpsertAsync(string from, string to, decimal rate, CancellationToken ct)
        {
            from = from.ToUpperInvariant();
            to = to.ToUpperInvariant();

            var entity = await _db.Rates.FirstOrDefaultAsync(r => r.From == from && r.To == to, ct);
            if (entity is null)
            {
                entity = new ExchangeRate { From = from, To = to, Rate = rate, UpdatedAt = DateTime.UtcNow };
                _db.Rates.Add(entity);
            }
            else
            {
                entity.Rate = rate;
                entity.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(ct);
            return entity;
        }
    }
}
