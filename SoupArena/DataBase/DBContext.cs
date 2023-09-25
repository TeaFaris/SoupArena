using Microsoft.EntityFrameworkCore;
using SoupArena.DataBase.Credentials;
using SoupArena.Models.Player.DB;

namespace SoupArena.DataBase
{
    public class DBContext : DbContext, IDBContext<PostgresCredentials>
    {
        public PostgresCredentials Credits { get; init; }

        public DbSet<DBPlayer> Players { get; init; }
        public DbSet<DBInventory> Inventories { get; init; }
        public DbSet<DBInventoryConsumable> Consumables { get; init; }

        public DBContext()
        {
            Credits = new PostgresCredentials
            {
                DataBase = "SoapArena",
#if DEBUG
                Password = "terrar"
#else
                Password = "1qa2ws3ed"
#endif
            };
        }

        protected override void OnConfiguring(DbContextOptionsBuilder OptionsBuilder) => OptionsBuilder.UseNpgsql($"""
                                                                                                                  Server={Credits.Host};
                                                                                                                  Port={Credits.Port};
                                                                                                                  Database={Credits.DataBase};
                                                                                                                  User ID={Credits.Username};
                                                                                                                  Password={Credits.Password};
                                                                                                                  Pooling=true;
                                                                                                                  Connection Lifetime=0;
                                                                                                                  SslMode=Disable;
                                                                                                                  SslMode=Disable;
                                                                                                                  """);
    }
}
