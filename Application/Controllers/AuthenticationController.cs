using System;
using System.Collections;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Application.Models;
using Application.Models.DataTransferObjects;
using Application.Models.Entities;
using Application.Repositories;
using Application.Services;
using Application.Utility.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Application.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly IMapper _mapper;
        private AppSettings _appSettings;

        public AuthenticationController(IAuthenticationService authenticationService, IAuthenticationRepository authenticationRepository, IMapper mapper,
            IOptions<AppSettings> appSettings)
        {
            _authenticationService = authenticationService;
            _authenticationRepository = authenticationRepository;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody] UserAuthenticationDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                    {Message = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)});

            try
            {
                var user = _authenticationService.Authenticate(userDto.Email, userDto.Password);

                if (user == null)
                    return BadRequest(new {message = "Username or password is incorrect"});

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Name, user.Id.ToString()),
                    }),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature),
                    Audience = "auth"
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Ok(new UserAuthenticatedDto()
                {
                    Id = user.Id,
                    Token = tokenString
                });
            }
            catch (Exception e)
            {
                return BadRequest(new MessageObj(e.Message));
            }
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                    {Message = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)});

            try
            {
                var user = _mapper.Map<AuthUser>(userDto);

                string id = await _authenticationService.RegisterUser(userDto.Email, userDto.FirstName, userDto.LastName);
                user.Id = id;

                var createdUser = _authenticationService.Create(user, userDto.Password);
                var createdUserDto = _mapper.Map<UserRegisteredDto>(createdUser);
                return Ok(createdUserDto);
            }
            catch (Exception e)
            {
                return BadRequest(new MessageObj(e.Message));
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetById(string id)
        {
            try
            {
                var user = _authenticationRepository.GetById(id);
                var userDto = _mapper.Map<UserDto>(user);
                return Ok(userDto);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return BadRequest(new MessageObj(e.Message));
            }
        }

        [HttpPut("{id}")]
        public IActionResult Update([FromRoute] string id, [FromBody] UserUpdateDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                    {Message = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)});

            try
            {
                var user = _mapper.Map<AuthUser>(userDto);
                user.Id = id;

                _authenticationService.Update(user, userDto.Password);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(new {Message = e.Message});
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            try
            {
                _authenticationRepository.Remove(id);

                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return BadRequest(e.Message);
            }
        }
    }
}