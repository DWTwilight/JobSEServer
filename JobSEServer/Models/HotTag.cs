using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace JobSEServer.Models
{
    [Table("hot_tags")]
    public class HotTag
    {
        [Key]
        [Column("tag_name")]
        public string TagName { get; set; }
        [Column("count")]
        public int Count { get; set; }
        [Column("last_update")]
        public DateTime LastUpdate { get; set; }
    }
}
