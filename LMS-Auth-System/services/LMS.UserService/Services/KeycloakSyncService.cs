using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Admin.Realms.Item.Users;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;
using LMS.UserService.Data;
using LMS.UserService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.UserService.Services;

public class KeycloakSyncService
{
    private readonly IKeycloakRealmClient _keycloakClient;
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakSyncService> _logger;
    private readonly string _realmName;

    public KeycloakSyncService(
        IKeycloakRealmClient keycloakClient,
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        ILogger<KeycloakSyncService> logger)
    {
        _keycloakClient = keycloakClient;
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _realmName = configuration["Keycloak:Realm"] ?? "lms-realm";
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

            var keycloakUser = new UserRepresentation
            {
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Enabled = user.IsActive,
                EmailVerified = true,
                Credentials = new List<CredentialRepresentation>
                {
                    new CredentialRepresentation
                    {
                        Type = "password",
                        Value = password,
                        Temporary = false
                    }
                }
            };

            await _keycloakClient.PostAsync(keycloakUser);

            // Get created user to retrieve ID
            var users = await _keycloakClient.GetAsync(query =>
            {
                query.QueryParameters.Username = user.Username;
                query.QueryParameters.Exact = true;
            });

            var createdUser = users?.FirstOrDefault();
            if (createdUser?.Id != null)
            {
                user.KeycloakId = createdUser.Id;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();

                // Sync roles
                await SyncUserRolesToKeycloak(user);

                _logger.LogInformation($"User {user.Username} synced to Keycloak with ID: {createdUser.Id}");
                return createdUser.Id;
            }

            throw new Exception("Failed to retrieve created user from Keycloak");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error syncing user {user.Username} to Keycloak");
            throw;
        }
    }

    public async Task UpdateUserInKeycloak(User user)
    {
        if (string.IsNullOrEmpty(user.KeycloakId))
        {
            throw new Exception("User does not have Keycloak ID");
        }

        try
        {
            var keycloakUser = new UserRepresentation
            {
                Id = user.KeycloakId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Enabled = user.IsActive,
                EmailVerified = true
            };

            await _keycloakClient[user.KeycloakId].PutAsync(keycloakUser);

            // Sync roles
            await SyncUserRolesToKeycloak(user);

            _logger.LogInformation($"User {user.Username} updated in Keycloak");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating user {user.Username} in Keycloak");
            throw;
        }
    }

    public async Task DeleteUserFromKeycloak(string keycloakId)
    {
        try
        {
            await _keycloakClient[keycloakId].DeleteAsync();
            _logger.LogInformation($"User {keycloakId} deleted from Keycloak");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting user {keycloakId} from Keycloak");
            throw;
        }
    }

    // ROLE SYNC
    public async Task<string?> SyncRoleToKeycloak(Role role)
    {
        try
        {
            if (!string.IsNullOrEmpty(role.KeycloakId))
            {
                await UpdateRoleInKeycloak(role);
                return role.KeycloakId;
            }

            var keycloakRole = new RoleRepresentation
            {
                Name = role.Name,
                Description = role.Description
            };

            var realmRoles = _keycloakClient.Roles;
            await realmRoles.PostAsync(keycloakRole);

            // Get created role
            var createdRole = await realmRoles[role.Name].GetAsync();
            if (createdRole?.Id != null)
            {
                role.KeycloakId = createdRole.Id;
                _dbContext.Roles.Update(role);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Role {role.Name} synced to Keycloak with ID: {createdRole.Id}");
                return createdRole.Id;
            }

            throw new Exception("Failed to retrieve created role from Keycloak");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error syncing role {role.Name} to Keycloak");
            throw;
        }
    }

    public async Task UpdateRoleInKeycloak(Role role)
    {
        try
        {
            var keycloakRole = new RoleRepresentation
            {
                Id = role.KeycloakId,
                Name = role.Name,
                Description = role.Description
            };

            await _keycloakClient.Roles[role.Name].PutAsync(keycloakRole);
            _logger.LogInformation($"Role {role.Name} updated in Keycloak");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating role {role.Name} in Keycloak");
            throw;
        }
    }

    public async Task DeleteRoleFromKeycloak(string roleName)
    {
        try
        {
            await _keycloakClient.Roles[roleName].DeleteAsync();
            _logger.LogInformation($"Role {roleName} deleted from Keycloak");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting role {roleName} from Keycloak");
            throw;
        }
    }

    // USER ROLE SYNC
    public async Task SyncUserRolesToKeycloak(User user)
    {
        if (string.IsNullOrEmpty(user.KeycloakId))
        {
            throw new Exception("User does not have Keycloak ID");
        }

        try
        {
            // Load user roles
            var userRoles = await _dbContext.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == user.Id)
                .ToListAsync();

            if (!userRoles.Any()) return;

            // Get current Keycloak roles
            var currentKeycloakRoles = await _keycloakClient[user.KeycloakId].Roles.GetAsync();

            // Prepare role representations
            var rolesToAdd = new List<RoleRepresentation>();
            var rolesToRemove = currentKeycloakRoles?.Mappings?.RealmMappings?.ToList() ?? new List<RoleRepresentation>();

            foreach (var userRole in userRoles)
            {
                var keycloakRole = await _keycloakClient.Roles[userRole.Role.Name].GetAsync();
                if (keycloakRole != null)
                {
                    rolesToAdd.Add(keycloakRole);
                    // Remove from removal list if already exists
                    rolesToRemove.RemoveAll(r => r.Name == userRole.Role.Name);
                }
            }

            // Add new roles
            if (rolesToAdd.Any())
            {
                await _keycloakClient[user.KeycloakId].Roles.Realm.PostAsync(rolesToAdd);
            }

            // Remove old roles
            if (rolesToRemove.Any())
            {
                await _keycloakClient[user.KeycloakId].Roles.Realm.DeleteAsync(rolesToRemove);
            }

            _logger.LogInformation($"User roles synced for {user.Username}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error syncing user roles for {user.Username}");
            throw;
        }
    }

    // ASSIGN ROLE TO USER
    public async Task AssignRoleToUser(Guid userId, Guid roleId, Guid? assignedBy = null)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        var role = await _dbContext.Roles.FindAsync(roleId);

        if (user == null || role == null)
            throw new Exception("User or Role not found");

        // Check if already assigned
        var exists = await _dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (exists)
            return;

        // Add to database
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedBy = assignedBy
        };

        _dbContext.UserRoles.Add(userRole);
        await _dbContext.SaveChangesAsync();

        // Sync to Keycloak
        if (!string.IsNullOrEmpty(user.KeycloakId))
        {
            var keycloakRole = await _keycloakClient.Roles[role.Name].GetAsync();
            if (keycloakRole != null)
            {
                await _keycloakClient[user.KeycloakId].Roles.Realm.PostAsync(new List<RoleRepresentation> { keycloakRole });
            }
        }

        _logger.LogInformation($"Role {role.Name} assigned to user {user.Username}");
    }

    // REMOVE ROLE FROM USER
    public async Task RemoveRoleFromUser(Guid userId, Guid roleId)
    {
        var userRole = await _dbContext.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole == null)
            return;

        // Remove from database
        _dbContext.UserRoles.Remove(userRole);
        await _dbContext.SaveChangesAsync();

        // Remove from Keycloak
        if (!string.IsNullOrEmpty(userRole.User.KeycloakId))
        {
            var keycloakRole = await _keycloakClient.Roles[userRole.Role.Name].GetAsync();
            if (keycloakRole != null)
            {
                await _keycloakClient[userRole.User.KeycloakId].Roles.Realm.DeleteAsync(new List<RoleRepresentation> { keycloakRole });
            }
        }

        _logger.LogInformation($"Role {userRole.Role.Name} removed from user {userRole.User.Username}");
    }
}
