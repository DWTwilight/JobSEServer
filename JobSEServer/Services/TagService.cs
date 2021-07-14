using JobSEServer.DatabaseContext;
using JobSEServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobSEServer.Services
{
    public class TagService
    {
        private readonly ILogger<TagService> logger;
        private readonly JobSEDbContext dbContext;

        public TagService(ILogger<TagService> logger, JobSEDbContext dbContext)
        {
            this.logger = logger;
            this.dbContext = dbContext;
        }

        public async Task CreateOrUpdateTagsAsync(List<string> tagNames)
        {
            try
            {
                if(tagNames == null)
                {
                    return;
                }
                tagNames.RemoveAll(t => string.IsNullOrEmpty(t));
                foreach (var tagName in tagNames)
                {
                    await this.CreateOrUpdateTagAsync(tagName);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }
        }

        public async Task CreateOrUpdateTagAsync(string tagName)
        {
            try
            {
                var tag = await dbContext.HotTags.FindAsync(tagName);
                if(tag == null)
                {
                    await dbContext.HotTags.AddAsync(new HotTag()
                    {
                        TagName = tagName,
                        Count = 1,
                        LastUpdate = DateTime.Now
                    });
                }
                else
                {
                    tag.Count++;
                    tag.LastUpdate = DateTime.Now;
                }
                await dbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e.InnerException.Message);
                logger.LogError(e.StackTrace);
            }
        }

        public async Task<IList<HotTag>> GetHotTagsAsync(int limit)
        {
            try
            {
                var timeLimit = DateTime.Now - TimeSpan.FromDays(30);
                return await dbContext.HotTags.Where(t => t.LastUpdate > timeLimit).OrderByDescending(t => t.Count).Take(limit).ToListAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e.InnerException.Message);
                logger.LogError(e.StackTrace);
                throw;
            }
        }
    }
}
