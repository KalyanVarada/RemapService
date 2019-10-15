using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemapService
{
    public class RemapDTO
    {
        public string Envoirment { get; set; }
        public string OldOrgXRefId { get; set; }
        public string NewOrgXRefId { get; set; }
        public string BearerToken { get; set; }
        public string Email { get; set; }
       
    }
}
