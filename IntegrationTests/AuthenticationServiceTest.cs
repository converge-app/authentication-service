using System;
using System.Net.Http;
using Application;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Application.Utility.Models;
using Application.Models.DataTransferObjects;
using Application.Models.Entities;

namespace ApplicationUnitTests
{
    public class AuthenticationServiceTest : IClassFixture<WebApplicationFactory<StartupDevelopment>>
    {
        private readonly WebApplicationFactory<StartupDevelopment> _factory;

        public AuthenticationServiceTest(WebApplicationFactory<StartupDevelopment> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_Health_ping()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/health/ping");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task GEt_Health_ping()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/health/ping");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            var actual = JsonConvert.DeserializeObject<MessageObj>(await response.Content.ReadAsStringAsync());
            Assert.Equal("pong!", actual.Message);
        }

        [Fact]
        public async Task Post_Authentication_Register()
        {
            // Arrange
            Environment.SetEnvironmentVariable("USERS_SERVICE_HTTP", "users-service.api.converge-app.net");

            var client = _factory.CreateClient();
            UserRegistrationDto user = new UserRegistrationDto
            {
                FirstName = Guid.NewGuid().ToString(),
                LastName = Guid.NewGuid().ToString(),
                Email = Guid.NewGuid().ToString() + "test@gmail.com",
                Password = Guid.NewGuid().ToString()
            };
            var expected = user;

            // Act
            var response = await client.PostAsJsonAsync("/api/authentication/register", user);

            // Assert
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(await response.Content.ReadAsStringAsync());
            }
            var actual = JsonConvert.DeserializeObject<UserRegisteredDto>(await response.Content.ReadAsStringAsync());
            Assert.Equal(expected.Email, actual.Email);
        }

        [Fact]
        public async Task Post_Authentication_authenticate()
        {
            // Arrange
            Environment.SetEnvironmentVariable("USERS_SERVICE_HTTP", "users-service.api.converge-app.net");

            var client = _factory.CreateClient();

            UserRegistrationDto user = new UserRegistrationDto
            {
                FirstName = Guid.NewGuid().ToString(),
                LastName = Guid.NewGuid().ToString(),
                Email = Guid.NewGuid().ToString() + "test@gmail.com",
                Password = Guid.NewGuid().ToString()
            };

            var response = await client.PostAsJsonAsync("/api/authentication/register", user);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(await response.Content.ReadAsStringAsync());
            }

            UserAuthenticationDto authUser = new UserAuthenticationDto
            {
                Email = user.Email,
                Password = user.Password
            };

            // Act
            response = await client.PostAsJsonAsync("/api/authentication/authenticate", user);

            // Assert
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(await response.Content.ReadAsStringAsync());
            }
            var actual = JsonConvert.DeserializeObject<UserAuthenticatedDto>(await response.Content.ReadAsStringAsync());
            Assert.NotNull(actual);
        }


        /* [Fact]
         public async Task Get_AuthenticationById()
         {

             // Arrange
             Environment.SetEnvironmentVariable("USERS_SERVICE_HTTP", "users-service.api.converge-app.net");

             var client = _factory.CreateClient();

             UserAuthenticationDto authUser = new UserAuthenticationDto
             {
                 Email = "test18@gmail.com",
                 Password = "samir1234"
             };

             // Act
             //Assert
             var response = await client.GetAsync("/api/Authentication/5dd7a0d9891672000148c24b");

             var actual = await response.Content.ReadAsStringAsync();
             Assert.NotNull(actual);

         }*/


        [Fact]
        public async Task Update_AuthenticationById()
        {

            // Arrange
            Environment.SetEnvironmentVariable("USERS_SERVICE_HTTP", "users-service.api.converge-app.net");

            var client = _factory.CreateClient();

            UserRegistrationDto user = new UserRegistrationDto
            {
                FirstName = Guid.NewGuid().ToString(),
                LastName = Guid.NewGuid().ToString(),
                Email = Guid.NewGuid().ToString() + "test@gmail.com",
                Password = Guid.NewGuid().ToString()
            };

            var response = await client.PostAsJsonAsync("/api/authentication/register", user);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(await response.Content.ReadAsStringAsync());
            }

            UserAuthenticationDto authUser = new UserAuthenticationDto
            {
                Email = user.Email,
                Password = user.Password
            };

            // Act
            response = await client.PostAsJsonAsync("/api/authentication/authenticate", user);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorizationToken);


            // Act
            //Assert
            response = await client.PutAsJsonAsync("/api/Authentication/5dd7a0d9891672000148c24b", authUser);
            var actual = await response.Content.ReadAsStringAsync();
            Assert.NotNull(actual);

        }

    }

}