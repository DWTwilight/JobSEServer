using JobSEServer.Models;
using JobSEServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobSEServer.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PositionController : ControllerBase
    {
        private readonly PositionService positionService;

        public PositionController(PositionService positionService)
        {
            this.positionService = positionService;
        }

        [HttpPost]
        public async Task<IActionResult> SearchAsync([FromForm] PositionQuery query)
        {
            try
            {
                return Ok(await positionService.SearchAsync(query));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        [Route("Relevant")]
        public async Task<IActionResult> SearchRelevantAsync([FromForm] RelevantPositionQuery query)
        {
            try
            {
                return Ok(await positionService.GetRelevantPositionInfoListAsync(query));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        [Route("Rate")]
        public async Task<IActionResult> RateAsync([FromForm] string id, [FromForm] double score)
        {
            try
            {
                await positionService.RateAsync(id, score);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDetailAsync(string id)
        {
            try
            {
                return Ok(await positionService.GetPositionDetailAsync(id));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
