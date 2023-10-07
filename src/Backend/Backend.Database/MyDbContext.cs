using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database
{
    public class MyDbContext : IdentityDbContext<UserDbModel>
    {
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
