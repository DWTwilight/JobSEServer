using Elasticsearch.Net;
using JobSEServer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobSEServer.Services
{
    public class PositionService
    {
        private readonly ILogger<PositionService> logger;
        private readonly CompanyService companyService;
        private readonly TagService tagService;
        private readonly IOptions<ElasticOptions> options;
        private ElasticClient client;

        public PositionService(ILogger<PositionService> logger, CompanyService companyService, TagService tagService, IOptions<ElasticOptions> options, ESClientManagerService esClientService)
        {
            this.logger = logger;
            this.companyService = companyService;
            this.tagService = tagService;
            this.options = options;

            //var settings = new ConnectionSettings(new Uri(options.Value.Url)).DefaultIndex(options.Value.PositionIndexName);
            this.client = esClientService.Client;//new ElasticClient(settings);
        }

        /// <summary>
        /// 搜索职位
        /// </summary>
        /// <param name="query">搜索参数</param>
        /// <returns>搜索结果，公司信息</returns>
        public async Task<PositionQueryRes> SearchAsync(PositionQuery query)
        {
            try
            {
                _ = this.tagService.CreateOrUpdateTagsAsync(query.Tags);
                var pList = await this.GetPositionInfoListAsync(query);

                var companies = new Dictionary<string, Company>();
                foreach (var p in pList.Positions)
                {
                    var cid = p.Position.CompanyId;
                    if (!companies.ContainsKey(cid))
                    {
                        var c = await companyService.GetCompanyAsync(cid);
                        c.Description = null;
                        companies.Add(cid, c);
                    }
                }

                return new PositionQueryRes()
                {
                    PositionList = pList,
                    Companies = companies
                };
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }

        /// <summary>
        /// 获取职位详情
        /// </summary>
        /// <param name="id">职位id</param>
        /// <returns>职位详情、公司信息</returns>
        public async Task<PositionDetail> GetPositionDetailAsync(string id)
        {
            try
            {
                var position = await this.GetPositionAsync(id);
                //Update Views
                position.Views++;

                _ = this.UpdateViewsAsync(position, id);

                return new PositionDetail()
                {
                    Position = position,
                    Company = await companyService.GetCompanyAsync(position.CompanyId)
                };
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }

        private async Task UpdateViewsAsync(Position position, string id)
        {
            try
            {
                var response = await this.client.UpdateAsync<Position>(id, op => op.Index(options.Value.PositionIndexName).Doc(position));
                if (!response.IsValid)
                {
                    throw new Exception("Unable To Update Views!\n" + response.DebugInformation);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }
        }

        public async Task RateAsync(string id, double score)
        {
            try
            {
                var position = await this.GetPositionAsync(id);
                if(position.Views > 0)
                {
                    position.Rating = (position.Rating * (position.Views - 1) + score) / position.Views;
                }
                else
                {
                    position.Rating = score;
                }
                var response = await this.client.UpdateAsync<Position>(id, op => op.Index(options.Value.PositionIndexName).Doc(position));
                if (!response.IsValid)
                {
                    throw new Exception("Unable To Update Views!");
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }


        /// <summary>
        /// 获取相关职位
        /// </summary>
        /// <param name="query">搜索参数</param>
        /// <returns>相关职位列表，无公司信息</returns>
        public async Task<IList<PositionInfo>> GetRelevantPositionInfoListAsync(RelevantPositionQuery query)
        {
            try
            {
                var response = await this.client.SearchAsync<Position>(s =>
                s.Index(options.Value.PositionIndexName).Sort(sd => sd.Descending(SortSpecialField.Score)).Size(query.Limit)
                .Query(q => q.Match(qd => qd.Field(p => p.Title).Query(query.Title)) && !q.Ids(p => p.Values(query.Exclude)))
                .Source(sc => sc.Excludes(e => e.Fields(p => p.Description)))
                );

                if (!response.IsValid)
                {
                    throw new Exception("Unable To Search");
                }

                return response.Hits.Select(hit => new PositionInfo()
                {
                    Id = hit.Id,
                    Position = hit.Source
                }).ToList();
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }

        /// <summary>
        /// 获取公司职位列表（无描述和要求）
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="start"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public async Task<PositionInfoList> GetCompanyPositionListAsync(string companyId, int start, int limit)
        {
            try
            {
                var response = await this.client.SearchAsync<Position>(s =>
                s.Index(options.Value.PositionIndexName).Sort(sd => sd.Descending(p => p.UpdateTime)).From(start).Size(limit)
                .Query(q => q.ConstantScore(qd => qd.Filter(qcd => qcd.Term(p => p.CompanyId, companyId))))
                .Source(sc => sc.Excludes(e => e.Fields(p => p.Description.Description)))
                );

                if (!response.IsValid)
                {
                    throw new Exception("Unable To Search");
                }

                return new PositionInfoList()
                {
                    Positions = response.Hits.Select(hit => new PositionInfo()
                    {
                        Id = hit.Id,
                        Position = hit.Source
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

        public async Task<IList<PositionSuggestion>> GetSuggestionsAsync(string keyWord)
        {
            try
            {
                if (string.IsNullOrEmpty(keyWord))
                {
                    throw new Exception("Value Cannot Be NULL!");
                }
                var response = await this.client.SearchAsync<Position>(s =>
                s.Index(options.Value.PositionIndexName).Sort(sd => sd.Descending(SortSpecialField.Score)).Size(5)
                .Query(q => q.Match(qd => qd.Field(p => p.Title).Query(keyWord)))
                .Source(sc => sc.Includes(i => i.Fields(p => p.Title, p => p.Views)))
                );

                if (!response.IsValid)
                {
                    throw new Exception("Unable To Search\n" + response.DebugInformation);
                }

                return response.Hits.Select(hit => new PositionSuggestion()
                {
                    Id = hit.Id,
                    Title = hit.Source.Title,
                    Views = hit.Source.Views
                }).ToList();
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }

        public async Task<long> GetPositionCountAsync()
        {
            try
            {
                var response = await this.client.CountAsync<Position>(cd => cd.Index(options.Value.PositionIndexName));

                if (!response.IsValid)
                {
                    throw new Exception(response.DebugInformation);
                }

                return response.Count;
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }

        public async Task<CompanyStatistics> GetCompanyStatisticsAsync(string companyId)
        {
            try
            {
                var res = new CompanyStatistics();

                var response = await this.client.SearchAsync<Position>(sd => sd.Index(options.Value.PositionIndexName).Size(0).Query(q => q.Term(p => p.CompanyId, companyId))
                .Aggregations(acd => acd
                .WeightedAverage("average_salary", waad => waad.Value(v => v.Script("(doc['salary.amount.gte'].value+doc['salary.amount.lte'].value)/2")).Weight(w => w.Field(p => p.Salary.Provided)))
                .Average("average_rating", aad => aad.Field(p => p.Rating).Missing(2.5))
                .Average("average_views", aad => aad.Field(p => p.Views).Missing(0))
                .Terms("tags", tad => tad.Field(p => p.Description.Tags).Size(50))
                .Range("salary_range", rad => rad.Script("if(doc['salary.provided'].value){ return (doc['salary.amount.gte'].value+doc['salary.amount.lte'].value)/2} return -1").Ranges(
                    ard => ard.To(0),
                    ard => ard.From(0).To(3000),
                    ard => ard.From(3000).To(5000),
                    ard => ard.From(5000).To(10000),
                    ard => ard.From(10000).To(15000),
                    ard => ard.From(15000).To(25000),
                    ard => ard.From(25000)
                    ))));

                if (!response.IsValid)
                {
                    throw new Exception("Unable to Get Statistics\n" + response.DebugInformation);
                }

                res.TotalCount = response.Total;
                res.AverageSalary = response.Aggregations.WeightedAverage("average_salary").Value;
                res.AverageRating = response.Aggregations.Average("average_rating").Value;
                res.AverageViewCount = response.Aggregations.Average("average_views").Value;
                res.Tags = response.Aggregations.Terms("tags").Buckets.Select(b => new KeyValuePair<string, long?>(b.Key, b.DocCount)).ToList();
                res.SalaryRange = response.Aggregations.Range("salary_range").Buckets.Select(b => b.DocCount).ToList();

                return res;
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }

        private async Task<Position> GetPositionAsync(string id)
        {
            try
            {
                var response = await this.client.GetAsync<Position>(id, gd => gd.Index(options.Value.PositionIndexName));
                if (!response.Found)
                {
                    throw new Exception("Position Not Found!");
                }
                return response.Source;
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }

        private async Task<PositionInfoList> GetPositionInfoListAsync(PositionQuery query)
        {
            try
            {
                var response = await this.client.SearchAsync<Position>(s =>
                s.Index(options.Value.PositionIndexName).Sort(sd =>
                {
                    switch (query.SortOrder)
                    {
                        case Models.SortOrder.UpdateTime:
                            return sd.Descending(p => p.UpdateTime);
                        case Models.SortOrder.Views:
                            return sd.Descending(p => p.Views);
                        case Models.SortOrder.Rating:
                            return sd.Descending(p => p.Rating);
                        default:
                            return sd.Descending(SortSpecialField.Score);
                    }
                }).From(query.Start).Size(query.Limit).TrackTotalHits(true).Query(q =>
                {
                    if (query.IsDefault())
                    {
                        return q.MatchAll();
                    }
                    QueryContainer titleQ;
                    QueryContainer tagQ = new QueryContainer();

                    if (!string.IsNullOrEmpty(query.Title))
                    {
                        titleQ = q.Match(qd => qd.Field(p => p.Title).Query(query.Title));

                    }
                    else
                    {
                        titleQ = new QueryContainer();
                    }

                    if (query.Tags == null || query.Tags.Count == 0)
                    {
                        tagQ = new QueryContainer();
                    }
                    else
                    {
                        foreach (var tag in query.Tags)
                        {
                            tagQ = tagQ || q.Term(p => p.Description.Tags, tag, 2);
                        }
                    }

                    var qContainer = titleQ && tagQ;

                    if (!string.IsNullOrEmpty(query.Base))
                    {
                        qContainer = qContainer && q.Term(p => p.Requirement.Base, query.Base);
                    }

                    if (query.Degree != Degree.None)
                    {
                        qContainer = qContainer && q.Range(qd => qd.Field(p => p.Requirement.Degree).LessThanOrEquals((int)query.Degree));
                    }

                    if (query.Salary > 0)
                    {
                        qContainer = qContainer && q.Term(p => p.Salary.Provided, true) && q.Range(qd => qd.Field(p => p.Salary.Amount.LessThanOrEqualTo).GreaterThanOrEquals(query.Salary));
                    }

                    if (query.Experience >= 0)
                    {
                        qContainer = qContainer && q.Range(qd => qd.Field(p => p.Requirement.Experience).LessThanOrEquals(query.Experience));
                    }

                    return qContainer;
                }).Highlight(hs => 
                hs.Fields(
                    fd => fd.Field(p => p.Title).Type(HighlighterType.Plain).PreTags("<span class=\"title highlight\">").PostTags("</span>"),
                    fd => fd.Field(p => p.Description.Tags).Type(HighlighterType.Plain).PreTags("<span class=\"tag highlight\">").PostTags("</span>"))
                ).Source(sc => sc.Excludes(e => e.Fields(p => p.Description.Description, p => p.Description.Url)))
                );

                if (!response.IsValid)
                {
                    throw new Exception("Unable To Search\n" + response.DebugInformation);
                }

                return new PositionInfoList()
                {
                    Positions = response.Hits.Select(hit => {
                        return new PositionInfo()
                        {
                            Id = hit.Id,
                            Position = hit.Source,
                            Highlight = new PositioinHighlight(hit.Highlight)
                        };
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
