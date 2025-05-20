using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PI_223_1_7.DbContext
{
    public class LibaryDbContextFactory : IDesignTimeDbContextFactory<LibraryDbContext>
    {
        private const string conStr = "Server=(localdb)\\mssqllocaldb;Database=LibraryDb;Trusted_Connection=True;";

        public LibraryDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LibraryDbContext>();
            optionsBuilder.UseSqlServer(conStr);

            return new LibraryDbContext(optionsBuilder.Options);
        }
    }
}
