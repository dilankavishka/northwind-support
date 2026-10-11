using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Auth.Commands.Login;
using SupportDesk.Application.Features.Auth.Queries.GetCurrentAgent;

namespace SupportDesk.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    /// <summary>Authenticates an agent and returns a JWT token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<LoginResponse> Login(
        [FromBody] LoginRequest request,
        [FromServices] LoginCommandHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(request, ct);

    /// <summary>Returns the profile of the current authenticated agent.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentAgentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<CurrentAgentDto> Me(
        [FromServices] GetCurrentAgentQueryHandler handler,
        CancellationToken ct)
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out var agentId))
        {
            throw new AuthenticationFailedException("Invalid token claims.");
        }

        return handler.HandleAsync(agentId, ct);
    }
}
