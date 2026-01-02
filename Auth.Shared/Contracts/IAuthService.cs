using Auth.Shared.DTO;
using Auth.Shared.Models;

namespace Auth.Shared.Contracts
{
    public interface IAuthService
    {
        // This MUST be a single parameter 'LoginUserDTO' to match your controller
        Task<LoginResult> LoginAsync(LoginUserDTO dto);
        Task<bool> LogoutAsync();
    }
}