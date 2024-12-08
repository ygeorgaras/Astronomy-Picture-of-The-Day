using APODApi.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APODApi.Infrastructure.Data
{
    public class ApodDbContext : DbContext
    {
        public ApodDbContext(DbContextOptions<ApodDbContext> options) : base(options)
        {
        }

        public DbSet<ApodEntry> ApodEntries { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApodEntry>()
                .HasIndex(e => e.Date)
                .IsUnique();
        }
    }
}
