using DomainCore.DTOs.User;
using DomainCore.Exceptions;
using InfrastructureCore.JWT;
using InfrastructureCore.Persistence.EF;
using Microsoft.AspNetCore.Identity;

namespace InfrastructureCore.Persistence.User
{
    public class UserRepository : IUserRepository
    {
        private const string InvalidCredentials = "Correo o contraseña incorrectos.";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IJwtService _jwtService;

        public UserRepository(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, IJwtService jwtService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
        }

        public async Task<UserDto> CreateUser(string email, string fullName, string password, string role)
        {
            var found = await _userManager.FindByEmailAsync(email);

            if (found != null) throw new ConflictException("El correo ya está registrado.");

            ApplicationUser userApp = new()
            {
                Email = email,
                FullName = fullName,
                UserName = email
            };

            var result = await _userManager.CreateAsync(userApp, password);

            if (!result.Succeeded)
            {
                throw new IdentityValidationException(result.Errors.Select(e => e.Description));
            }

            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));

            await _userManager.AddToRoleAsync(userApp, role);

            return new UserDto { Email = email };
        }

        public async Task<UserDetailDto> Login(string email, string password)
        {
            var found = await _userManager.FindByEmailAsync(email) ?? throw new NotFoundException(InvalidCredentials);

            if (await _userManager.IsLockedOutAsync(found))
                throw new NotFoundException("Cuenta bloqueada temporalmente por intentos fallidos. Intenta más tarde.");

            if (!await _userManager.CheckPasswordAsync(found, password))
            {
                await _userManager.AccessFailedAsync(found);
                throw new NotFoundException(InvalidCredentials);
            }

            await _userManager.ResetAccessFailedCountAsync(found);

            var roles = await _userManager.GetRolesAsync(found);
            var token = _jwtService.GenerateToken(found.Id, found.Email ?? email, roles, found.FullName);

            return new UserDetailDto
            {
                Token = token,
                Email = found.Email ?? string.Empty,
                UserId = found.Id,
                UserName = found.UserName ?? string.Empty,
                FullName = found.FullName,
                Roles = roles
            };
        }

        public Task<bool> Validate(string token)
        {
            return Task.FromResult(_jwtService.ValidateTokenJwt(token));
        }
    }
}
