using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace yearbook_tests;

public class UserTest
{
    [Fact]
    public async void Get_shall_fetch_all_users()
    {
        var log = new Mock<ILogger>();
        var request = new Mock<HttpRequest>();
        var response = (OkObjectResult)await YearbookApp.User.Run(request.Object, log.Object);
        Assert.Equal("ok", response.Value);
    }
}