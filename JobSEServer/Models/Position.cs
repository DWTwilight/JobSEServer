using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

    [Table("job")]
    public class PositionMySql
    {
        [Key]
        [Column("url")]
        public string Url { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("company_id")]
        public string CompanyId { get; set; }

        [Column("company_name")]
        public string CompanyName { get; set; }

        [Column("update_time")]
        public DateTime UpdateTime { get; set; }

        [Column("salary_provided")]
        public bool SalaryProvided { get; set; }

        [Column("salary_min")]
        public double SalaryMin { get; set; }

        [Column("salary_max")]
        public double SalaryMax { get; set; }

        [Column("experience")]
        public int Experience { get; set; }

        [Column("degree")]
        public Degree Degree { get; set; }

        [Column("base")]
        public string Base { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("tags")]
        public string Tags { get; set; }

        [Column("uploaded")]
        public bool Uploaded { get; set; }

        public Position GetPosition()
        {
            return new Position()
            {
                Title = string.Join("#", this.CompanyName, this.Title),
                CompanyId = this.CompanyId,
                UpdateTime = this.UpdateTime,
                Salary = new Salary()
                {
                    Provided = this.SalaryProvided,
                    Amount = new Amount()
                    {
                        GreaterThanOrEqualTo = this.SalaryMin,
                        LessThanOrEqualTo = this.SalaryMax
                    }
                },
                Requirement = new JobRequirement()
                {
                    Degree = this.Degree,
                    Experience = this.Experience,
                    Base = JsonConvert.DeserializeObject<List<string>>(this.Base)
                },
                Description = new JobDescription()
                {
                    Description = this.Description,
                    Url = this.Url,
                    Tags = JsonConvert.DeserializeObject<List<string>>(this.Tags)
                }
            };
        }
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
        [Keyword(Index = false)]
        public string Url { get; set; }

        [Keyword(Index = false)]
        public string Description { get; set; }

        [Keyword(Index = true)]
        public List<string> Tags { get; set; }
    }

    public class Salary
    {
        public bool Provided { get; set; }
        public Amount Amount { get; set; }
    }

    public class Amount
    {
        [PropertyName("gte")]
        public double GreaterThanOrEqualTo { get; set; }
        [PropertyName("lte")]
        public double LessThanOrEqualTo { get; set; }
    }
}
