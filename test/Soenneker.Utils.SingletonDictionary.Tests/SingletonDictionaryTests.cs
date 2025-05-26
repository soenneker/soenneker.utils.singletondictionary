using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Soenneker.Extensions.Enumerable;
using Xunit;

namespace Soenneker.Utils.SingletonDictionary.Tests;

public class SingletonDictionaryTests
{
    [Fact]
    public async Task Get_with_inline_func()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>((key, objects) =>
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds((int) objects[0])
            };

            return client;
        });

        HttpClient result = await httpClientSingleton.Get("100", 100);

        result.Timeout.TotalSeconds.Should().Be(100);
    }

    [Fact]
    public async Task Get_async_with_async_init_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(AsyncInitializationFunc);

        HttpClient result = await httpClientSingleton.Get("arst", 100);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_async_with_key_with_async_init_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(AsyncKeyInitializationFunc);

        HttpClient result = await httpClientSingleton.Get("arst", 100);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_async_with_async_init_with_null_params_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(AsyncInitializationFunc);

        HttpClient result = await httpClientSingleton.Get("arst", null);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_async_with_async_init_with_empty_params_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(AsyncInitializationFunc);

        HttpClient result = await httpClientSingleton.Get("arst");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_async_with_sync_init_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(InitializeFunc);

        HttpClient result = await httpClientSingleton.Get("arst", 100);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_async_and_sync_init_with_empty_object_param_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(InitializeFunc);

        HttpClient result = await httpClientSingleton.Get("arst");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_async_and_sync_init_with_null_object_param_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(InitializeFunc);

        HttpClient result = await httpClientSingleton.Get("arst", null);

        result.Should().NotBeNull();
    }

    private static HttpClient InitializeFunc(object[] arg)
    {
        var httpClient = new HttpClient();

        if (arg.Populated())
        {
            httpClient.Timeout = TimeSpan.FromSeconds((int) arg[0]);
        }

        return httpClient;
    }

    [Fact]
    public void Get_sync_with_sync_init_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(InitializeFunc);

        HttpClient result = httpClientSingleton.GetSync("arst", 100);

        result.Should().NotBeNull();
    }

    [Fact]
    public void Get_sync_and_sync_init_with_empty_object_param_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(InitializeFunc);

        HttpClient result = httpClientSingleton.GetSync("arst");

        result.Should().NotBeNull();
    }

    [Fact]
    public void Get_sync_and_sync_init_with_null_object_param_should_return_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(InitializeFunc);

        HttpClient result = httpClientSingleton.GetSync("arst", null);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Multiple_get_should_return_same_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(AsyncInitializationFunc);

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
        var httpClientSingleton = new SingletonDictionary<HttpClient>(AsyncInitializationFunc);

        // Don't do this in real usage, make keys unique to their parameters
        Task<HttpClient> task1 = httpClientSingleton.Get("test", 100).AsTask();
        Task<HttpClient> task2 = httpClientSingleton.Get("test", 200).AsTask();

        var results = await Task.WhenAll(task1, task2);

        results[0].Timeout.TotalSeconds.Should().Be(100);
        results[1].Timeout.TotalSeconds.Should().Be(100);
    }

    [Fact]
    public async Task Get_DisposeAsync_should_throw_after_disposing()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(_ => new HttpClient());

        _ = await httpClientSingleton.Get("test");

        await httpClientSingleton.DisposeAsync();

        Func<Task> act = async () => _ = await httpClientSingleton.Get("test");

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task Get_with_token_should_be_token_given()
    {
        var cancellationToken = new CancellationToken();

        var httpClientSingleton = new SingletonDictionary<HttpClient>(async (key, token, obj) =>
        {
            token.Should().Be(cancellationToken);
            return new HttpClient();
        });

        _ = await httpClientSingleton.Get("test", cancellationToken);
    }

    [Fact]
    public async Task GetSync_Dispose_should_throw_after_disposing()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(_ => new HttpClient());

        _ = await httpClientSingleton.Get("test");

        // ReSharper disable once MethodHasAsyncOverload
        httpClientSingleton.Dispose();

        Action act = () => _ = httpClientSingleton.GetSync("test");

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task Dispose_with_nondisposable_should_not_throw()
    {
        var httpClientSingleton = new SingletonDictionary<object>(_ => new object());

        _ = await httpClientSingleton.Get("test");

        // ReSharper disable once MethodHasAsyncOverload
        httpClientSingleton.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_with_nondisposable_should_not_throw()
    {
        var httpClientSingleton = new SingletonDictionary<object>(_ => new object());

        _ = await httpClientSingleton.Get("test");

        await httpClientSingleton.DisposeAsync();
    }

    [Fact]
    public void Remove_sync_should_remove_instance()
    {
        var httpClientSingleton = new SingletonDictionary<HttpClient>(InitializeFunc);

        HttpClient result = httpClientSingleton.GetSync("arst", 100);

        httpClientSingleton.RemoveSync("arst");

        result = httpClientSingleton.GetSync("arst", 200);

        result.Timeout.TotalSeconds.Should().Be(200);
    }


    private static async ValueTask<HttpClient> AsyncInitializationFunc(object[] arg)
    {
        var httpClient = new HttpClient();

        if (arg.Populated())
            httpClient.Timeout = TimeSpan.FromSeconds((int) arg[0]);

        await Task.Delay(100);

        return httpClient;
    }

    private static async ValueTask<HttpClient> AsyncKeyInitializationFunc(string key, object[] arg)
    {
        var httpClient = new HttpClient();

        if (arg.Populated())
            httpClient.Timeout = TimeSpan.FromSeconds((int) arg[0]);

        await Task.Delay(100);

        return httpClient;
    }
}