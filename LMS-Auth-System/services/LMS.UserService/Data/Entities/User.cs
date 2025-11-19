using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.UserService.Data.Entities;

[Table("users", Schema = "lms")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("keycloak_id")]
    [MaxLength(255)]
    public string? KeycloakId { get; set; }

    [Column("username")]
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Column("first_name")]
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Column("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
