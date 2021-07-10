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
    [Route("api/[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {
        private readonly ElasticService elasticService;

        public DataController(ElasticService elasticService)
        {
            this.elasticService = elasticService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateIndexAsync()
        {
            try
            {
                await elasticService.CreateIndexAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        [Route("Test")]
        public async Task<IActionResult> InsertTestDataAsync()
        {
            try
            {
                await elasticService.InsertTestDataAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
