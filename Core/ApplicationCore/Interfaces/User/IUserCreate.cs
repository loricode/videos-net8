using DomainCore.DTOs.User;
using DomainCore.RequestData.User;

namespace ApplicationCore.Interfaces.User
{
    public interface IUserCreate
    {
        Task<UserDto> CreateUser(UserData data);

    }
}
