using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobSEServer.Models
{
    public class CompanyQuery
    {
        public string Title { get; set; }
        public List<string> Tags { get; set; }
        public int Start { get; set; }
        public int Limit { get; set; }
    }
}
