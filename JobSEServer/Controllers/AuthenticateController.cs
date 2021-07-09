using JobSEServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace JobSEServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private readonly IOptions<JWTAuthOption> options;

        public AuthenticateController(IOptions<JWTAuthOption> options)
        {
            this.options = options;
        }

        [HttpPost]
        public IActionResult AuthenticateAsync([FromForm] string apiKey)
        {
            var clientKey = options.Value.ApiKeys.Find(key => key.Key == apiKey);
            if(clientKey == null || DateTime.Now >= clientKey.ExpireTime)
            {
                return Unauthorized("API Key is invalid or outdated!");
            }

            //Claims
            var authClaims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub,clientKey.Audience),
                new Claim(JwtRegisteredClaimNames.Exp, clientKey.ExpireTime.ToString())
            };
            IdentityModelEventSource.ShowPII = true;
            //Token
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.PrivateKey));
            var token = new JwtSecurityToken(
                   issuer: options.Value.Issuer,
                   audience: clientKey.Audience,
                   expires: DateTime.Now.AddDays(7) < clientKey.ExpireTime ? DateTime.Now.AddDays(7) : clientKey.ExpireTime,
                   claims: authClaims,
                   signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                   );

            //Reture
            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpirationTime = token.ValidTo
            });
        }
    }
}
