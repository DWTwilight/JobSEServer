using JobSEServer.DatabaseContext;
using JobSEServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JobSEServer.Services
{
    public class ElasticService
    {
        private readonly ILogger<ElasticService> logger;
        private readonly IOptions<ElasticOptions> options;
        private ElasticClient client;

        public ElasticService(ILogger<ElasticService> logger, IOptions<ElasticOptions> options)
        {
            this.logger = logger;
            this.options = options;

            var settings = new ConnectionSettings(new Uri(options.Value.Url));
            this.client = new ElasticClient(settings);
        }

        public async Task CreateIndexAsync()
        {
            await this.client.Indices.DeleteAsync(options.Value.CompanyIndexName);
            await this.client.Indices.DeleteAsync(options.Value.PositionIndexName);

            var response = await this.client.Indices.CreateAsync(options.Value.CompanyIndexName, c => c.Map<Company>(m => m.AutoMap()));
            if (!response.ShardsAcknowledged)
            {
                throw new Exception(response.DebugInformation);
            }
            response = await this.client.Indices.CreateAsync(options.Value.PositionIndexName, c => c.Map<Position>(m => m.AutoMap()));
            if (!response.ShardsAcknowledged)
            {
                throw new Exception(response.DebugInformation);
            }
        }

        public async Task InsertTestDataAsync()
        {
            var companies = new List<Company>()
            {
                new Company()
                {
                    Name = "Microsoft",
                    IconUrl = "iurl0",
                    Location = "San Francisco",
                    Description = "Description For Micorsoft"
                },
                new Company()
                {
                    Name = "HP",
                    IconUrl = "iurl1",
                    Location = "New York",
                    Description = "Description For HP",
                }
            };

            var response = await this.client.IndexManyAsync(companies, options.Value.CompanyIndexName);
            if (response.Errors)
            {
                foreach (var error in response.ItemsWithErrors)
                {
                    logger.LogError($"Failed to index document {error.Id}: {error.Error}");
                }
                throw new Exception("Unalbe To Insert");
            }

            var ids = response.Items.Select(i => i.Id).ToList();

            var jobs = new List<Position>()
            {
                new Position()
            {
                            Title = "Microsoft#C++ Engineer",
                            CompanyId = ids[0],
                            UpdateTime = DateTime.Now,
                            Rating = 0,
                            Views = 0,
                            Salary = new Salary()
                            {
                                Provided = true,
                                Amount = new DoubleRange()
                                {
                                    GreaterThanOrEqualTo = 15000,
                                    LessThanOrEqualTo = 25000
                                }
                            },
                            Requirement = new JobRequirement()
                            {
                                Experience = 6,
                                Degree = Degree.Master,
                                Base = new List<string>()
                                {
                                    "Beijing", "Shanghai"
                                }
                            },
                            Description = new JobDescription()
                            {
                                Url = "Job Url 00",
                                Description = "Job Des for 00",
                                Tags = new List<string>()
                                {
                                    "微软",
                                    "高福利"
                                }
                            }
                        },
                new Position()
                        {
                            Title = "Microsoft#Java Engineer",
                            CompanyId = ids[0],
                            UpdateTime = DateTime.Now - TimeSpan.FromDays(1),
                            Rating = 0,
                            Views = 0,
                            Salary = new Salary()
                            {
                                Provided = false,
                                Amount = new DoubleRange()
                                {
                                    GreaterThanOrEqualTo = 20000,
                                    LessThanOrEqualTo = 25000
                                }
                            },
                            Requirement = new JobRequirement()
                            {
                                Experience = 36,
                                Degree = Degree.Bachelor,
                                Base = new List<string>()
                                {
                                    "Beijing", "Chengdu"
                                }
                            },
                            Description = new JobDescription()
                            {
                                Url = "Job Url 01",
                                Description = "Job Des for 01",
                                Tags = new List<string>()
                                {
                                    "微软",
                                    "高福利"
                                }
                            }
                        },
                new Position()
                        {
                            Title = "HP#C++ Engineer(Extra)",
                            CompanyId = ids[1],
                            UpdateTime = DateTime.Now - TimeSpan.FromDays(5),
                            Rating = 0,
                            Views = 0,
                            Salary = new Salary()
                            {
                                Provided = true,
                                Amount = new DoubleRange()
                                {
                                    GreaterThanOrEqualTo = 20000,
                                    LessThanOrEqualTo = 30000
                                }
                            },
                            Requirement = new JobRequirement()
                            {
                                Experience = 18,
                                Degree = Degree.Master,
                                Base = new List<string>()
                                {
                                    "Guangdong", "Shanghai"
                                }
                            },
                            Description = new JobDescription()
                            {
                                Url = "Job Url 10",
                                Description = "Job Des for 10",
                                Tags = new List<string>()
                                {
                                    "外企",
                                    "高福利"
                                }
                            }
                        },
                new Position()
                        {
                            Title = "HP#Java Engineer(Extra)",
                            CompanyId = ids[1],
                            UpdateTime = DateTime.Now - TimeSpan.FromDays(6),
                            Rating = 0,
                            Views = 0,
                            Salary = new Salary()
                            {
                                Provided = false,
                                Amount = new DoubleRange()
                                {
                                    GreaterThanOrEqualTo = 30000,
                                    LessThanOrEqualTo = 40000
                                }
                            },
                            Requirement = new JobRequirement()
                            {
                                Experience = 24,
                                Degree = Degree.Doctor,
                                Base = new List<string>()
                                {
                                    "Nanjing", "Chengdu"
                                }
                            },
                            Description = new JobDescription()
                            {
                                Url = "Job Url 11",
                                Description = "Job Des for 11",
                                Tags = new List<string>()
                                {
                                    "外企",
                                    "高福利"
                                }
                            }
                        }
            };

            response = await this.client.IndexManyAsync(jobs, options.Value.PositionIndexName);
            if (response.Errors)
            {
                foreach (var error in response.ItemsWithErrors)
                {
                    logger.LogError($"Failed to index document {error.Id}: {error.Error}");
                }
                throw new Exception("Unalbe To Insert");
            }
        }

        
    }
}
