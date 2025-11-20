using LMS.UserService.Data;
using LMS.UserService.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(ApplicationDbContext context, ILogger<PermissionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Permissions
    [HttpGet]
    [Authorize(Roles = "admin,user")]
    public async Task<IActionResult> GetAll()
    {
        var permissions = await _context.Permissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToListAsync();
        return Ok(permissions);
    }

    // GET: api/Permissions/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "admin,user")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null) return NotFound();
        return Ok(permission);
    }

    // POST: api/Permissions
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreatePermissionRequest request)
    {
        var permission = new Permission
        {
            Name = request.Name,
            Resource = request.Resource,
            Action = request.Action,
            Description = request.Description
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Permission {permission.Name} created");
        return CreatedAtAction(nameof(GetById), new { id = permission.Id }, permission);
    }

    // PUT: api/Permissions/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePermissionRequest request)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null) return NotFound();

        permission.Name = request.Name ?? permission.Name;
        permission.Resource = request.Resource ?? permission.Resource;
        permission.Action = request.Action ?? permission.Action;
        permission.Description = request.Description ?? permission.Description;
        permission.IsActive = request.IsActive ?? permission.IsActive;

        _context.Permissions.Update(permission);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Permission {permission.Name} updated");
        return Ok(permission);
    }

    // DELETE: api/Permissions/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null) return NotFound();

        // Soft delete
        permission.IsActive = false;
        _context.Permissions.Update(permission);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Permission {permission.Name} deleted (soft)");
        return NoContent();
    }

    // POST: api/Permissions/{permissionId}/roles/{roleId}
    [HttpPost("{permissionId}/roles/{roleId}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> AssignToRole(Guid permissionId, Guid roleId)
    {
        var permission = await _context.Permissions.FindAsync(permissionId);
        var role = await _context.Roles.FindAsync(roleId);

        if (permission == null || role == null)
            return NotFound(new { message = "Permission or Role not found" });

        var exists = await _context.RolePermissions
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (exists)
            return BadRequest(new { message = "Permission already assigned to this role" });

        var rolePermission = new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        };

        _context.RolePermissions.Add(rolePermission);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Permission {permission.Name} assigned to role {role.Name}");
        return Ok(new { message = "Permission assigned to role successfully" });
    }

    // DELETE: api/Permissions/{permissionId}/roles/{roleId}
    [HttpDelete("{permissionId}/roles/{roleId}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> RemoveFromRole(Guid permissionId, Guid roleId)
    {
        var rolePermission = await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Include(rp => rp.Role)
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (rolePermission == null)
            return NotFound(new { message = "Permission not assigned to this role" });

        _context.RolePermissions.Remove(rolePermission);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Permission {rolePermission.Permission.Name} removed from role {rolePermission.Role.Name}");
        return Ok(new { message = "Permission removed from role successfully" });
    }

    // POST: api/Permissions/{permissionId}/users/{userId}
    [HttpPost("{permissionId}/users/{userId}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> AssignToUser(Guid permissionId, Guid userId)
    {
        var permission = await _context.Permissions.FindAsync(permissionId);
        var user = await _context.Users.FindAsync(userId);

        if (permission == null || user == null)
            return NotFound(new { message = "Permission or User not found" });

        var exists = await _context.UserPermissions
            .AnyAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (exists)
            return BadRequest(new { message = "Permission already assigned to this user" });

        var userPermission = new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId
        };

        _context.UserPermissions.Add(userPermission);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Permission {permission.Name} assigned to user {user.Username}");
        return Ok(new { message = "Permission assigned to user successfully" });
    }

    // DELETE: api/Permissions/{permissionId}/users/{userId}
    [HttpDelete("{permissionId}/users/{userId}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> RemoveFromUser(Guid permissionId, Guid userId)
    {
        var userPermission = await _context.UserPermissions
            .Include(up => up.Permission)
            .Include(up => up.User)
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (userPermission == null)
            return NotFound(new { message = "Permission not assigned to this user" });

        _context.UserPermissions.Remove(userPermission);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Permission {userPermission.Permission.Name} removed from user {userPermission.User.Username}");
        return Ok(new { message = "Permission removed from user successfully" });
    }

    // GET: api/Permissions/users/{userId}
    [HttpGet("users/{userId}")]
    [Authorize(Roles = "admin,user")]
    public async Task<IActionResult> GetUserPermissions(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        // Get direct user permissions
        var directPermissions = await _context.UserPermissions
            .Where(up => up.UserId == userId)
            .Include(up => up.Permission)
            .Select(up => new
            {
                up.Permission.Id,
                up.Permission.Name,
                up.Permission.Resource,
                up.Permission.Action,
                up.Permission.Description,
                Source = "direct"
            })
            .ToListAsync();

        // Get permissions from roles
        var rolePermissions = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => new
            {
                rp.Permission.Id,
                rp.Permission.Name,
                rp.Permission.Resource,
                rp.Permission.Action,
                rp.Permission.Description,
                Source = $"role:{ur.Role.Name}"
            }))
            .ToListAsync();

        var allPermissions = directPermissions.Concat(rolePermissions)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();

        return Ok(new
        {
            userId,
            username = user.Username,
            permissions = allPermissions
        });
    }

    // GET: api/Permissions/roles/{roleId}
    [HttpGet("roles/{roleId}")]
    [Authorize(Roles = "admin,user")]
    public async Task<IActionResult> GetRolePermissions(Guid roleId)
    {
        var role = await _context.Roles.FindAsync(roleId);
        if (role == null) return NotFound(new { message = "Role not found" });

        var permissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.Permission)
            .Select(rp => new
            {
                rp.Permission.Id,
                rp.Permission.Name,
                rp.Permission.Resource,
                rp.Permission.Action,
                rp.Permission.Description
            })
            .ToListAsync();

        return Ok(new
        {
            roleId,
            roleName = role.Name,
            permissions
        });
    }
}

public record CreatePermissionRequest(string Name, string Resource, string Action, string? Description);
public record UpdatePermissionRequest(string? Name, string? Resource, string? Action, string? Description, bool? IsActive);
