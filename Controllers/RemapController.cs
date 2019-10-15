using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RemapService.Models;

namespace RemapService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RemapController : ControllerBase
    {
        private readonly IOptions<Envoirment> _envoirment;
        public RemapController(IOptions<Envoirment> envoirment)
        {
            this._envoirment = envoirment;
        }
        // POST api/values
        [HttpPost]
        public async Task<IActionResult> Post(RemapDTO remapRequest)
        {
            try
            {
                await new RemapHandler().ProcessRemap(remapRequest, _envoirment);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        
    }
}
