using DomainCore.DTOs.User;

namespace InfrastructureCore.Persistence.User
{
    public interface IUserRepository
    {
        /// <summary>Crea un usuario con el rol indicado (Instructor o Student).</summary>
        Task<UserDto> CreateUser(string email, string fullName, string password, string role);
        Task<UserDetailDto> Login(string email, string password);
        Task<bool> Validate(string token);
    }
}
