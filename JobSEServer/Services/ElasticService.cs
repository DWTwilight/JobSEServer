using JobSEServer.Models;
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
                    Url = "url0",
                    Location = "San Francisco",
                    Description = "Description For Micorsoft"
                },
                new Company()
                {
                    Name = "HP",
                    IconUrl = "iurl1",
                    Url = "url1",
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
                                Descritpion = "Job Des for 00",
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
                                Descritpion = "Job Des for 01",
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
                                Descritpion = "Job Des for 10",
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
                                Descritpion = "Job Des for 11",
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

        public async Task<string> AddCompanyAsync(Company company)
        {
            try
            {
                var id = GetStringHash(company.Url);

                var existResponse = await this.client.DocumentExistsAsync<Company>(id, idx => idx.Index(this.options.Value.CompanyIndexName));
                if (existResponse.Exists)
                {
                    return id;
                }

                var insertResponse = await this.client.IndexAsync(company, idx => idx.Index(this.options.Value.CompanyIndexName).Id(id));
                if (!insertResponse.IsValid)
                {
                    throw new Exception(insertResponse.DebugInformation);
                }
                return id;
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }

        public async Task<string> AddPositionAsync(Position position)
        {
            try
            {
                var getResponse = await this.client.GetAsync<Company>(position.CompanyId, idx => idx.Index(this.options.Value.CompanyIndexName));
                if (!getResponse.Found)
                {
                    throw new Exception("Company does not exist!");
                }

                var id = GetStringHash(position.Description.Url);
                position.Title = string.Join('#', getResponse.Source.Name, position.Title);

                await this.InsertPositionAsync(position, id);
                return id;

            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }

        public async Task<IList<string>> BulkAddPositionAsync(IList<Position> positions)
        {
            try
            {
                if(positions.Count <= 0)
                {
                    return null;
                }
                //Check Company Ids
                var cId = positions[0].CompanyId;
                if(positions.Any(p => p.CompanyId != cId))
                {
                    throw new Exception("Company Ids must be the same!");
                }

                //CheckCompany
                var getResponse = await this.client.GetAsync<Company>(cId, idx => idx.Index(this.options.Value.CompanyIndexName));
                if (!getResponse.Found)
                {
                    throw new Exception("Company does not exist!");
                }

                //Generate Ids
                var ids = positions.Select(p => GetStringHash(p.Description.Url)).ToList();

                //Process Title
                for (int i = 0; i < positions.Count; i++)
                {
                    var p = positions[i];
                    p.Title = string.Join('#', getResponse.Source.Name, p.Title);
                    await this.InsertPositionAsync(p, ids[i]);
                }

                return ids;
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }

        private async Task InsertPositionAsync(Position position, string id)
        {
            var insertResponse = await this.client.IndexAsync(position, idx => idx.Index(this.options.Value.PositionIndexName).Id(id));
            if (!insertResponse.IsValid)
            {
                throw new Exception(insertResponse.DebugInformation);
            }
        }

        private static MD5 md5 = System.Security.Cryptography.MD5.Create();
        public static string GetStringHash(string str)
        {
            var hash = md5.ComputeHash(Encoding.Default.GetBytes(str));
            var sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }
    }
}
