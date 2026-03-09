using EcoCityWaste.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Data
{
    public class AppDbContext : DbContext
    { public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Models.Container> Contentores { get; set; }

        public DbSet<ContainerStatusHistory> ContainerStatusHistories { get; set; }
    }

    }
