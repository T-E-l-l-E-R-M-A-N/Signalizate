using System;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database
{
    public class MyDbContext : DbContext
    {
        public DbSet<UserDbModel> Users { get; set; }
        public DbSet<MessageDbModel> Messages { get; set; }
        public DbSet<DialogDbModel> Dialogs{ get; set; }

        public MyDbContext()
        {
            
        }

        public MyDbContext(DbContextOptions<MyDbContext> o) : base(o)
        {
            
        }
    }
}
