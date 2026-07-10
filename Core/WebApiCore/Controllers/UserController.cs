using ApplicationCore.Interfaces.User;
using ApplicationCore.RequestData.User;
using DomainCore.DTOs.User;
using DomainCore.RequestData.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApiCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {

        private readonly IUserCreate _iUserCommand;

        private readonly IUserlogin _iUserLoginCommand;

        private readonly IUserValidatetoken _iUserValidatetokenQuery;

        public UserController(IUserCreate iUserCommand, IUserlogin userLoginCommand, IUserValidatetoken userValidatetokenQuery)
        {
            _iUserCommand = iUserCommand;
            _iUserLoginCommand = userLoginCommand;
            _iUserValidatetokenQuery = userValidatetokenQuery;
        }

        [HttpPost("CreateUser")]
        [AllowAnonymous]
        public async Task<UserDto> CreateUser(UserData data)
        {
            return await _iUserCommand.CreateUser(data);
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<UserDetailDto> Login(UserLoginData data)
        {
            return await _iUserLoginCommand.Login(data);
        }

        [HttpGet("Validate")]
        public async Task<bool> Validate()
        {

            var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

            if (string.IsNullOrEmpty(authorizationHeader))
            {
                throw new UnauthorizedAccessException("Token no enviado");
            }
            
            var token = authorizationHeader.Replace("Bearer ", "");

            return await _iUserValidatetokenQuery.Validate(token);

        }

    }
}
