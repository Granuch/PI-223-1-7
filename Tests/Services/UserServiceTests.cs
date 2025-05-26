using AutoMapper;
using BLL.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PI_223_1_7.Models;
using Tests.TestHelpers;

namespace Tests.Services
{
    [TestFixture]
    public class UserServiceTests
    {
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<ILogger<UserService>> _mockLogger;
        private IMapper _mapper;
        private UserService _userService;

        [SetUp]
        public void Setup()
        {
            // Мокування UserManager
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Мокування RoleManager
            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
            _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                roleStoreMock.Object, null, null, null, null);

            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _mapper = MapperHelper.CreateMapper();

            _userService = new UserService(
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockConfiguration.Object,
                _mapper,
                _mockLogger.Object);
        }

        // Тести для отримання користувачів

        [Test]
        public async Task GetUserByIdAsync_ExistingId_ReturnsUser()
        {
            // Arrange
            var userId = "user1";
            var user = TestDataFactory.CreateUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "RegisteredUser" });

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(userId));
            Assert.That(result.Email, Is.EqualTo(user.Email));
            Assert.That(result.Roles.First(), Is.EqualTo("RegisteredUser"));
        }

        [Test]
        public async Task GetUserByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            var nonExistingId = "nonexisting";
            _mockUserManager.Setup(m => m.FindByIdAsync(nonExistingId))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _userService.GetUserByIdAsync(nonExistingId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetUserByEmailAsync_ExistingEmail_ReturnsUser()
        {
            // Arrange
            var email = "user1@example.com";
            var user = TestDataFactory.CreateUser("user1", email);

            _mockUserManager.Setup(m => m.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "RegisteredUser" });

            // Act
            var result = await _userService.GetUserByEmailAsync(email);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Email, Is.EqualTo(email));
            Assert.That(result.Roles.First(), Is.EqualTo("RegisteredUser"));
        }

        [Test]
        public async Task GetUserByEmailAsync_NonExistingEmail_ReturnsNull()
        {
            // Arrange
            var nonExistingEmail = "nonexisting@example.com";
            _mockUserManager.Setup(m => m.FindByEmailAsync(nonExistingEmail))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _userService.GetUserByEmailAsync(nonExistingEmail);

            // Assert
            Assert.That(result, Is.Null);
        }

        // Тести для створення користувачів

        [Test]
        public async Task CreateUserAsync_ValidRequest_CreatesUser()
        {
            // Arrange
            var request = TestDataFactory.CreateUserRequest();

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser)null);
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockRoleManager.Setup(m => m.RoleExistsAsync("RegisteredUser"))
                .ReturnsAsync(true);
            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "RegisteredUser"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.CreateUserAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password), Times.Once);
            _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "RegisteredUser"), Times.Once);
        }

        [Test]
        public async Task CreateUserAsync_ExistingEmail_ReturnsError()
        {
            // Arrange
            var request = TestDataFactory.CreateUserRequest();
            var existingUser = TestDataFactory.CreateUser("existing", request.Email);

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _userService.CreateUserAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Does.Contain("вже існує"));
        }

        [Test]
        public async Task CreateAdminAsync_ValidRequest_CreatesAdminUser()
        {
            // Arrange
            var request = TestDataFactory.CreateUserRequest("admin@example.com");

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser)null);
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockRoleManager.Setup(m => m.RoleExistsAsync("Administrator"))
                .ReturnsAsync(true);
            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Administrator"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.CreateAdminAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password), Times.Once);
            _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Administrator"), Times.Once);
        }

        [Test]
        public async Task CreateManagerAsync_ValidRequest_CreatesManagerUser()
        {
            // Arrange
            var request = TestDataFactory.CreateUserRequest("manager@example.com");

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser)null);
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockRoleManager.Setup(m => m.RoleExistsAsync("Manager"))
                .ReturnsAsync(true);
            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Manager"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.CreateManagerAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password), Times.Once);
            _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Manager"), Times.Once);
        }

        // Тести для управління користувачами

        [Test]
        public async Task UpdateUserAsync_ValidRequest_UpdatesUser()
        {
            // Arrange
            var userId = "user1";
            var user = TestDataFactory.CreateUser(userId);
            var updateRequest = TestDataFactory.CreateUpdateUserRequest();

            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.UpdateUserAsync(userId, updateRequest);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(user.FirstName, Is.EqualTo(updateRequest.FirstName));
            Assert.That(user.LastName, Is.EqualTo(updateRequest.LastName));
            Assert.That(user.Email, Is.EqualTo(updateRequest.Email));
            Assert.That(user.PhoneNumber, Is.EqualTo(updateRequest.PhoneNumber));
            _mockUserManager.Verify(m => m.UpdateAsync(user), Times.Once);
        }

        [Test]
        public async Task UpdateUserAsync_NonExistingUser_ReturnsError()
        {
            // Arrange
            var nonExistingId = "nonexisting";
            var updateRequest = TestDataFactory.CreateUpdateUserRequest();

            _mockUserManager.Setup(m => m.FindByIdAsync(nonExistingId))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _userService.UpdateUserAsync(nonExistingId, updateRequest);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Does.Contain("не знайдено"));
        }

        [Test]
        public async Task DeleteUserAsync_ExistingUser_DeletesUser()
        {
            // Arrange
            var userId = "user1";
            var user = TestDataFactory.CreateUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _mockUserManager.Setup(m => m.DeleteAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.DeleteUserAsync(userId);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            _mockUserManager.Verify(m => m.DeleteAsync(user), Times.Once);
        }

        [Test]
        public async Task DeleteUserAsync_NonExistingUser_ReturnsError()
        {
            // Arrange
            var nonExistingId = "nonexisting";

            _mockUserManager.Setup(m => m.FindByIdAsync(nonExistingId))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _userService.DeleteUserAsync(nonExistingId);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Does.Contain("не знайдено"));
        }

        [Test]
        public async Task ChangeUserPasswordAsync_ExistingUser_ChangesPassword()
        {
            // Arrange
            var userId = "user1";
            var user = TestDataFactory.CreateUser(userId);
            var newPassword = "NewPassword123!";
            var token = "resetToken";

            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _mockUserManager.Setup(m => m.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync(token);
            _mockUserManager.Setup(m => m.ResetPasswordAsync(user, token, newPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.ChangeUserPasswordAsync(userId, newPassword);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            _mockUserManager.Verify(m => m.ResetPasswordAsync(user, token, newPassword), Times.Once);
        }

        [Test]
        public async Task ChangeUserPasswordAsync_NonExistingUser_ReturnsError()
        {
            // Arrange
            var nonExistingId = "nonexisting";
            var newPassword = "NewPassword123!";

            _mockUserManager.Setup(m => m.FindByIdAsync(nonExistingId))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _userService.ChangeUserPasswordAsync(nonExistingId, newPassword);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Does.Contain("не знайдено"));
        }

        // Тести для управління ролями

        [Test]
        public async Task AssignRoleToUserAsync_ValidData_AssignsRole()
        {
            // Arrange
            var userId = "user1";
            var roleName = "Administrator";
            var user = TestDataFactory.CreateUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _mockRoleManager.Setup(m => m.RoleExistsAsync(roleName))
                .ReturnsAsync(true);
            _mockUserManager.Setup(m => m.IsInRoleAsync(user, roleName))
                .ReturnsAsync(false);
            _mockUserManager.Setup(m => m.AddToRoleAsync(user, roleName))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.AssignRoleToUserAsync(userId, roleName);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            _mockUserManager.Verify(m => m.AddToRoleAsync(user, roleName), Times.Once);
        }

        [Test]
        public async Task AssignRoleToUserAsync_NonExistingUser_ReturnsError()
        {
            // Arrange
            var nonExistingId = "nonexisting";
            var roleName = "Administrator";

            _mockUserManager.Setup(m => m.FindByIdAsync(nonExistingId))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _userService.AssignRoleToUserAsync(nonExistingId, roleName);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Does.Contain("не знайдено"));
        }

        [Test]
        public async Task AssignRoleToUserAsync_NonExistingRole_ReturnsError()
        {
            // Arrange
            var userId = "user1";
            var nonExistingRole = "NonExistingRole";
            var user = TestDataFactory.CreateUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _mockRoleManager.Setup(m => m.RoleExistsAsync(nonExistingRole))
                .ReturnsAsync(false);

            // Act
            var result = await _userService.AssignRoleToUserAsync(userId, nonExistingRole);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Does.Contain("не існує"));
        }

        [Test]
        public async Task AssignRoleToUserAsync_UserAlreadyInRole_ReturnsError()
        {
            // Arrange
            var userId = "user1";
            var roleName = "Administrator";
            var user = TestDataFactory.CreateUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _mockRoleManager.Setup(m => m.RoleExistsAsync(roleName))
                .ReturnsAsync(true);
            _mockUserManager.Setup(m => m.IsInRoleAsync(user, roleName))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.AssignRoleToUserAsync(userId, roleName);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Does.Contain("вже має цю роль"));
        }

        [Test]
        public async Task RemoveRoleFromUserAsync_ValidData_RemovesRole()
        {
            // Arrange
            var userId = "user1";
            var roleName = "Administrator";
            var user = TestDataFactory.CreateUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _mockUserManager.Setup(m => m.IsInRoleAsync(user, roleName))
                .ReturnsAsync(true);
            _mockUserManager.Setup(m => m.RemoveFromRoleAsync(user, roleName))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.RemoveRoleFromUserAsync(userId, roleName);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            _mockUserManager.Verify(m => m.RemoveFromRoleAsync(user, roleName), Times.Once);
        }

        [Test]
        public async Task RemoveRoleFromUserAsync_NonExistingUser_ReturnsError()
        {
            // Arrange
            var nonExistingId = "nonexisting";
            var roleName = "Administrator";

            _mockUserManager.Setup(m => m.FindByIdAsync(nonExistingId))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _userService.RemoveRoleFromUserAsync(nonExistingId, roleName);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Does.Contain("не знайдено"));
        }

        [Test]
        public async Task RemoveRoleFromUserAsync_UserNotInRole_ReturnsError()
        {
            // Arrange
            var userId = "user1";
            var roleName = "Administrator";
            var user = TestDataFactory.CreateUser(userId);

            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _mockUserManager.Setup(m => m.IsInRoleAsync(user, roleName))
                .ReturnsAsync(false);

            // Act
            var result = await _userService.RemoveRoleFromUserAsync(userId, roleName);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Does.Contain("не має цієї ролі"));
        }

        [Test]
        public async Task GetUserRolesAsync_ExistingUser_ReturnsRoles()
        {
            // Arrange
            var userId = "user1";
            var user = TestDataFactory.CreateUser(userId);
            var roles = new List<string> { "RegisteredUser", "Administrator" };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(roles);

            // Act
            var result = await _userService.GetUserRolesAsync(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(roles.Count));
            Assert.That(result, Does.Contain("RegisteredUser"));
            Assert.That(result, Does.Contain("Administrator"));
        }

        [Test]
        public async Task GetUserRolesAsync_NonExistingUser_ReturnsEmptyList()
        {
            // Arrange
            var nonExistingId = "nonexisting";

            _mockUserManager.Setup(m => m.FindByIdAsync(nonExistingId))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _userService.GetUserRolesAsync(nonExistingId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }
       

        [Test]
        public async Task CreateRoleAsync_ValidRoleName_CreatesRole()
        {
            // Arrange
            var roleName = "NewRole";
            var description = "Description for new role";

            _mockRoleManager.Setup(m => m.RoleExistsAsync(roleName))
                .ReturnsAsync(false);
            _mockRoleManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationRole>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.CreateRoleAsync(roleName, description);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            _mockRoleManager.Verify(m => m.CreateAsync(It.Is<ApplicationRole>(
                r => r.Name == roleName && r.Description == description)), Times.Once);
        }

        [Test]
        public async Task CreateRoleAsync_ExistingRoleName_ReturnsError()
        {
            // Arrange
            var existingRoleName = "Administrator";

            _mockRoleManager.Setup(m => m.RoleExistsAsync(existingRoleName))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.CreateRoleAsync(existingRoleName);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Does.Contain("вже існує"));
        }

        [Test]
        public async Task CreateRoleAsync_Exception_ReturnsError()
        {
            // Arrange
            var roleName = "ErrorRole";

            _mockRoleManager.Setup(m => m.RoleExistsAsync(roleName))
                .ReturnsAsync(false);
            _mockRoleManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationRole>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _userService.CreateRoleAsync(roleName);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Does.Contain("Помилка створення ролі"));
        }
    }
}