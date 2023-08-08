using JwtAuthASP.Configurations;
using JwtAuthASP.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JwtAuthASP.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class AuthController: ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _congiguration;

        public AuthController(UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _userManager= userManager;
            _congiguration = configuration;
        }

        [Authorize]
        [HttpGet("hola")]
        public async Task<ActionResult<string>> Hola()
        {
            var usuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return "hola xd "+usuario;
        } 


        [HttpPost("register")]
        public async Task<IActionResult> CreateUser([FromBody] RegisterUserDto registerUser)
        {
            if(ModelState.IsValid)
            {
                var user_exist = await _userManager.FindByEmailAsync(registerUser.Email);
                if (user_exist != null)
                {
                    return BadRequest(new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Email already exist"
                        }
                    });
                }
                var new_user = new IdentityUser()
                {
                    Email = registerUser.Email,
                    UserName = registerUser.Email
                };
                var is_created = await _userManager.CreateAsync(new_user, registerUser.Password);
                if (is_created.Succeeded)
                {
                    //return token
                    var token = GeneratedToken(new_user);
                    return Ok(new AuthResult()
                    {
                        Result = true,
                        Token = token
                    });
                }
                else
                {
                    return BadRequest();
                }

            }
            return BadRequest();

        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginUser([FromBody] LoginUserDto loginUser)
        {
            if (ModelState.IsValid)
            {
                var exist_user = await _userManager.FindByEmailAsync(loginUser.Email);
                if(exist_user == null)
                {
                    return BadRequest(new AuthResult()
                    {
                        Result = true,
                        Errors = new List<string>()
                        {
                            "email not found"
                        }
                    });
                }
                var Compare_Password = await _userManager.CheckPasswordAsync(exist_user, loginUser.Password);
                if (!Compare_Password)
                {
                    return BadRequest(new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "password dont match"
                        }
                    });
                }
                var jwtToken = GeneratedToken(exist_user);
                return Ok(new AuthResult()
                {
                    Result = true,
                    Token = jwtToken
                });

            }

            return BadRequest(new AuthResult()
            {
                Errors = new List<string>()
                    {
                        "invalid user"
                    }
            });
        }

        private string GeneratedToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.UTF8.GetBytes(_congiguration.GetSection("JwtConfig:Secret").Value);

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Email, value:user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat,DateTime.Now.ToUniversalTime().ToString()), 
                }),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),SecurityAlgorithms.HmacSha256),
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            return jwtToken;
        }
    }
}
