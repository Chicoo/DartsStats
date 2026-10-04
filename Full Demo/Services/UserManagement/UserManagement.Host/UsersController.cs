using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserManagement;

[ApiController, Route("api/users"), Authorize(Policy = "AdminOnly")]
public sealed class UsersController(IUserDirectory directory, ILogger<UsersController> logger) : ControllerBase
{
    [HttpGet]
    public Task<UserPage> List(string? search, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1 || page > 100000 || pageSize < 1 || pageSize > 100 || search?.Length > 200)
            throw new DirectoryException(400, "Page must be positive, page size 1–100, and search at most 200 characters.");
        return directory.ListAsync(search, page, pageSize, ct);
    }

    [HttpGet("{id}")]
    public Task<UserDto> Get(string id, CancellationToken ct) => directory.GetAsync(id, ct);

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username)) throw new DirectoryException(400, "A username is required.");
        var user = await directory.CreateAsync(request, ct);
        Audit("created", user.Id);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UpdateUserRequest request, CancellationToken ct)
    { await directory.UpdateAsync(id, request, ct); Audit("updated", id); return NoContent(); }

    [HttpPut("{id}/enabled")]
    public async Task<IActionResult> Enable(string id, EnabledRequest request, CancellationToken ct)
    {
        if (!request.Enabled) GuardSelf(id);
        await directory.SetEnabledAsync(id, request.Enabled, ct);
        Audit(request.Enabled ? "enabled" : "disabled", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    { GuardSelf(id); await directory.DeleteAsync(id, ct); Audit("deleted", id); return NoContent(); }

    [HttpPut("{id}/roles")]
    public async Task<IActionResult> Roles(string id, RolesRequest request, CancellationToken ct)
    {
        if (!request.Roles.Contains("admin")) GuardSelf(id);
        await directory.SetRolesAsync(id, request.Roles, ct);
        Audit("roles changed", id);
        return NoContent();
    }

    [HttpPost("{id}/password-reset")]
    public async Task<IActionResult> Reset(string id, PasswordRequest request, CancellationToken ct)
    { await directory.ResetPasswordAsync(id, request.TemporaryPassword, ct); Audit("password reset", id); return NoContent(); }

    private void GuardSelf(string id)
    {
        if (User.FindFirstValue("sub") == id)
            throw new DirectoryException(400, "You cannot delete, disable, or remove your own administrator role.");
    }
    private void Audit(string action, string id) =>
        logger.LogInformation("Administrator {ActorId} {Action} user {UserId}", User.FindFirstValue("sub"), action, id);
}
