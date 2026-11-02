using System.IdentityModel.Tokens.Jwt;

namespace Shared.Extensions
{
    public static class OAuthExtensions
    {
        public static bool IsValidOAuth(this string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var jwtToken = new JwtSecurityToken(token);
            if (jwtToken is null)
            {
                return false;
            }

            var now = DateTime.UtcNow;

            if (now >= jwtToken.ValidFrom && now < jwtToken.ValidTo.AddSeconds(-5))
            {
                return true;
            }

            return false;
        }
    }
}
