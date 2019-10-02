using System;
using System.Net.Http;
using System.Threading.Tasks;
using Application.Models.DataTransferObjects;
using Application.Models.Entities;
using Application.Repositories;
using Application.Utility.Models;

namespace Application.Services
{
    public interface IAuthenticationService
    {
        AuthUser Authenticate(string email, string password);
        AuthUser Create(AuthUser user, string password);
        void Update(AuthUser userParam, string password = null);
        Task<string> RegisterUser(string email, string firstName, string lastName);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthenticationRepository _userRepository;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthenticationService(IAuthenticationRepository authenticationRepository, IHttpClientFactory httpClientFactory)
        {
            _userRepository = authenticationRepository;
            _httpClientFactory = httpClientFactory;
        }

        public AuthUser Authenticate(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return null;

            var user = _userRepository.GetByEmail(email) ??
                       throw new Exception("Email or password was incorrect");
            
            if (!VerifyPasswordHash(password, user.CurrentPassword))
                throw new Exception("Email or password was incorrect");

            return user;
        }

        public AuthUser Create(AuthUser user, string password)
        {
            if (string.IsNullOrWhiteSpace(password)) // TODO: set proper rules
                throw new Exception("Password is required");

            if (_userRepository.GetById(user.Id) != null)
                throw new Exception("User is already in the system");

            CreatePasswordHash(password, out var passwordHash);

            user.CurrentPassword = passwordHash;

            return _userRepository.Create(user);
        }

        public void Update(AuthUser userParam, string password = null)
        {
            var user = _userRepository.GetById(userParam.Id) ??
                       throw new ArgumentNullException("_userRepository.GetById(userParam.Id)");

            if (userParam.Id != user.Id)
                if (_userRepository.GetById(userParam.Id) != null)
                    throw new Exception("Username was already taken");

            if (!string.IsNullOrWhiteSpace(password))
            {
                CreatePasswordHash(password, out var passwordHash);

                user.CurrentPassword = passwordHash;
            }

            _userRepository.Update(user.Id, user);
        }

        public async Task<string> RegisterUser(string email, string firstName, string lastName)
        {
            var client  = _httpClientFactory.CreateClient("UsersService");
            var host = Environment.GetEnvironmentVariable("USERS_SERVICE_HTTP");
            var uri = $"http://{host}/api/users";
            var content = new {FirstName = firstName, LastName = lastName, Email = email};
            var response = await client.PostAsJsonAsync(uri, content);

            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadAsAsync<UserRegisteredDto>()).Id;
            if ((await response.Content.ReadAsAsync<MessageObj>()).Message == "Email is already taken")
            {
                response = await client.GetAsync(uri + "/email/" + email);
                if (response.IsSuccessStatusCode)
                {
                    return (await response.Content.ReadAsAsync<UserRegisteredDto>()).Id;
                }
                throw new Exception("Something went wrong");
            }
            var str = await response.Content.ReadAsStringAsync();
            throw new Exception((await response.Content.ReadAsAsync<MessageObj>()).Message); 
        }
        
        private static void CreatePasswordHash(string password, out string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Password is missing");

            try
            {
                passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static bool VerifyPasswordHash(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Password is null or whitespace");
            if (string.IsNullOrWhiteSpace(storedHash))
                throw new Exception("Stored hash is null or whitespace");

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}