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
using System.Net.Http.Headers;
using Application.Utility.ClientLibrary.Authentication;
using ApplicationModulTests.TestUtility;

namespace ApplicationIntegerationsTests
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

            var user = AuthUtility.GenerateUser();

            await AuthUtility.RegisterUser(client, user, true);

            UserAuthenticationDto authUser = new UserAuthenticationDto
            {
                Email = user.Email,
                Password = user.Password
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/authentication/authenticate", authUser);

            // Assert
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(await response.Content.ReadAsStringAsync());
            }
            var actual = JsonConvert.DeserializeObject<UserAuthenticatedDto>(await response.Content.ReadAsStringAsync());
            Assert.NotNull(actual.Token);
        }


        [Fact]
        public async Task Get_AuthenticationById()
        {

            // Arrange
            Environment.SetEnvironmentVariable("USERS_SERVICE_HTTP", "users-service.api.converge-app.net");

            var client = _factory.CreateClient();

            var user = AuthUtility.GenerateUser();
            await AuthUtility.RegisterUser(client, user, true);
            var authUser = await AuthUtility.AuthenticateUser(client, AuthUtility.GenerateCredentials(user), true);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authUser.Token);

            // Act
            var response = await client.GetAsync("/api/authentication/" + authUser.Id);

            //Assert
            var actual = await response.Content.ReadAsAsync<UserRegisteredDto>();
            Assert.NotNull(actual);
        }


        /* [Fact]
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
             client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"TOKEN id=\"{authUser}\"");

             UserUpdateDto updateUser = new UserUpdateDto
             {
                 Id = "5ddbbad6bab6ba00018071c3",
                 Password = "Test123445"
             };

             // Act
             //Assert
             response = await client.PostAsJsonAsync("/api/Authentication/5ddbbad6bab6ba00018071c3", updateUser);
             var actual = JsonConvert.DeserializeObject<UserAuthenticatedDto>(await response.Content.ReadAsStringAsync());
             Assert.NotNull(actual);

         }*/

    }

}