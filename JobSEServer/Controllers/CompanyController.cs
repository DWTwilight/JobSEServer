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
    public class CompanyController : ControllerBase
    {
        private readonly CompanyService companyService;
        private readonly PositionService positionService;

        public CompanyController(CompanyService companyService, PositionService positionService)
        {
            this.companyService = companyService;
            this.positionService = positionService;
        }

        /// <summary>
        /// 获取公司详细信息，不包括职位
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetCompanyDetailAsync(string id)
        {
            try
            {
                return Ok(await companyService.GetCompanyAsync(id));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// 搜公司
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="start"></param>
        /// <param name="limit"></param>
        /// <returns>公司列表，简略信息</returns>
        [HttpPost]
        [Route("Search")]
        public async Task<IActionResult> SearchCompanyAsync([FromForm] CompanyQuery query)
        {
            try
            {
                return Ok(await companyService.SearchCompanyAsync(query));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [Route("Suggest")]
        public async Task<IActionResult> GetSuggestionsAsync(string name)
        {
            try
            {
                return Ok(await companyService.GetSuggestionsAsync(name));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// 获取职位列表
        /// </summary>
        /// <param name="id"></param>
        /// <param name="start"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Positions")]
        public async Task<IActionResult> GetPositionsAsync(string id, int start, int limit)
        {
            try
            {
                return Ok(await positionService.GetCompanyPositionListAsync(id, start, limit));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
