using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

namespace RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware.Tests.Options;

public sealed class ExceptionHandlingOptionsTests
{
    [Fact]
    public void On_ReturnsSameInstance_ForFluentChaining()
    {
        var options = new ExceptionHandlingOptions();

        var chained = options
            .On<InvalidOperationException>((_, _) => Task.FromResult(true))
            .On<ArgumentException>((_, _) => Task.FromResult(true));

        Assert.Same(options, chained);
    }

    [Fact]
    public void On_PreservesRegistrationOrder_InHandlersList()
    {
        var options = new ExceptionHandlingOptions();

        options.On<InvalidOperationException>((_, _) => Task.FromResult(true));
        options.On<ArgumentException>((_, _) => Task.FromResult(true));
        options.On<Exception>((_, _) => Task.FromResult(true));

        Assert.Collection(
            options.Handlers,
            registration => Assert.Equal(typeof(InvalidOperationException), registration.ExceptionType),
            registration => Assert.Equal(typeof(ArgumentException), registration.ExceptionType),
            registration => Assert.Equal(typeof(Exception), registration.ExceptionType));
    }

    [Fact]
    public void Handlers_ExposesRegistrations_AsReadOnlyList()
    {
        var options = new ExceptionHandlingOptions();
        options.On<InvalidOperationException>((_, _) => Task.FromResult(true));

        var handlers = options.Handlers;

        Assert.IsAssignableFrom<IReadOnlyList<ExceptionHandlerRegistration>>(handlers);

        // Regression: previously the property returned the internal List<T> directly, which allowed callers
        // to cast it to a writable ICollection<T> and bypass On<TException>. It must now expose a read-only
        // view (ReadOnlyCollection<T>.IsReadOnly == true), even when the underlying storage is a List<T>.
        if (handlers is ICollection<ExceptionHandlerRegistration> collection)
        {
            Assert.True(collection.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => collection.Add(
                new ExceptionHandlerRegistration(typeof(Exception), (_, _) => Task.FromResult(true))));
        }
    }

    [Fact]
    public async Task On_HandlerReceivesStronglyTypedException()
    {
        InvalidOperationException? captured = null;
        var options = new ExceptionHandlingOptions();
        options.On<InvalidOperationException>((_, exception) =>
        {
            captured = exception;
            return Task.FromResult(true);
        });

        var registration = Assert.Single(options.Handlers);
        var boom = new InvalidOperationException("boom");
        await registration.Handler(
            new PipelineContext(
                new Tests.Support.SampleRequest(),
                new ServiceCollection().BuildServiceProvider(),
                RequestMetadata.For(typeof(Tests.Support.SampleRequest), RequestKind.Command)),
            boom);

        Assert.NotNull(captured);
        Assert.Same(boom, captured);
    }
}
