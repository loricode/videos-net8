using DomainCore.DTOs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureCore.Persistence.User
{
    public interface IUserRepository
    {
        Task<UserDto> CreateUser(string email, string fullName, string password);

        Task<UserDetailDto> Login(string email, string password);

        Task<bool> Validate(string token);

    }
}
