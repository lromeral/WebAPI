using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebAPI.Models;
using WebAPI.ModelsInfo;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
 

    public class AuthController : ControllerBase
    {

        private readonly DBWebAPIContext db;

        public AuthController(DBWebAPIContext context)
        {
            db = context;
        }

        [HttpPost, Route("login")]
        public async Task<ActionResult> Login([FromBody]InfoLogin user)
        {
            try
            {
                if (user == null)
                {
                    return BadRequest("Credenciales vacías");
                }

                byte[] base64EncodedBytes = Convert.FromBase64String(user.Password);
                string pass = Encoding.UTF8.GetString(base64EncodedBytes, 0, base64EncodedBytes.Length);
                string passMD5 = this.EncodePasswordMd5(pass);

                Usuarios usuario = await db.Usuarios.Where(x => x.UserName == user.Username && x.Password == passMD5).FirstOrDefaultAsync();
                //var usuario = await (from x in db.Usuarios
                //                      where x.IdUsuario == user.Username && x.Password == passMD5
                //                      select x).FirstOrDefaultAsync();
                if (usuario != null)
                {
                    var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("CursoWebAngular@2019"));
                    var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

                    var tokeOptions = new JwtSecurityToken(
                        issuer: "http://localhost:44314",
                        audience: "http://localhost:4200",
                        claims: new List<Claim>{
                         new Claim(ClaimTypes.Name, usuario.UserName),
                         new Claim(ClaimTypes.Country, "PaisDeNuncaJamas")
                        },
                        expires: DateTime.Now.AddMinutes(30),
                        signingCredentials: signinCredentials
                    );

                    var tokenString = new JwtSecurityTokenHandler().WriteToken(tokeOptions);
                    return Ok(new { Token = tokenString });
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }

        [HttpPost, Route("logout")]
        public async Task<ActionResult> Logout([FromBody]InfoLogout data)
        {
            return Ok();
        }

        private string EncodePasswordMd5(string pass) //Encrypt using MD5    
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(pass);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}