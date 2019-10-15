using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemapService.Models
{
    public class Envoirment
    {
        public List<Setting> Setting { get; set; }

    }
    public class Setting
    {
        public string Name { get; set; }
        public string ProductServiceUrl { get; set; }
        public string ConnectionString { get; set; }
    }
}
