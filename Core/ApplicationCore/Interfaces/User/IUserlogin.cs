using ApplicationCore.RequestData.User;
using DomainCore.DTOs.User;

namespace ApplicationCore.Interfaces.User
{
    public interface IUserlogin
    {
        Task<UserDetailDto> Login(UserLoginData data);

    }
}
