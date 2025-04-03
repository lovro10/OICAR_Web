using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace CARSHARE_WEBAPP.Models
{
    public class JwtTokenProvider
    {

        public static string CreateToken(string secureKey, int expiration, string subject = null)
        {
         
            var tokenKey = Encoding.UTF8.GetBytes(secureKey);

          
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddMinutes(expiration),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(tokenKey),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            if (!string.IsNullOrEmpty(subject))
            {
                tokenDescriptor.Subject = new ClaimsIdentity(new System.Security.Claims.Claim[]
                {
                new System.Security.Claims.Claim(ClaimTypes.Name, subject),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Sub, subject),
                });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var serializedToken = tokenHandler.WriteToken(token);

            return serializedToken;
        }

    }
}

