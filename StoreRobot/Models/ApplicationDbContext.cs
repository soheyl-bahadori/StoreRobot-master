using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace StoreRobot.Models
{
    public sealed class ApplicationDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=LocalDb.db;");
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductErrors> ProductErrors { get; set; }
        public DbSet<DigiKalaCookie> DigiKalaCookies { get; set; }
        public DbSet<Commission> Commissions { get; set; }
        public DbSet<DigikalaLoginToken> DigikalaLoginToken { get; set; }
        public DbSet<DigikalaLimitLog> DigikalaLimitLogs { get; set; }
    }
}
