using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCP_POC.Storage
{
    public class ExchangeRate
    {
        public int Id { get; set; }
        public string From { get; set; } = default!;
        public string To { get; set; } = default!;
        public decimal Rate { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}