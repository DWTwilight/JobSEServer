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
        private readonly JobSEDbContext dbContext;
        private ElasticClient client;

        public ElasticService(ILogger<ElasticService> logger, IOptions<ElasticOptions> options, JobSEDbContext dbContext)
        {
            this.logger = logger;
            this.options = options;
            this.dbContext = dbContext;

            var settings = new ConnectionSettings(new Uri(options.Value.Url));
            this.client = new ElasticClient(settings);
        }

        public async Task CreateIndexAsync()
        {
            await dbContext.Positions.ForEachAsync(p => p.Uploaded = false);
            //await dbContext.Positions.ForEachAsync(p =>
            //{
            //    if (p.Base != null && !p.Base.StartsWith("["))
            //    {
            //        p.Base = "[\"" + p.Base + "\"]";
            //    }
            //});
            await dbContext.Companies.ForEachAsync(c => c.Uploaded = false);
            await dbContext.SaveChangesAsync();

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
            try
            {
                await dbContext.Positions.ForEachAsync(p => p.Url = p.Url.Replace("html", "html?"));
                await dbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }

        
    }
}
