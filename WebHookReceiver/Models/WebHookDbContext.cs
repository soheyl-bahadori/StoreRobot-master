using Microsoft.EntityFrameworkCore;

namespace WebHookReceiver.Models
{
    public sealed class WebHookDbContext : DbContext
    {
        public WebHookDbContext(DbContextOptions<WebHookDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<Order> Orders { get; set; }
    }
}
