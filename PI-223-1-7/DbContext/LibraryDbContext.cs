using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PI_223_1_7.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PI_223_1_7.DbContext
{
    public class LibraryDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string,
        IdentityUserClaim<string>, ApplicationUserRole, IdentityUserLogin<string>,
        IdentityRoleClaim<string>, IdentityUserToken<string>>
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
        {

        }
        public LibraryDbContext() { }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Book> Books { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //Configuration for Book model
            modelBuilder.Entity<Book>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name).IsRequired();

                entity.Property(x => x.Author).IsRequired();

                entity.Property(x => x.Description).IsRequired();

                entity.Property(x => x.Genre).IsRequired();

                entity.Property(x => x.Type).IsRequired();

                entity.Property(x => x.Year).IsRequired();

                entity.HasMany(b => b.Orders).WithOne(b => b.Book).OnDelete(DeleteBehavior.Cascade);
            });

            //Configuration for Order model
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(entity => entity.Id);

                entity.Property(o => o.OrderDate).IsRequired();

                entity.Property(o => o.Type).IsRequired();
            });

            //Configuration for ApplicationUser model
            modelBuilder.Entity<ApplicationUserRole>(userRole =>
            {
                userRole.HasKey(ur => new { ur.UserId, ur.RoleId });

                userRole.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                userRole.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });
        }
    }
}
