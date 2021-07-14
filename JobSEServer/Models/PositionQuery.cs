using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobSEServer.Models
{
    public enum SortOrder
    {
        UpdateTime,
        Relevance,
        Views,
        Rating
    }

    public class RelevantPositionQuery
    {
        public string Title { get; set; }
        public int Limit { get; set; }
        public string Exclude { get; set; }
    }

    public class PositionQuery
    {
        /// <summary>
        /// 职位名称，分词查询，若为Null或空则查询所有
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 标签
        /// </summary>
        public List<string> Tags { get; set; }
        /// <summary>
        /// 工作地点，全词匹配，若为Null或空则不限
        /// </summary>
        public string Base { get; set; }
        /// <summary>
        /// 学历要求，为0则不限
        /// </summary>
        public Degree Degree { get; set; }
        /// <summary>
        /// 工资区间，0则不限
        /// </summary>
        public double Salary { get; set; }
        /// <summary>
        /// 经验，单位为月,-1为不限
        /// </summary>
        public int Experience { get; set; }
        /// <summary>
        /// 排序
        /// </summary>
        public SortOrder SortOrder { get; set; }
        public int Start { get; set; }
        public int Limit { get; set; }

        public bool IsDefault()
        {
            return string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(Base) && Degree == 0 && Salary == 0 && Experience == -1 && Tags == null;
        }
    }

    public class PositionSuggestion
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int Views { get; set; }
    }

    public class PositioinHighlight
    {
        public string TitleHighlight { get; set; }
        public IDictionary<string, string> TagsHighlight { get; set; }

        public PositioinHighlight(IReadOnlyDictionary<string, IReadOnlyCollection<string>> highlight)
        {
            if(highlight.TryGetValue("title", out var collection))
            {
                TitleHighlight = collection.FirstOrDefault();
            }

            if(highlight.TryGetValue("description.tags", out collection))
            {
                if(collection.Count > 0)
                {
                    TagsHighlight = collection.ToDictionary(tag => GetOriginalTag(tag), tag => tag);
                }
            }
        }

        private static string GetOriginalTag(string highlightedTag)
        {
            try
            {
                var i = highlightedTag.IndexOf('>');
                var j = highlightedTag.LastIndexOf('<');
                return highlightedTag.Substring(i + 1, j - i - 1);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class PositionInfo
    {
        public string Id { get; set; }
        public Position Position { get; set; }
        public PositioinHighlight Highlight { get; set; }
    }

    public class PositionInfoList
    {
        public long Total { get; set; }
        public IList<PositionInfo> Positions { get; set; }
    }

    public class PositionDetail
    {
        public Position Position { get; set; }
        public Company Company { get; set; }
    }

    public class PositionQueryRes
    {
        public PositionInfoList PositionList { get; set; }
        public IDictionary<string, Company> Companies { get; set; }
    }
}
