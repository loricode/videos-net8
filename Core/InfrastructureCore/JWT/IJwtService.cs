using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureCore.JWT
{
    public interface IJwtService
    {
        string GenerateToken(Guid userId, string email);

        bool ValidateTokenJwt(string token);

    }
}
