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

    public class CompanyStatistics
    {
        public long TotalCount { get; set; }
        public double? AverageSalary { get; set; }
        public double? AverageRating { get; set; }
        public double? TotalViewCount { get; set; }
        public IList<KeyValuePair<string, long?>> Tags { get; set; }
        public IList<long> SalaryRange { get; set; }
    }
}
