using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobSEServer.Models
{
    public enum Degree
    {
        None,   //不限
        JuniorCollege,  //大专
        Bachelor,   //本科（学士）
        Master, //研究生（硕士）
        Doctor	//研
    }

    public class Position
    {
        /// <summary>
        /// 职位名称，可分词查询
        /// </summary>
        [Text(Index = true)]
        public string Title { get; set; }

        [Keyword(Index = true)]
        public string CompanyId { get; set; }

        public DateTime UpdateTime { get; set; }

        public double Rating { get; set; }

        public int Views { get; set; }

        public Salary Salary { get; set; }

        /// <summary>
        /// 工作描述
        /// </summary>
        public JobRequirement Requirement { get; set; }

        public JobDescription Description { get; set; }
    }

    public class JobRequirement
    {
        /// <summary>
        /// 经验，单位为月
        /// </summary>
        public int Experience { get; set; }

        /// <summary>
        /// 学历
        /// </summary>
        public Degree Degree { get; set; }

        /// <summary>
        /// 工作地点
        /// </summary>
        [Keyword]
        public List<string> Base { get; set; }
    }

    public class JobDescription
    {
        [Keyword(Index = true)]
        public string Url { get; set; }

        [Keyword(Index = false)]
        public string Descritpion { get; set; }

        [Keyword(Index = true)]
        public List<string> Tags { get; set; }
    }

    public class Salary
    {
        public bool Provided { get; set; }
        public DoubleRange Amount { get; set; }
    }
}
