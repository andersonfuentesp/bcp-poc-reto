using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCP_POC.Storage
{
    public class RatesDbContext : DbContext
    {
        public RatesDbContext(DbContextOptions<RatesDbContext> options) : base(options) { }
        public DbSet<ExchangeRate> Rates => Set<ExchangeRate>();
    }
}
