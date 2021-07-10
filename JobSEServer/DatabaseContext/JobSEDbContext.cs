using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobSEServer.Models;
using Microsoft.EntityFrameworkCore;

namespace JobSEServer.DatabaseContext
{
    public class JobSEDbContext : DbContext
    {
        public DbSet<PositionMySql> Positions { get; set; }
        public DbSet<CompanyMySql> Companies { get; set; }


        public JobSEDbContext(DbContextOptions<JobSEDbContext> options) : base(options) {}
    }
}
