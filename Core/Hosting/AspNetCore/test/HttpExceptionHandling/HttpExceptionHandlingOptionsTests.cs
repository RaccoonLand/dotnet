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

    [Fact]
    public void On_ReturnsSameInstance_ForFluentChaining()
    {
        var options = new HttpExceptionHandlingOptions();

        var result = options.On<InvalidOperationException>((_, _) => Task.FromResult(true));

        Assert.Same(options, result);
    }

    [Fact]
    public void On_PreservesRegistrationOrder_InHandlersList()
    {
        // The middleware iterates Handlers in order and expects specific-before-general;
        // guard the ordering contract at the options layer, independently of the middleware test.
        var options = new HttpExceptionHandlingOptions();
        options.On<InvalidOperationException>((_, _) => Task.FromResult(true));
        options.On<ArgumentException>((_, _) => Task.FromResult(true));
        options.On<Exception>((_, _) => Task.FromResult(true));

        var types = options.Handlers.Select(h => h.ExceptionType).ToArray();

        Assert.Equal(
            new[] { typeof(InvalidOperationException), typeof(ArgumentException), typeof(Exception) },
            types);
    }

    [Fact]
    public void Handlers_ExposesRegistrations_AsReadOnlyList()
    {
        var options = new HttpExceptionHandlingOptions();
        options.On<Exception>((_, _) => Task.FromResult(true));

        // Public contract must be IReadOnlyList — no ICollection<T> / IList<T> surface leak.
        Assert.IsAssignableFrom<IReadOnlyList<HttpExceptionHandlingOptions.ExceptionHandlerRegistration>>(
            options.Handlers);
        Assert.False(options.Handlers is ICollection<HttpExceptionHandlingOptions.ExceptionHandlerRegistration> writable
            && !writable.IsReadOnly);
    }

    [Fact]
    public async Task On_HandlerReceivesStronglyTypedException()
    {
        var options = new HttpExceptionHandlingOptions();
        InvalidOperationException? seen = null;
        options.On<InvalidOperationException>((_, ex) =>
        {
            seen = ex;
            return Task.FromResult(true);
        });

        var registered = Assert.Single(options.Handlers);
        var context = new DefaultHttpContext();
        var thrown = new InvalidOperationException("bang");

        var handled = await registered.Handler(context, thrown);

        Assert.True(handled);
        Assert.Same(thrown, seen);
    }
}
