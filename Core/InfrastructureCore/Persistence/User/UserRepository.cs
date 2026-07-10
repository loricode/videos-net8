using DomainCore.DTOs.User;
using DomainCore.Exceptions;
using InfrastructureCore.JWT;
using InfrastructureCore.Persistence.EF;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureCore.Persistence.User
{
    public class UserRepository : IUserRepository
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;

        public UserRepository(UserManager<ApplicationUser> userManager, IJwtService jwtService) 
        { 
            _userManager = userManager;
            _jwtService = jwtService;
        }

        public async Task<UserDto> CreateUser(string email, string fullName, string password)
        {

            var found = await _userManager.FindByEmailAsync(email);

            if(found != null) throw new ConflictException("el correo ya existe.");

            ApplicationUser userApp = new()
            {
                Email = email,
                FullName = fullName,
                UserName = email.Split("@")[0]
            };

            var result = await _userManager.CreateAsync(userApp, password);

            if (!result.Succeeded)
            {
                throw new IdentityValidationException(result.Errors.Select(e => e.Description));
            }

            return new UserDto { Email = email };
        }

        public async Task<UserDetailDto> Login(string email, string password)
        {

            var found = await _userManager.FindByEmailAsync(email) ?? throw new NotFoundException("No existe el correo.");

            var validUser = await _userManager.CheckPasswordAsync(found, password);

            if (!validUser) throw new NotFoundException("Clave incorrecta.");

            var token = _jwtService.GenerateToken(found.Id, email);
          
            return new UserDetailDto {
                Token = token,
                Email = found.Email ?? string.Empty,
                UserId = found.Id,
                UserName = found.UserName ?? string.Empty
            };
        }

        public Task<bool> Validate(string token)
        {
            var isValidToekn = _jwtService.ValidateTokenJwt(token);

            return Task.FromResult(isValidToekn);
        }
    }
}
