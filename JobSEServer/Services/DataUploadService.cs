using Elasticsearch.Net;
using JobSEServer.DatabaseContext;
using JobSEServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobSEServer.Services
{
    public class DataUploadService : BackgroundService
    {
        private const int interval = 30;

        private readonly ILogger<DataUploadService> logger;
        private readonly IOptions<ElasticOptions> options;
        private readonly JobSEDbContext dbContext;
        private ElasticClient client;

        public DataUploadService(ILogger<DataUploadService> logger, IOptions<ElasticOptions> options, MysqlOption mysqlOption, ESClientManagerService esClientService)
        {
            this.logger = logger;
            this.options = options;

            var dbOptions = new DbContextOptionsBuilder<JobSEDbContext>().UseMySQL(mysqlOption.ConnectionString).Options;
            this.dbContext = new JobSEDbContext(dbOptions);

            var settings = new ConnectionSettings(new Uri(options.Value.Url)).BasicAuthentication(options.Value.Username, options.Value.Password);
            this.client = esClientService.Client;
        }

        public async Task UploadElasticAsync()
        {
            try
            {
                var companiesToUpload = await dbContext.Companies.Where(c => !c.Uploaded).Take(500).ToListAsync();
                if(companiesToUpload.Count > 0)
                {
                    foreach (var company in companiesToUpload)
                    {
                        try
                        {
                            await this.InsertCompanyAsync(company.GetCompany(), company.Id);
                            company.Uploaded = true;
                            dbContext.Companies.Update(company);
                        }
                        catch (Exception) { }
                    }
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Updated Companies { Count: " + companiesToUpload.Count + " }");
                }
                

                var positionsToUpload = await dbContext.Positions.Where(p => !p.Uploaded && p.CompanyId != null && p.Tags != null).Take(1000).ToListAsync();
                if(positionsToUpload.Count > 0)
                {
                    foreach (var position in positionsToUpload)
                    {
                        try
                        {
                            await this.InsertPositionAsync(position.GetPosition());
                            position.Uploaded = true;
                            dbContext.Positions.Update(position);
                        }
                        catch (Exception e) { logger.LogError(e.Message); }
                    }
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Updated Positions { Count: " + positionsToUpload.Count + " }");
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }
        }

        private async Task InsertCompanyAsync(Company company, string id)
        {
            try
            {
                var insertResponse = await this.client.IndexAsync(company, idx => idx.Index(this.options.Value.CompanyIndexName).Id(id));
                if (!insertResponse.IsValid)
                {
                    throw new Exception(insertResponse.DebugInformation);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }

        private async Task InsertPositionAsync(Position position)
        {
            try
            {
                var id = GetStringHash(position.Title + position.Description.Url);
                position.Views = 0;
                position.Rating = 5;

                var insertResponse = await this.client.IndexAsync(position, idx => idx.Index(this.options.Value.PositionIndexName).Id(id));
                if (!insertResponse.IsValid)
                {
                    throw new Exception(insertResponse.DebugInformation);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(10000, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await this.UploadElasticAsync();
                await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
            }
        }
    }
}
