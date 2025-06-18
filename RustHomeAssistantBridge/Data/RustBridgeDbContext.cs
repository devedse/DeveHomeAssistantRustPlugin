using Microsoft.EntityFrameworkCore;
using RustHomeAssistantBridge.Models;

namespace RustHomeAssistantBridge.Data;

public class RustBridgeDbContext : DbContext
{
    public RustBridgeDbContext(DbContextOptions<RustBridgeDbContext> options) : base(options)
    {
    }

    public DbSet<RustServer> RustServers { get; set; }
    public DbSet<EntityInfo> EntityInfos { get; set; }
    public DbSet<ServerLog> ServerLogs { get; set; }
    public DbSet<FcmCredential> FcmCredentials { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure RustServer
        modelBuilder.Entity<RustServer>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => new { e.ServerAddress, e.Port }).IsUnique();
        });

        // Configure EntityInfo
        modelBuilder.Entity<EntityInfo>(entity =>
        {
            entity.HasIndex(e => new { e.ServerId, e.EntityId }).IsUnique();

            entity.HasOne(e => e.Server)
                  .WithMany(s => s.Entities)
                  .HasForeignKey(e => e.ServerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ServerLog
        modelBuilder.Entity<ServerLog>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);

            entity.HasOne(l => l.Server)
                  .WithMany(s => s.Logs)
                  .HasForeignKey(l => l.ServerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure FcmCredential
        modelBuilder.Entity<FcmCredential>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }
}
