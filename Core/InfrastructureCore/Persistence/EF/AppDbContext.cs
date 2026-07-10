using InfrastructureCore.Persistence.EF.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureCore.Persistence.EF
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {

        public DbSet<VideoEntity> Videos { get; set; } 
        public DbSet<CourseEntity> Courses { get; set; }  
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>().ToTable("Users");

            builder.Entity<IdentityRole<Guid>>().ToTable("Roles");

            builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");

            builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");

            builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");

            builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

            builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

            builder.Entity<VideoEntity>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id).ValueGeneratedOnAdd();

            });

            builder.Entity<CourseEntity>(entity =>
            {
                entity.ToTable("Courses");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Title).HasMaxLength(200);

                entity.Property(x => x.Description).HasMaxLength(1000);

                entity.HasOne(x => x.User)
                 .WithMany(x => x.Courses)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(x => x.Videos)
                 .WithOne(x => x.Course)
                 .HasForeignKey(x => x.CourseId)
                 .OnDelete(DeleteBehavior.Cascade);
                
            });

        }
    }
}