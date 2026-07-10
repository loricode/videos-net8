

namespace DomainCore.DTOs.User
{
    public class UserDetailDto
    {
        public Guid UserId {  get; set; }

        public string Email { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;
    }
}
