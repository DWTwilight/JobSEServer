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
    public class ESClientManagerService
    {
        public ElasticClient Client { get; set; }

        private readonly ILogger<ESClientManagerService> logger;
        private readonly IOptions<ElasticOptions> options;

        public ESClientManagerService(ILogger<ESClientManagerService> logger, IOptions<ElasticOptions> options)
        {
            this.logger = logger;
            this.options = options;

            var settings = new ConnectionSettings(new Uri(options.Value.Url)).BasicAuthentication(options.Value.Username, options.Value.Password);
            this.Client = new ElasticClient(settings);
        }
    }
}
