using Microsoft.EntityFrameworkCore;

namespace PotatoServer.Services
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<EnvSettings> EnvSettings { get; set; }
    }

    public class EnvSettings
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
