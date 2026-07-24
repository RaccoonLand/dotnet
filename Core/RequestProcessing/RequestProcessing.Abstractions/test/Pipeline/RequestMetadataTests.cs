using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Support;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Pipeline;

public sealed class RequestMetadataTests
{
    [Fact]
    public void For_TypedRequest_ResolvesResponseTypeFromIRequestOfT()
    {
        var metadata = RequestMetadata.For(typeof(SampleQuery), RequestKind.Query);

        Assert.Equal(typeof(SampleQuery), metadata.RequestType);
        Assert.Equal(typeof(string), metadata.ResponseType);
        Assert.Equal(RequestKind.Query, metadata.Kind);
        Assert.True(metadata.HasTypedResponse);
    }

    [Fact]
    public void For_VoidRequest_HasNullResponseType()
    {
        var metadata = RequestMetadata.For(typeof(SampleRequest), RequestKind.Command);

        Assert.Equal(typeof(SampleRequest), metadata.RequestType);
        Assert.Null(metadata.ResponseType);
        Assert.Equal(RequestKind.Command, metadata.Kind);
        Assert.False(metadata.HasTypedResponse);
    }

    [Fact]
    public void For_SameKeyPair_ReturnsCachedInstance()
    {
        var first = RequestMetadata.For(typeof(SampleQuery), RequestKind.Query);
        var second = RequestMetadata.For(typeof(SampleQuery), RequestKind.Query);

        Assert.Same(first, second);
    }

    [Fact]
    public void For_DifferentKind_ReturnsDistinctInstances()
    {
        // Kind is part of the cache key: the same request type used under two kinds must not alias.
        var asCommand = RequestMetadata.For(typeof(SampleRequest), RequestKind.Command);
        var asQuery = RequestMetadata.For(typeof(SampleRequest), RequestKind.Query);

        Assert.NotSame(asCommand, asQuery);
        Assert.Equal(RequestKind.Command, asCommand.Kind);
        Assert.Equal(RequestKind.Query, asQuery.Kind);
    }

    [Fact]
    public void For_NullRequestType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RequestMetadata.For(null!, RequestKind.Command));
    }

    [Fact]
    public void Constructor_PreservesConstructorArguments()
    {
        var metadata = new RequestMetadata(typeof(SampleQuery), typeof(string), RequestKind.Query);

        Assert.Equal(typeof(SampleQuery), metadata.RequestType);
        Assert.Equal(typeof(string), metadata.ResponseType);
        Assert.Equal(RequestKind.Query, metadata.Kind);
    }

    [Fact]
    public void Constructor_NullResponseType_IsAllowedForVoidRequests()
    {
        var metadata = new RequestMetadata(typeof(SampleRequest), responseType: null, RequestKind.Command);

        Assert.Null(metadata.ResponseType);
        Assert.False(metadata.HasTypedResponse);
    }

    [Fact]
    public void Constructor_NullRequestType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RequestMetadata(null!, typeof(string), RequestKind.Query));
    }

    [Fact]
    public void For_ConcurrentAccess_ReturnsStableInstance()
    {
        // The memoized cache must be safe under contention: many threads racing on the same key must all
        // observe the same instance, so downstream identity checks in hot paths stay valid.
        var results = new RequestMetadata[64];
        Parallel.For(0, results.Length, i =>
        {
            results[i] = RequestMetadata.For(typeof(SampleQuery), RequestKind.Query);
        });

        Assert.All(results, m => Assert.Same(results[0], m));
    }
}
