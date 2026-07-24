using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaccoonLand.Core.Hosting.AspNetCore.Controllers;
using RaccoonLand.Core.Hosting.AspNetCore.PipelineResponseMapping;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Dispatch;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Core.Hosting.AspNetCore.Tests.Controllers;

public sealed class RaccoonLandControllerTests
{
    [Fact]
    public async Task DispatchAsync_ResolvesDispatcherFromRequestServices_NotRoot()
    {
        var dispatcher = new CapturingDispatcher
        {
            ResponseToReturn = new PipelineResponse { Result = "ok" },
        };
        var mapper = new CapturingMapper();

        var root = new ServiceCollection().BuildServiceProvider();
        var requestServices = new ServiceCollection();
        requestServices.AddSingleton<IRequestDispatcher>(dispatcher);
        requestServices.AddSingleton<IPipelineResponseMapper>(mapper);
        var requestProvider = requestServices.BuildServiceProvider();

        var controller = CreateController(requestProvider);

        await controller.DispatchPublicAsync(new SampleRequest(), CancellationToken.None);

        Assert.Same(requestProvider, dispatcher.LastRequestServices);
        Assert.NotSame(root, dispatcher.LastRequestServices);
        Assert.True(dispatcher.WasCalled);
    }

    [Fact]
    public async Task DispatchAsync_PassesSameRequestServicesToDispatcher()
    {
        var dispatcher = new CapturingDispatcher
        {
            ResponseToReturn = new PipelineResponse(),
        };
        var mapper = new CapturingMapper();
        var requestProvider = CreateRequestServices(dispatcher, mapper);
        var controller = CreateController(requestProvider);

        await controller.DispatchPublicAsync(new SampleRequest(), CancellationToken.None);

        Assert.Same(requestProvider, dispatcher.LastRequestServices);
        Assert.Same(
            controller.ControllerContext.HttpContext.RequestServices,
            dispatcher.LastRequestServices);
    }

    [Fact]
    public async Task DispatchAsync_PropagatesCancellationTokenUnchanged()
    {
        var dispatcher = new CapturingDispatcher
        {
            ResponseToReturn = new PipelineResponse(),
        };
        var mapper = new CapturingMapper();
        var controller = CreateController(CreateRequestServices(dispatcher, mapper));

        using var cts = new CancellationTokenSource();
        await controller.DispatchPublicAsync(new SampleRequest(), cts.Token);

        Assert.Equal(cts.Token, dispatcher.LastCancellationToken);
    }

