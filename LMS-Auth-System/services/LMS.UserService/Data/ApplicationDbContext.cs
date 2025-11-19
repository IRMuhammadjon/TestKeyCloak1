using LMS.UserService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.UserService.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.KeycloakId).IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name).IsUnique();
        modelBuilder.Entity<Role>()
            .HasIndex(r => r.KeycloakId).IsUnique();

        modelBuilder.Entity<Permission>()
            .HasIndex(p => new { p.Resource, p.Action }).IsUnique();

        modelBuilder.Entity<UserRole>()
            .HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();

        modelBuilder.Entity<UserPermission>()
            .HasIndex(up => new { up.UserId, up.PermissionId }).IsUnique();

        modelBuilder.Entity<RolePermission>()
            .HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
    }
}
