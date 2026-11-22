using ApplicationCore.Interfaces.User;
using ApplicationCore.RequestData.User;
using DomainCore.Constants;
using DomainCore.DTOs.User;
using DomainCore.RequestData.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApiCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserCreate _userCreate;
        private readonly IUserlogin _userLogin;
        private readonly IUserValidatetoken _userValidateToken;

        public UserController(IUserCreate userCreate, IUserlogin userLogin, IUserValidatetoken userValidateToken)
        {
            _userCreate = userCreate;
            _userLogin = userLogin;
            _userValidateToken = userValidateToken;
        }

        /// <summary>Alta de otro instructor. Solo un instructor puede crearlo.</summary>
        [HttpPost("CreateUser")]
        [Authorize(Roles = Roles.Instructor)]
        public Task<UserDto> CreateUser(UserData data)
        {
            return _userCreate.CreateUser(data, Roles.Instructor);
        }

        /// <summary>Registro público de estudiantes.</summary>
        [HttpPost("Register")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public Task<UserDto> Register(UserData data)
        {
            return _userCreate.CreateUser(data, Roles.Student);
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public Task<UserDetailDto> Login(UserLoginData data)
        {
            return _userLogin.Login(data);
        }

        [HttpGet("Validate")]
        [Authorize]
        public Task<bool> Validate()
        {
            var token = Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "") ?? string.Empty;
            return _userValidateToken.Validate(token);
        }
    }

    public static class RateLimitPolicies
    {
        public const string Auth = "auth";
    }
}
