using LMS.UserService.Data;
using LMS.UserService.Data.Entities;
using LMS.UserService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly KeycloakSyncService _syncService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ApplicationDbContext context, KeycloakSyncService syncService, ILogger<UsersController> logger)
    {
        _context = context;
        _syncService = syncService;
        _logger = logger;
    }

    [HttpGet("me")]
    [Authorize(Roles = "admin,user")]
    public async Task<IActionResult> GetCurrentUser()
    {
        // Get username from JWT token claims
        var username = User.Identity?.Name ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;

        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found in database" });

        // Get user's permissions (direct + from roles)
        var directPermissions = await _context.UserPermissions
            .Where(up => up.UserId == user.Id)
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

        var rolePermissions = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
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
            user.Id,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.IsActive,
            Roles = user.UserRoles?.Select(ur => new { ur.Role.Id, ur.Role.Name }).ToList(),
            Permissions = allPermissions
        });
    }

    [HttpGet]
    [Authorize(Roles = "admin,user")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "admin,user")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Sync to Keycloak
        await _syncService.SyncUserToKeycloak(user, request.Password ?? "ChangeMe123!");

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.Email = request.Email ?? user.Email;
        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.Phone = request.Phone ?? user.Phone;
        user.IsActive = request.IsActive ?? user.IsActive;
        // UpdatedAt is automatically set by ApplicationDbContext.SaveChangesAsync

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        // Sync to Keycloak
        if (!string.IsNullOrEmpty(user.KeycloakId))
        {
            await _syncService.UpdateUserInKeycloak(user);
        }

        return Ok(user);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        // Delete from Keycloak
        if (!string.IsNullOrEmpty(user.KeycloakId))
        {
            await _syncService.DeleteUserFromKeycloak(user.KeycloakId);
        }

        return NoContent();
    }

    [HttpPost("{userId}/roles/{roleId}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> AssignRole(Guid userId, Guid roleId)
    {
        await _syncService.AssignRoleToUser(userId, roleId);
        return Ok(new { message = "Role assigned successfully" });
    }

    [HttpDelete("{userId}/roles/{roleId}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId)
    {
        await _syncService.RemoveRoleFromUser(userId, roleId);
        return Ok(new { message = "Role removed successfully" });
    }
}

public record CreateUserRequest(string Username, string Email, string FirstName, string LastName, string? Phone, string? Password);
public record UpdateUserRequest(string? Email, string? FirstName, string? LastName, string? Phone, bool? IsActive);
