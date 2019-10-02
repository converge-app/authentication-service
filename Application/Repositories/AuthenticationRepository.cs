using System;
using System.Collections.Generic;
using Application.Database;
using Application.Models.Entities;
using MongoDB.Driver;

namespace Application.Repositories
{
    public interface IAuthenticationRepository
        {
            List<AuthUser> Get();
            AuthUser GetById(string id);
            AuthUser Create(AuthUser authUser);
            void Update(string id, AuthUser authUserIn);
            void Remove(AuthUser authUserIn);
            void Remove(string id);
            AuthUser GetByEmail(string email);
        }
    
    public class AuthenticationRepository : IAuthenticationRepository
    {
        private readonly IDatabaseContext dbContext;
        private IMongoCollection<AuthUser> _authUsers;

        public AuthenticationRepository(IDatabaseContext dbContext)
        {
            this.dbContext = dbContext;
            if (dbContext.IsConnectionOpen()) _authUsers = dbContext.Users;
        }

        public List<AuthUser> Get()
        {
            return _authUsers.Find(user => true).ToList();
        }

        public AuthUser GetById(string id)
        {
            return _authUsers.Find<AuthUser>(user => user.Id == id).FirstOrDefault();
        }

        public AuthUser GetByEmail(string email)
        {
            return _authUsers.Find(user => user.Email == email).FirstOrDefault();
        }

        public AuthUser Create(AuthUser authUser)
        {
            authUser.Passwords.Add(authUser.CurrentPassword);
            _authUsers.InsertOne(authUser);
            return authUser;
        }

        public void Update(string id, AuthUser authUserIn)
        {
            if (authUserIn.Passwords.Contains(authUserIn.CurrentPassword))
            {
                authUserIn.Passwords.Add(authUserIn.CurrentPassword);
                _authUsers.ReplaceOne(user => user.Id == id, authUserIn);
            }
            else
            {
                throw new Exception("Password was used previously");
            }
        }

        public void Remove(AuthUser authUserIn)
        {
            _authUsers.DeleteOne(user => user.Id == authUserIn.Id);
        }

        public void Remove(string id)
        {
            _authUsers.DeleteOne(user => user.Id == id);
        }
    }
}