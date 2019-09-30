using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Application.Helpers;
using Application.Models.Entities;
using Application.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services
{
    public interface IUserService
    {
        User Authenticate(string id, string password);
        User Create(User user, string password);
        void Update(User userParam, string password = null);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public User Authenticate(string id, string password)
        {
            if (id == string.Empty || string.IsNullOrEmpty(password))
                return null;

            var user = _userRepository.GetById(id) ??
                       throw new ArgumentNullException("_userRepository.GetByUsername(username)");

            if (!VerifyPasswordHash(password, user.CurrentPassword))
                return null;

            return user;
        }

        public User Create(User user, string password)
        {
            if (string.IsNullOrWhiteSpace(password)) // TODO: set proper rules
                throw new Exception("Password is required");

            if (_userRepository.GetById(user.Id) != null)
                throw new Exception("username i already taken");

            CreatePasswordHash(password, out var passwordHash);

            user.CurrentPassword = passwordHash;

            return _userRepository.Create(user);
        }

        public void Update(User userParam, string password = null)
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