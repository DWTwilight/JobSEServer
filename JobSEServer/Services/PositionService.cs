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
    public class PositionService
    {
        private readonly ILogger<PositionService> logger;
        private readonly CompanyService companyService;
        private readonly IOptions<ElasticOptions> options;
        private ElasticClient client;

        public PositionService(ILogger<PositionService> logger, CompanyService companyService, IOptions<ElasticOptions> options)
        {
            this.logger = logger;
            this.companyService = companyService;
            this.options = options;

            var settings = new ConnectionSettings(new Uri(options.Value.Url)).DefaultIndex(options.Value.PositionIndexName);
            this.client = new ElasticClient(settings);
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
                var pList = await this.GetPositionInfoListAsync(query);

                var companies = new Dictionary<string, Company>();
                foreach (var p in pList.Positions)
                {
                    var cid = p.Position.CompanyId;
                    if (!companies.ContainsKey(cid))
                    {
                        companies.Add(cid, await companyService.GetCompanyAsync(cid));
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

                var response = await this.client.UpdateAsync<Position>(id, op => op.Doc(position));
                if (!response.IsValid)
                {
                    throw new Exception("Unable To Update Views!");
                }

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
                var response = await this.client.UpdateAsync<Position>(id, op => op.Doc(position));
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
                s.Sort(sd => sd.Descending(SortSpecialField.Score)).Size(query.Limit)
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
                s.Sort(sd => sd.Descending(p => p.UpdateTime)).From(start).Size(limit)
                .Query(q => q.ConstantScore(qd => qd.Filter(qcd => qcd.Term(p => p.CompanyId, companyId))))
                .Source(sc => sc.Excludes(e => e.Fields(p => p.Description, p => p.Requirement)))
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

        private async Task<Position> GetPositionAsync(string id)
        {
            try
            {
                var response = await this.client.GetAsync<Position>(id);
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
                s.Sort(sd =>
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
                }).From(query.Start).Size(query.Limit).Query(q =>
                {
                    if (query.IsDefault())
                    {
                        return q.MatchAll();
                    }
                    var qContainer = new QueryContainer();

                    if (!string.IsNullOrEmpty(query.Title))
                    {
                        qContainer = qContainer && (q.Match(qd => qd.Field(p => p.Title).Query(query.Title)) || q.Term(p => p.Description.Tags, query.Title));
                    }

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
                        qContainer = qContainer && q.Term(p => p.Salary.Provided, true) && q.Range(qd => qd.Field(p => p.Salary.Amount).GreaterThanOrEquals(query.Salary));
                    }

                    if (query.Experience >= 0)
                    {
                        qContainer = qContainer && q.Range(qd => qd.Field(p => p.Requirement.Experience).LessThanOrEquals(query.Experience));
                    }

                    return qContainer;
                }).Source(sc => sc.Excludes(e => e.Field(p => p.Description))));

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
    }
}
