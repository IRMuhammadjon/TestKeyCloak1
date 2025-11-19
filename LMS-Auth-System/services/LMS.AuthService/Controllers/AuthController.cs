using LMS.Shared.TokenValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpGet("validate")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        var userId = User.GetUserId();
        var username = User.GetUsername();
        var email = User.GetEmail();
        var firstName = User.GetFirstName();
        var lastName = User.GetLastName();
        var roles = User.GetRoles();

        return Ok(new
        {
            userId,
            username,
            email,
            firstName,
            lastName,
            roles,
            isAuthenticated = true
        });
    }

    [HttpGet("user-info")]
    [Authorize]
    public IActionResult GetUserInfo()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });

        return Ok(new
        {
            userId = User.GetUserId(),
            username = User.GetUsername(),
            email = User.GetEmail(),
            firstName = User.GetFirstName(),
            lastName = User.GetLastName(),
            roles = User.GetRoles(),
            claims
        });
    }

    [HttpGet("check-role/{role}")]
    [Authorize]
    public IActionResult CheckRole(string role)
    {
        var hasRole = User.HasRole(role);
        return Ok(new { role, hasRole });
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
