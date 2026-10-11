using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Auth.Commands.Login;

/// <summary>
/// Handles agent authentication, verifying credentials and issuing JWT bearer tokens.
/// </summary>
public sealed class LoginCommandHandler(
    IAgentRepository agentRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService tokenService)
{
    public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        var agent = await agentRepository.GetByEmailAsync(request.Email.Trim(), ct);

        // AU-4: Wrong email and wrong password give the identical 401 and message. Inactive agents cannot log in.
        if (agent == null || !agent.IsActive || !passwordHasher.VerifyPassword(request.Password, agent.PasswordHash))
        {
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        var (token, expiresAtUtc) = tokenService.GenerateToken(agent);

        return new LoginResponse(
            token,
            expiresAtUtc,
            new CurrentAgentDto(agent.Id, agent.FullName, agent.Email));
    }
}
