using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Project_Manager.Controllers;
using Project_Manager.Models;
using Project_Manager.ViewModels;
using Xunit;

public class AccountControllerTests
{
    private Mock<UserManager<ApplicationUser>> _userManagerMock;
    private Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private Mock<IEmailSender> _emailSenderMock;
    private Mock<ILogger<AccountController>> _loggerMock;
    private AccountController _controller;

    public AccountControllerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);

        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
            null, null, null, null);

        _emailSenderMock = new Mock<IEmailSender>();
        _loggerMock = new Mock<ILogger<AccountController>>();

        _controller = new AccountController(_userManagerMock.Object, _signInManagerMock.Object, _loggerMock.Object, _emailSenderMock.Object);

        var httpContext = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();
        var response = new Mock<HttpResponse>();
        var session = new Mock<ISession>();
        var connection = new Mock<ConnectionInfo>();

        httpContext.Setup(x => x.Request).Returns(request.Object);
        httpContext.Setup(x => x.Response).Returns(response.Object);
        httpContext.Setup(x => x.Session).Returns(session.Object);
        httpContext.Setup(x => x.Connection).Returns(connection.Object);

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("http://localhost/confirm");

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };
        _controller.Url = urlHelper.Object;
    }

    [Fact]
    public async System.Threading.Tasks.Task Register_ValidModel_RedirectsToHomeIndex()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            UserName = "testuser",
            Email = "testuser@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("confirmation_token");

        _emailSenderMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        _signInManagerMock.Setup(x => x.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), null))
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        // Act
        var result = await _controller.Register(model);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectToActionResult.ActionName);
        Assert.Equal("Home", redirectToActionResult.ControllerName);
    }

    [Fact]
    public async System.Threading.Tasks.Task Login_ValidModel_RedirectsToHomeIndex()
    {
        // Arrange
        var model = new LoginViewModel
        {
            UserName = "testuser",
            Password = "Password123!",
            RememberMe = true
        };

        _signInManagerMock.Setup(x => x.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _controller.Login(model);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectToActionResult.ActionName);
        Assert.Equal("Home", redirectToActionResult.ControllerName);
    }

    [Fact]
    public async System.Threading.Tasks.Task Logout_RedirectsToHomeIndex()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.SignOutAsync()).Returns(System.Threading.Tasks.Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectToActionResult.ActionName);
        Assert.Equal("Home", redirectToActionResult.ControllerName);
        
    }
    [Fact]
    public async System.Threading.Tasks.Task Register_InvalidModel_ReturnsViewWithModelError()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            UserName = "testuser",
            Email = "invalid-email", // Invalid email format
            Password = "123", // Too short
            ConfirmPassword = "456" // Does not match password
        };

        _controller.ModelState.AddModelError("Email", "Invalid email format");
        _controller.ModelState.AddModelError("Password", "The Password must be at least 6 characters long.");
        _controller.ModelState.AddModelError("ConfirmPassword", "The password and confirmation password do not match.");

        // Act
        var result = await _controller.Register(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var modelState = viewResult.ViewData.ModelState;

        Assert.False(modelState.IsValid);
        Assert.Equal(3, modelState.ErrorCount);
        Assert.Contains("Email", modelState.Keys);
        Assert.Contains("Password", modelState.Keys);
        Assert.Contains("ConfirmPassword", modelState.Keys);

        var emailError = modelState["Email"].Errors.First();
        var passwordError = modelState["Password"].Errors.First();
        var confirmPasswordError = modelState["ConfirmPassword"].Errors.First();

        Assert.Equal("Invalid email format", emailError.ErrorMessage);
        Assert.Equal("The Password must be at least 6 characters long.", passwordError.ErrorMessage);
        Assert.Equal("The password and confirmation password do not match.", confirmPasswordError.ErrorMessage);
    }
}