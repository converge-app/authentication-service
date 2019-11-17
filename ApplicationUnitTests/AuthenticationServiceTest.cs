using System;
using System.Net.Http;
using Application.Models.Entities;
using Application.Repositories;
using Application.Services;
using Moq;
using Xunit;

namespace ApplicationUnitTests
{
    public class AuthenticationServiceTest
    {
        [Fact]
        public void Authenticate_EmailOrPasswordWasIncorrect_NullEmailWasInCorrect()
        {
            // Arrange
            var mockEmail = "test@test.com";
            var authRepository = new Mock<IAuthenticationRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            authRepository.Setup(m => m.GetByEmail(It.IsAny<string>())).Returns(new AuthUser() { Email = mockEmail });
            var authService = new AuthenticationService(authRepository.Object, httpClientFactory.Object);

            // Act
            // Assert
            Assert.Null(authService.Authenticate("", "test"));
        }

        [Fact]
        public void Authenticate_EmailOrPasswordWasIncorrect_NullPasswordWasInCorrect()
        {
            // Arrange
            var mockEmail = "test@test.com";
            var authRepository = new Mock<IAuthenticationRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            authRepository.Setup(m => m.GetByEmail(It.IsAny<string>())).Returns(new AuthUser() { Email = mockEmail });
            var authService = new AuthenticationService(authRepository.Object, httpClientFactory.Object);

            // Act
            // Assert
            Assert.Null(authService.Authenticate("test@test.com", ""));
        }

        [Fact]
        public void Authenticate_EmailOrPasswordWasIncorrect_NullEmailAndPasswordWasInCorrect()
        {
            // Arrange
            var mockEmail = "test@test.com";
            var authRepository = new Mock<IAuthenticationRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            authRepository.Setup(m => m.GetByEmail(It.IsAny<string>())).Returns(new AuthUser() { Email = mockEmail });
            var authService = new AuthenticationService(authRepository.Object, httpClientFactory.Object);

            // Act
            // Assert
            Assert.Null(authService.Authenticate("", ""));
        }

        [Fact]
        public void Authenticate_EmailOrPasswordWasIncorrect_ThrowsEmail()
        {
            // Arrange
            var mockEmail = "test@test.com";
            var authRepository = new Mock<IAuthenticationRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            authRepository.Setup(m => m.GetByEmail(It.IsAny<string>())).Returns(new AuthUser() { Email = mockEmail });
            var authService = new AuthenticationService(authRepository.Object, httpClientFactory.Object);

            // Act
            // Assert
            Assert.Throws<Exception>(() => authService.Authenticate("test@test.com", "test"));
        }

        [Fact]
        public void Authenticate_EmailOrPasswordWasIncorrect_ThrowsVerfyPassword()
        {
            // Arrange
            var mockEmail = "test@test.com";
            var authRepository = new Mock<IAuthenticationRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            authRepository.Setup(m => m.GetByEmail(It.IsAny<string>())).Returns(new AuthUser() { Email = mockEmail, CurrentPassword = "$2b$10$WYLx9JXvzAEjdYyDMMwSPOTRpJrADsWYhQmN2d2JKOG" });
            var authService = new AuthenticationService(authRepository.Object, httpClientFactory.Object);

            // Act
            // Assert
            Assert.Throws<Exception>(() => authService.Authenticate("test@test.com", "Test1234"));
        }


        [Fact]
        public void Create_IsEmpty_Throws()
        {
            // Arrange
            var authRepository = new Mock<IAuthenticationRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            var authService = new AuthenticationService(authRepository.Object, httpClientFactory.Object);

            // Act
            // Assert
            Assert.Throws<Exception>(() => authService.Create(new AuthUser(), ""));
        }
        [Fact]
        public void Create_IsNull_Throws()
        {
            // Arrange
            var authRepository = new Mock<IAuthenticationRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            var authService = new AuthenticationService(authRepository.Object, httpClientFactory.Object);

            // Act
            // Assert
            Assert.Throws<Exception>(() => authService.Create(new AuthUser(), null));
        }

        [Fact]
        public void Create_GetById_UserAlreadyInTheSystem_Throws()
        {
            // Arrange
            var authRepository = new Mock<IAuthenticationRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            authRepository.Setup(m => m.GetById(It.IsAny<string>())).Returns(new AuthUser());
            var authService = new AuthenticationService(authRepository.Object, httpClientFactory.Object);

            // Act
            // Assert
            Assert.Throws<Exception>(() => authService.Create(new AuthUser(), "Test123"));
        }

        [Fact]
        public void Create_GetById_UserAlreadyInTheSystem_ReturnsAuthUser()
        {
            // Arrange
            var authRepository = new Mock<IAuthenticationRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            authRepository.Setup(m => m.GetById(It.IsAny<string>())).Returns((AuthUser)null);
            authRepository.Setup(m => m.Create(It.IsAny<AuthUser>())).Returns<AuthUser>(x => x);
            var authService = new AuthenticationService(authRepository.Object, httpClientFactory.Object);

            // Act
            var user = authService.Create(new AuthUser(), "Test1234");

            // Assert
            Assert.NotNull(user);

        }

        [Fact]
        public void Update_GetById_ThrowsArgumentNullException()
        {
            // Arrange
            var mockEmail = "test@test.com";
            var authRepository = new Mock<IAuthenticationRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            authRepository.Setup(m => m.GetByEmail(It.IsAny<string>())).Returns(new AuthUser() { Email = mockEmail, CurrentPassword = "$2b$10$WYLx9JXvzAEjdYyDMMwSPOTRpJrADsWYhQmN2d2JKOG" });
            authRepository.Setup(m => m.GetById(It.IsAny<string>())).Returns((AuthUser)null);
            var authService = new AuthenticationService(authRepository.Object, httpClientFactory.Object);

            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => authService.Update(new AuthUser()));

        }

        /*[Fact]
        public void Update_GetById_ThrowsException()
        {
            // Arrange
            var mockEmail = "test@test.com";
            var authRepository = new Mock<IAuthenticationRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            authRepository.Setup(m => m.GetByEmail(It.IsAny<string>())).Returns(new AuthUser() { Email = mockEmail, CurrentPassword = "$2b$10$WYLx9JXvzAEjdYyDMMwSPOTRpJrADsWYhQmN2d2JKOG" });
            authRepository.Setup(m => m.GetById(It.IsAny<string>())).Returns(new AuthUser() { Email = mockEmail });
            var authService = new AuthenticationService(authRepository.Object, httpClientFactory.Object);

            // Act
            // Assert
            Assert.Throws<Exception>(() => authService.Update(new AuthUser(), "test123455"));

        }*/



    }

}