    [Fact]
    public async Task DispatchAsync_FallsBackToRequestAborted_WhenCallerPassesDefaultToken()
    {
        // Regression: if the action author forgets to accept + forward the CancellationToken
        // parameter, the base must NOT dispatch with a token that never cancels — otherwise a
        // client disconnect leaves in-flight DB / HTTP / cache work running to completion.
        var dispatcher = new CapturingDispatcher
        {
            ResponseToReturn = new PipelineResponse(),
        };
        var mapper = new CapturingMapper();

        using var requestAborted = new CancellationTokenSource();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = CreateRequestServices(dispatcher, mapper),
            RequestAborted = requestAborted.Token,
        };
        var controller = new TestController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };

        await controller.DispatchPublicAsync(new SampleRequest(), CancellationToken.None);

        Assert.Equal(requestAborted.Token, dispatcher.LastCancellationToken);
        Assert.True(dispatcher.LastCancellationToken.CanBeCanceled);
    }

    [Fact]
    public async Task DispatchAsync_PrefersExplicitToken_OverRequestAborted()
    {
        // When the caller does forward a real token, we must not swap it for RequestAborted —
        // they may be composing cancellation with a timeout, a linked source, etc.
        var dispatcher = new CapturingDispatcher
        {
            ResponseToReturn = new PipelineResponse(),
        };
        var mapper = new CapturingMapper();

        using var requestAborted = new CancellationTokenSource();
        using var caller = new CancellationTokenSource();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = CreateRequestServices(dispatcher, mapper),
            RequestAborted = requestAborted.Token,
        };
        var controller = new TestController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };

        await controller.DispatchPublicAsync(new SampleRequest(), caller.Token);

        Assert.Equal(caller.Token, dispatcher.LastCancellationToken);
        Assert.NotEqual(requestAborted.Token, dispatcher.LastCancellationToken);
    }

    [Fact]
    public async Task DispatchAsync_PassesDispatcherResponseToMapper_WithoutMutation()
    {
        var envelope = new PipelineResponse
        {
            Result = 7,
            Errors = [new PipelineMessage("E", "x")],
            Warnings = [new PipelineMessage("W", "y")],
            StatusHint = StatusCodes.Status409Conflict,
        };
        var dispatcher = new CapturingDispatcher { ResponseToReturn = envelope };
        var mapper = new CapturingMapper();
        var controller = CreateController(CreateRequestServices(dispatcher, mapper));

        await controller.DispatchPublicAsync(new SampleQuery(), CancellationToken.None);

        Assert.Same(envelope, mapper.LastMappedResponse);
        Assert.Equal(7, mapper.LastMappedResponse!.Result);
        Assert.Equal("E", Assert.Single(mapper.LastMappedResponse.Errors).Code);
        Assert.Equal("W", Assert.Single(mapper.LastMappedResponse.Warnings).Code);
        Assert.Equal(StatusCodes.Status409Conflict, mapper.LastMappedResponse.StatusHint);
    }

    [Fact]
    public async Task DispatchAsync_ReturnsMapperResultUnchanged()
    {
        var dispatcher = new CapturingDispatcher
        {
            ResponseToReturn = new PipelineResponse { Result = "ok" },
        };
        var mapper = new CapturingMapper();
        var controller = CreateController(CreateRequestServices(dispatcher, mapper));

        var actionResult = await controller.DispatchPublicAsync(new SampleRequest(), CancellationToken.None);

        Assert.Same(mapper.ResultToReturn, actionResult);
    }

    [Fact]
    public void Dispatcher_ResolvesFromHttpContextRequestServices_OnEachAccess()
    {
        // The property MUST NOT cache into a field: swapping HttpContext (test scenarios, custom
        // scopes) between accesses must swap the resolved instance too.
        var firstServices = CreateRequestServices(new CapturingDispatcher(), new CapturingMapper());
        var secondServices = CreateRequestServices(new CapturingDispatcher(), new CapturingMapper());
        var controller = CreateController(firstServices);

        var first = controller.DispatcherPublic;

        controller.ControllerContext.HttpContext = new DefaultHttpContext { RequestServices = secondServices };
        var second = controller.DispatcherPublic;

        Assert.Same(firstServices.GetRequiredService<IRequestDispatcher>(), first);
        Assert.Same(secondServices.GetRequiredService<IRequestDispatcher>(), second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void ResponseMapper_ResolvesFromHttpContextRequestServices_OnEachAccess()
    {
        var firstServices = CreateRequestServices(new CapturingDispatcher(), new CapturingMapper());
        var secondServices = CreateRequestServices(new CapturingDispatcher(), new CapturingMapper());
        var controller = CreateController(firstServices);

        var first = controller.ResponseMapperPublic;

        controller.ControllerContext.HttpContext = new DefaultHttpContext { RequestServices = secondServices };
        var second = controller.ResponseMapperPublic;

        Assert.Same(firstServices.GetRequiredService<IPipelineResponseMapper>(), first);
        Assert.Same(secondServices.GetRequiredService<IPipelineResponseMapper>(), second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task Dispatcher_UsedByCustomFlow_SharesSameInstanceAsDispatchAsync()
    {
        // The advanced-scenario property MUST resolve the same dispatcher that DispatchAsync uses,
        // otherwise a controller mixing both paths would produce inconsistent behavior.
        var dispatcher = new CapturingDispatcher { ResponseToReturn = new PipelineResponse() };
        var mapper = new CapturingMapper();
        var controller = CreateController(CreateRequestServices(dispatcher, mapper));

        var viaProperty = controller.DispatcherPublic;
        await controller.DispatchPublicAsync(new SampleRequest(), CancellationToken.None);

        Assert.Same(dispatcher, viaProperty);
        Assert.True(dispatcher.WasCalled);
    }

    private static TestController CreateController(IServiceProvider requestServices)
    {
        return new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = requestServices },
            },
        };
    }

    private static ServiceProvider CreateRequestServices(
        IRequestDispatcher dispatcher,
        IPipelineResponseMapper mapper)
    {
        var services = new ServiceCollection();
        services.AddSingleton(dispatcher);
        services.AddSingleton(mapper);
        return services.BuildServiceProvider();
    }

    private sealed class TestController : RaccoonLandController
    {
        public Task<IActionResult> DispatchPublicAsync(IRequest request, CancellationToken cancellationToken)
            => DispatchAsync(request, cancellationToken);

        public Task<IActionResult> DispatchPublicAsync(IRequest<int> request, CancellationToken cancellationToken)
            => DispatchAsync(request, cancellationToken);

        public IRequestDispatcher DispatcherPublic => Dispatcher;

        public IPipelineResponseMapper ResponseMapperPublic => ResponseMapper;
    }

    private sealed class SampleRequest : IRequest;

    private sealed class SampleQuery : IRequest<int>;

    private sealed class CapturingDispatcher : IRequestDispatcher
    {
        public PipelineResponse? ResponseToReturn { get; init; }

        public bool WasCalled { get; private set; }

        public IServiceProvider? LastRequestServices { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<PipelineResponse?> DispatchAsync(
            IRequestBase request,
            IServiceProvider requestServices,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastRequestServices = requestServices;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(ResponseToReturn);
        }
    }

    private sealed class CapturingMapper : IPipelineResponseMapper
    {
        public PipelineResponse? LastMappedResponse { get; private set; }

        public IActionResult ResultToReturn { get; } = new OkResult();

        public IActionResult Map(PipelineResponse? response)
        {
            LastMappedResponse = response;
            return ResultToReturn;
        }
    }
}
