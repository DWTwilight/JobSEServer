using Elasticsearch.Net;
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

        public CompanyService(ILogger<CompanyService> logger, IOptions<ElasticOptions> options, ESClientManagerService esClientService)
        {
            this.logger = logger;
            this.options = options;

            //var settings = new ConnectionSettings(new Uri(options.Value.Url)).DefaultIndex(options.Value.CompanyIndexName).BasicAuthentication(options.Value.Username, options.Value.Password);
            this.client = esClientService.Client;
        }

        public async Task<Company> GetCompanyAsync(string id)
        {
            try
            {
                var response = await this.client.GetAsync<Company>(id, gd => gd.Index(options.Value.CompanyIndexName));
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

        public async Task<CompanyInfoList> SearchCompanyAsync(CompanyQuery query)
        {
            try
            {
                var response = await this.client.SearchAsync<Company>(s =>
                s.Index(options.Value.CompanyIndexName).Sort(sd => sd.Descending(SortSpecialField.Score)).From(query.Start).Size(query.Limit)
                .Query(q =>
                    {
                        var titleQ = new QueryContainer();
                        var tagQ = new QueryContainer();
                        if(query.Title != null)
                        {
                            titleQ = q.Match(qd => qd.Field(p => p.Name).Query(query.Title));
                        }

                        if(query.Tags != null)
                        {
                            foreach (var tag in query.Tags)
                            {
                                tagQ = tagQ || q.Term(p => p.Tags, tag);
                            }
                        }

                        return titleQ && tagQ;
                    }
                ).Highlight(hs =>
                hs.Fields(
                    fd => fd.Field(p => p.Name).Type(HighlighterType.Plain).PreTags("<span class=\"title highlight\">").PostTags("</span>"),
                    fd => fd.Field(p => p.Tags).Type(HighlighterType.Plain).PreTags("<span class=\"tag highlight\">").PostTags("</span>"))
                ).Source(sc => sc.Excludes(e => e.Field(p => p.Description))));

                if (!response.IsValid)
                {
                    throw new Exception("Unable To Search");
                }

                return new CompanyInfoList()
                {
                    CompanyList = response.Hits.Select(hit => new CompanyInfo()
                    {
                        Id = hit.Id,
                        Company = hit.Source,
                        Highlight = new CompanyHighlight(hit.Highlight)
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

        public async Task<IList<CompanyInfo>> GetSuggestionsAsync(string name)
        {
            try
            {
                var response = await this.client.SearchAsync<Company>(s =>
                s.Index(options.Value.CompanyIndexName).Sort(sd => sd.Descending(SortSpecialField.Score)).Size(5)
                .Query(q => q.Match(qd => qd.Field(p => p.Name).Query(name)) || q.Term(p => p.Tags, name))
                .Source(sc => sc.Excludes(e => e.Field(p => p.Description))));

                if (!response.IsValid)
                {
                    throw new Exception("Unable To Search");
                }

                return response.Hits.Select(hit => new CompanyInfo()
                {
                    Id = hit.Id,
                    Company = hit.Source
                }).ToList();
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }
    }
}
