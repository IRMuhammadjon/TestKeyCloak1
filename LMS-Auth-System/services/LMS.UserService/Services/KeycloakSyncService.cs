using LMS.UserService.Data;
using LMS.UserService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LMS.UserService.Services;

public class KeycloakSyncService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakSyncService> _logger;
    private readonly string _realmName;
    private readonly string _keycloakUrl;
    private readonly string _adminUsername;
    private readonly string _adminPassword;

    public KeycloakSyncService(
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        ILogger<KeycloakSyncService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _realmName = configuration["Keycloak:Realm"] ?? "lms-realm";
        _keycloakUrl = configuration["Keycloak:AuthServerUrl"] ?? "http://keycloak:8080";
        _adminUsername = configuration["Keycloak:AdminUsername"] ?? "admin";
        _adminPassword = configuration["Keycloak:AdminPassword"] ?? "admin123";
    }

    private async Task<string> GetAdminToken()
    {
        var tokenUrl = $"{_keycloakUrl}/realms/master/protocol/openid-connect/token";
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", "admin-cli"),
            new KeyValuePair<string, string>("username", _adminUsername),
            new KeyValuePair<string, string>("password", _adminPassword),
            new KeyValuePair<string, string>("grant_type", "password")
        });

        var response = await _httpClient.PostAsync(tokenUrl, content);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return result.GetProperty("access_token").GetString() ?? throw new Exception("Failed to get admin token");
    }

    // USER SYNC
    public async Task<string> SyncUserToKeycloak(User user, string password = "ChangeMe123!")
    {
        try
        {
            if (!string.IsNullOrEmpty(user.KeycloakId))
            {
                await UpdateUserInKeycloak(user);
                return user.KeycloakId;
            }

            var token = await GetAdminToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var keycloakUser = new
            {
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                enabled = user.IsActive,
                emailVerified = true,
                credentials = new[]
                {
                    new
                    {
                        type = "password",
                        value = password,
                        temporary = false
                    }
                }
            };

            var createUrl = $"{_keycloakUrl}/admin/realms/{_realmName}/users";
            var jsonContent = new StringContent(JsonSerializer.Serialize(keycloakUser), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(createUrl, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                // Get created user ID from location header
                var location = response.Headers.Location?.ToString();
                if (location != null)
                {
                    var userId = location.Split('/').Last();
                    user.KeycloakId = userId;
                    _dbContext.Users.Update(user);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation($"User {user.Username} synced to Keycloak with ID: {userId}");
                    return userId;
                }
            }

            _logger.LogWarning($"User {user.Username} sync to Keycloak returned: {response.StatusCode}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error syncing user {user.Username} to Keycloak");
            return string.Empty;
        }
    }

    public async Task UpdateUserInKeycloak(User user)
    {
        if (string.IsNullOrEmpty(user.KeycloakId))
            return;

        try
        {
            var token = await GetAdminToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var keycloakUser = new
            {
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                enabled = user.IsActive,
                emailVerified = true
            };

            var updateUrl = $"{_keycloakUrl}/admin/realms/{_realmName}/users/{user.KeycloakId}";
            var jsonContent = new StringContent(JsonSerializer.Serialize(keycloakUser), Encoding.UTF8, "application/json");
            await _httpClient.PutAsync(updateUrl, jsonContent);

            _logger.LogInformation($"User {user.Username} updated in Keycloak");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating user {user.Username} in Keycloak");
        }
    }

    public async Task DeleteUserFromKeycloak(string keycloakId)
    {
        try
        {
            var token = await GetAdminToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var deleteUrl = $"{_keycloakUrl}/admin/realms/{_realmName}/users/{keycloakId}";
            await _httpClient.DeleteAsync(deleteUrl);

            _logger.LogInformation($"User {keycloakId} deleted from Keycloak");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting user {keycloakId} from Keycloak");
        }
    }

    // ROLE SYNC - Simplified implementations
    public async Task<string?> SyncRoleToKeycloak(Role role)
    {
        _logger.LogInformation($"Role sync not yet implemented for {role.Name}");
        return await Task.FromResult(role.KeycloakId);
    }

    public async Task UpdateRoleInKeycloak(Role role)
    {
        _logger.LogInformation($"Role update not yet implemented for {role.Name}");
        await Task.CompletedTask;
    }

    public async Task DeleteRoleFromKeycloak(string roleName)
    {
        _logger.LogInformation($"Role deletion not yet implemented for {roleName}");
        await Task.CompletedTask;
    }

    public async Task SyncUserRolesToKeycloak(User user)
    {
        _logger.LogInformation($"User role sync not yet implemented for {user.Username}");
        await Task.CompletedTask;
    }

    public async Task AssignRoleToUser(Guid userId, Guid roleId, Guid? assignedBy = null)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        var role = await _dbContext.Roles.FindAsync(roleId);

        if (user == null || role == null)
            throw new Exception("User or Role not found");

        var exists = await _dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (exists)
            return;

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedBy = assignedBy
        };

        _dbContext.UserRoles.Add(userRole);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Role {role.Name} assigned to user {user.Username}");
    }

    public async Task RemoveRoleFromUser(Guid userId, Guid roleId)
    {
        var userRole = await _dbContext.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole == null)
            return;

        _dbContext.UserRoles.Remove(userRole);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Role {userRole.Role.Name} removed from user {userRole.User.Username}");
    }
}
