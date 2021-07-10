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
    public class Company
    {
        /// <summary>
        /// 公司名称，可分词查询
        /// </summary>
        [Text(Index = true)]
        public string Name { get; set; }

        [Keyword(Index = true)]
        public List<string> Tags { get; set; }

        /// <summary>
        /// 公司图标Url，不可查询
        /// </summary>
        [Keyword(Index = false)]
        public string IconUrl { get; set; }

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

    [Table("company")]
    public class CompanyMySql
    {
        [Key]
        [Column("id")]
        public string Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("iconurl")]
        public string IconUrl { get; set; }

        [Column("tags")]
        public string Tags { get; set; }

        [Column("location")]
        public string Location { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("uploaded")]
        public bool Uploaded { get; set; }

        public Company GetCompany()
        {
            return new Company()
            {
                Description = this.Description,
                IconUrl = this.IconUrl,
                Location = this.Location,
                Name = this.Name,
                Tags = JsonConvert.DeserializeObject<List<string>>(this.Tags)
            };
        }
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
