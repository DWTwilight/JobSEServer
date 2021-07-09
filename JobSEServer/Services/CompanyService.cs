using JobSEServer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobSEServer.Services
{
    public class CompanyService
    {
        private readonly ILogger<CompanyService> logger;
        private readonly IOptions<ElasticOptions> options;
        private ElasticClient client;

        public CompanyService(ILogger<CompanyService> logger, IOptions<ElasticOptions> options)
        {
            this.logger = logger;
            this.options = options;

            var settings = new ConnectionSettings(new Uri(options.Value.Url)).DefaultIndex(options.Value.CompanyIndexName);
            this.client = new ElasticClient(settings);
        }

        public async Task<Company> GetCompanyAsync(string id)
        {
            try
            {
                var response = await this.client.GetAsync<Company>(id);
                if (!response.Found)
                {
                    throw new Exception("Company Not Found!");
                }
                return response.Source;
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return null;
            }
        }

        public async Task<CompanyInfoList> SearchCompanyAsync(string name, int start, int limit)
        {
            try
            {
                var response = await this.client.SearchAsync<Company>(s =>
                s.Sort(sd => sd.Descending(SortSpecialField.Score)).From(start).Size(limit)
                .Query(q => q.Match(qd => qd.Field(p => p.Name).Query(name)) || q.Term(p => p.Tags, name))
                .Source(sc => sc.Excludes(e => e.Field(p => p.Description))));

                if (!response.IsValid)
                {
                    throw new Exception("Unable To Search");
                }

                return new CompanyInfoList()
                {
                    CompanyList = response.Hits.Select(hit => new CompanyInfo()
                    {
                        Id = hit.Id,
                        Company = hit.Source
                    }).ToList(),
                    Total = response.Total
                };
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }
    }
}
