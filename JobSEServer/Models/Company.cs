using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobSEServer.Models
{
    public class Company
    {
        /// <summary>
        /// 公司名称，可分词查询
        /// </summary>
        [Text(Index = true)]
        public string Name { get; set; }

        /// <summary>
        /// 公司图标Url，不可查询
        /// </summary>
        [Keyword(Index = false)]
        public string IconUrl { get; set; }

        /// <summary>
        /// 公司Url(天眼查)
        /// </summary>
        [Keyword(Index = false)]
        public string Url { get; set; }

        /// <summary>
        /// 地点
        /// </summary>
        [Keyword(Index = false)]
        public string Location { get; set; }

        /// <summary>
        /// 公司描述，不可查询
        /// </summary>
        [Keyword(Index = false)]
        public string Description { get; set; }
    }

    public class CompanyInfo
    {
        public string Id { get; set; }
        public Company Company { get; set; }
    }

    public class CompanyInfoList
    {
        public long Total { get; set; }
        public IList<CompanyInfo> CompanyList { get; set; }
    }
}
