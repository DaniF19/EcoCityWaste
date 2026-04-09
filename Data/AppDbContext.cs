using EcoCityWaste.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Data
{
    public class AppDbContext : DbContext
    { 
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Models.Container> Contentores { get; set; }
        public DbSet<ContainerStatusHistory> ContainerStatusHistories { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Occurrence> Occurrences { get; set; }
    
        //public DbSet<Route> Routes { get; set; }
        public DbSet<EcoCityWaste.Models.Route> Routes { get; set; }
        public DbSet<RouteContainer> RouteContainers { get; set; }

        public DbSet<FailureLog> FailureLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Route → RouteContainer (cascade delete cleans up join rows)
            modelBuilder.Entity<RouteContainer>()
                .HasOne(rc => rc.Route)
                .WithMany(r => r.RouteContainers)
                .HasForeignKey(rc => rc.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Container → RouteContainer (restrict: don't delete containers if on a route)
            modelBuilder.Entity<RouteContainer>()
                .HasOne(rc => rc.Container)
                .WithMany()
                .HasForeignKey(rc => rc.ContainerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
