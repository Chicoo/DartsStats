using System.ComponentModel.DataAnnotations;

namespace UserManagement;

public record UserDto(string Id, string Username, string? FirstName, string? LastName,
    string? Email, bool Enabled, string[] Roles);
public record UserPage(UserDto[] Items, int Total, int Page, int PageSize);
public record CreateUserRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Username,
    [StringLength(100)] string? FirstName,
    [StringLength(100)] string? LastName,
    [EmailAddress, StringLength(254)] string? Email,
    bool Enabled,
    [Required, StringLength(128, MinimumLength = 8)] string TemporaryPassword);
public record UpdateUserRequest(
    [StringLength(100)] string? FirstName,
    [StringLength(100)] string? LastName,
    [EmailAddress, StringLength(254)] string? Email);
public record EnabledRequest(bool Enabled);
public record RolesRequest([Required] string[] Roles);
public record PasswordRequest([Required, StringLength(128, MinimumLength = 8)] string TemporaryPassword);

public interface IUserDirectory
{
    Task<UserPage> ListAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<UserDto> GetAsync(string id, CancellationToken ct);
    Task<UserDto> CreateAsync(CreateUserRequest user, CancellationToken ct);
    Task UpdateAsync(string id, UpdateUserRequest user, CancellationToken ct);
    Task SetEnabledAsync(string id, bool enabled, CancellationToken ct);
    Task DeleteAsync(string id, CancellationToken ct);
    Task SetRolesAsync(string id, string[] roles, CancellationToken ct);
    Task ResetPasswordAsync(string id, string password, CancellationToken ct);
    Task RevokeSessionsAsync(string id, CancellationToken ct);
    Task<bool> IsSessionActiveAsync(string id, string sessionId, CancellationToken ct);
}

public sealed class DirectoryException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

public sealed class KeycloakOptions
{
    public string BaseUrl { get; set; } = "";
    public string Realm { get; set; } = "dartsstats";
    public string ClientId { get; set; } = "user-management-web";
    public string ClientSecret { get; set; } = "";
    public string AdminClientId { get; set; } = "user-management-admin";
    public string AdminClientSecret { get; set; } = "";
    public string PublicUrl { get; set; } = "";
    public string? PublicAuthority { get; set; }
    public string DartsUrl { get; set; } = "";
    public string Authority => $"{BaseUrl.TrimEnd('/')}/realms/{Uri.EscapeDataString(Realm)}";
}
