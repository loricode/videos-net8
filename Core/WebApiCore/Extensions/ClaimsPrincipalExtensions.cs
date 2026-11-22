using System.Security.Claims;

namespace WebApiCore.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>Id del usuario autenticado (claim NameIdentifier del JWT).</summary>
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id)
                ? id
                : throw new UnauthorizedAccessException("Token sin identificador de usuario.");
        }
    }

    public static class UploadLimits
    {
        public const long VideoBytes = 600L * 1024 * 1024;
        public const long ImageBytes = 12L * 1024 * 1024;
    }
}
