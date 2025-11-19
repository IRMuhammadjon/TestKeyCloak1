using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.UserService.Data.Entities;

[Table("user_roles", Schema = "lms")]
public class UserRole
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    [Required]
    public Guid UserId { get; set; }

    [Column("role_id")]
    [Required]
    public Guid RoleId { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [Column("assigned_by")]
    public Guid? AssignedBy { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;
}

[Table("user_permissions", Schema = "lms")]
public class UserPermission
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    [Required]
    public Guid UserId { get; set; }

    [Column("permission_id")]
    [Required]
    public Guid PermissionId { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [Column("assigned_by")]
    public Guid? AssignedBy { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("PermissionId")]
    public virtual Permission Permission { get; set; } = null!;
}

[Table("role_permissions", Schema = "lms")]
public class RolePermission
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("role_id")]
    [Required]
    public Guid RoleId { get; set; }

    [Column("permission_id")]
    [Required]
    public Guid PermissionId { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [Column("assigned_by")]
    public Guid? AssignedBy { get; set; }

    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("PermissionId")]
    public virtual Permission Permission { get; set; } = null!;
}
