using ApplicationCore.Interfaces.User;
using InfrastructureCore.Persistence.User;

namespace ApplicationCore.Querys.User
{
    public class UserValidateToken : IUserValidatetoken
    {

        private readonly IUserRepository _userRepo;

        public UserValidateToken(IUserRepository userRepository)
        {
            _userRepo = userRepository;
        }
        public Task<bool> Validate(string token)
        {
            return _userRepo.Validate(token);

        }
    }
}
