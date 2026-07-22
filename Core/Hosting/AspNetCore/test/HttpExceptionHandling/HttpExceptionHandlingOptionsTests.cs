using Microsoft.AspNetCore.Http;
using RaccoonLand.Core.Hosting.AspNetCore.HttpExceptionHandling;

namespace RaccoonLand.Core.Hosting.AspNetCore.Tests.HttpExceptionHandling;

public sealed class HttpExceptionHandlingOptionsTests
{
    [Fact]
    public void On_Throws_WhenHandlerIsNull()
    {
        var options = new HttpExceptionHandlingOptions();

        Assert.Throws<ArgumentNullException>(() =>
            options.On<Exception>(null!));
    }
}
