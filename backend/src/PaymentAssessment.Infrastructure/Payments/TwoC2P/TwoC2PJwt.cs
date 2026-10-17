using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace PaymentAssessment.Infrastructure.Payments.TwoC2P;

public class TwoC2PJwt
{
    private static readonly JsonWebTokenHandler Handler = new();

    public static string Sign(string payloadJson, string secretKey)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        return Handler.CreateToken(payloadJson, signingCredentials);
    }
    public static async Task<string> VerifyAndDecodeAsync(string token, string secretKey)
    {
        var validationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            RequireExpirationTime = false
        };

        var result = await Handler.ValidateTokenAsync(token, validationParameters);
        if (!result.IsValid)
        {
            throw new SecurityTokenException("Invalid token", result.Exception);
        }
        var jwtToken = (JsonWebToken)result.SecurityToken;
        return Base64UrlEncoder.Decode(jwtToken.EncodedPayload);
    }
}
