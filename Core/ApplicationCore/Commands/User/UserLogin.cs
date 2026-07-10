using ApplicationCore.Interfaces.User;
using ApplicationCore.RequestData.User;
using DomainCore.DTOs.User;
using FluentValidation;
using InfrastructureCore.Persistence.User;

namespace ApplicationCore.Commands.User
{
    public class UserLogin : IUserlogin
    {

        private IUserRepository _userRepo;
        private readonly IValidator<UserLoginData> _validator;

        public UserLogin(IUserRepository userRepo, IValidator<UserLoginData> validator)
        {
            _userRepo = userRepo;
            _validator = validator;
        }

        public async Task<UserDetailDto> Login(UserLoginData data)
        {

            await _validator.ValidateAndThrowAsync(data);

            return await _userRepo.Login(data.Email, data.Password);
        }
    }
}
