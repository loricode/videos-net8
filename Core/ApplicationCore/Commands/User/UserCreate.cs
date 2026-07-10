using ApplicationCore.Interfaces.User;
using DomainCore.DTOs.User;
using DomainCore.RequestData.User;
using FluentValidation;
using InfrastructureCore.Persistence.User;

namespace ApplicationCore.Commands.User
{
    public class UserCreate: IUserCreate
    {

        private readonly IUserRepository _userRepo;
        private readonly IValidator<UserData> _validator;
        public UserCreate(IUserRepository userRepo, IValidator<UserData> validator) {
            _userRepo = userRepo;
            _validator = validator;
        }

        public async Task<UserDto> CreateUser(UserData data)
        {

            await _validator.ValidateAndThrowAsync(data);

            return await _userRepo.CreateUser(data.Email, data.FullName, data.Password);

        }

    }
}
