using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobSEServer.Models
{
    public class ElasticOptions
    {
        public string Url { get; set; }
        public string CompanyIndexName { get; set; }
        public string PositionIndexName { get; set; }
    }
}
