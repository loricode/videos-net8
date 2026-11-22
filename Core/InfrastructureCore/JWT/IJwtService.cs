namespace InfrastructureCore.JWT
{
    public interface IJwtService
    {
        string GenerateToken(Guid userId, string email, IEnumerable<string> roles, string fullName = "");

        bool ValidateTokenJwt(string token);
    }
}
