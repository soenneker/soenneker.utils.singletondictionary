using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Soenneker.Tests.Unit;
using Xunit;

namespace Soenneker.Utils.SingletonDictionary.Tests;

public class SingletonDictionaryTests : UnitTest
{
    [Fact]
    public async Task Get_with_inline_func()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient, int>((key, timeout) =>
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };

            return client;
        });

        HttpClient result = await httpClientSingleton.Get("100", 100);

        result.Timeout.TotalSeconds.Should().Be(100);
    }

    [Fact]
    public async Task Get_async_with_async_init_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient, int>(AsyncInitializationFunc);

        HttpClient result = await httpClientSingleton.Get("arst", 100);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_async_with_key_with_async_init_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient, int>(AsyncKeyInitializationFunc);

        HttpClient result = await httpClientSingleton.Get("arst", 100);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_async_with_sync_init_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient, int>(InitializeFunc);

        HttpClient result = await httpClientSingleton.Get("arst", 100);

        result.Should().NotBeNull();
    }

    private static HttpClient InitializeFunc(int timeout)
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeout)
        };

        return httpClient;
    }

    [Fact]
    public void Get_sync_with_sync_init_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient, int>(InitializeFunc);

        HttpClient result = httpClientSingleton.GetSync("arst", 100);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Multiple_get_should_return_same_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient, int>(AsyncInitializationFunc);

        HttpClient client1 = await httpClientSingleton.Get("test", 200);
        HttpClient client2 = await httpClientSingleton.Get("test", 100);

        client1.Should().NotBeNull();
        client2.Should().NotBeNull();

        client1.Timeout.TotalSeconds.Should().Be(200);
        client2.Timeout.TotalSeconds.Should().Be(200);
    }

    [Fact]
    public async Task Parallel_get_should_return_same_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient, int>(AsyncInitializationFunc);

        // Don't do this in real usage, make keys unique to their parameters
        Task<HttpClient> task1 = httpClientSingleton.Get("test", 100).AsTask();
        Task<HttpClient> task2 = httpClientSingleton.Get("test", 200).AsTask();

        HttpClient[] results = await Task.WhenAll(task1, task2);

        results[0].Timeout.TotalSeconds.Should().Be(100);
        results[1].Timeout.TotalSeconds.Should().Be(100);
    }

    [Fact]
    public async Task Get_DisposeAsync_should_throw_after_disposing()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient, int>(_ => new HttpClient());

        _ = await httpClientSingleton.Get("test", 100);

        await httpClientSingleton.DisposeAsync();

        Func<Task> act = async () => _ = await httpClientSingleton.Get("test", 100);

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task Get_with_token_should_be_token_given()
    {
        var cancellationToken = new CancellationToken();

        var httpClientSingleton = new SingletonDictionary<HttpClient, int>(async (key, token, timeout) =>
        {
            token.Should().Be(cancellationToken);
            return new HttpClient();
        });

        _ = await httpClientSingleton.Get("test", 100, cancellationToken);
    }

    [Fact]
    public async Task GetSync_Dispose_should_throw_after_disposing()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient, int>(_ => new HttpClient());

        _ = await httpClientSingleton.Get("test", 100);

        // ReSharper disable once MethodHasAsyncOverload
        httpClientSingleton.Dispose();

        Action act = () => _ = httpClientSingleton.GetSync("test", 100);

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task Dispose_with_nondisposable_should_not_throw()
    {
        var objectSingleton = new SingletonDictionary<object, int>(_ => new object());

        _ = await objectSingleton.Get("test", 100);

        // ReSharper disable once MethodHasAsyncOverload
        objectSingleton.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_with_nondisposable_should_not_throw()
    {
        var objectSingleton = new SingletonDictionary<object, int>(_ => new object());

        _ = await objectSingleton.Get("test", 100);

        await objectSingleton.DisposeAsync();
    }

    [Fact]
    public void Remove_sync_should_remove_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient, int>(InitializeFunc);

        HttpClient result = httpClientSingleton.GetSync("arst", 100);

        httpClientSingleton.RemoveSync("arst");

        result = httpClientSingleton.GetSync("arst", 200);

        result.Timeout.TotalSeconds.Should().Be(200);
    }


    private static async ValueTask<HttpClient> AsyncInitializationFunc(int timeout)
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeout)
        };

        await Task.Delay(100);

        return httpClient;
    }

    private static async ValueTask<HttpClient> AsyncKeyInitializationFunc(string key, int timeout)
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeout)
        };

        await Task.Delay(100);

        return httpClient;
    }
}