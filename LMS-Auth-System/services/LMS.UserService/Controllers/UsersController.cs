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
        user.UpdatedAt = DateTime.UtcNow;

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
