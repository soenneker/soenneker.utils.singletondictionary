[![](https://img.shields.io/nuget/v/Soenneker.Utils.SingletonDictionary.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.SingletonDictionary/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.singletondictionary/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.singletondictionary/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Utils.SingletonDictionary.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.SingletonDictionary/)

# ![]() Soenneker.Utils.SingletonDictionary
### An externally initializing singleton dictionary that uses double-check asynchronous locking, with optional async and sync disposal

## Installation

```
dotnet add package Soenneker.Utils.SingletonDictionary
```

## Example

Below is a long-living `HttpClient` implementation using `SingletonDictionary` with different settings. It guarantees only one instance of a particular key is instantiated due to the locking.

```csharp
public class HttpRequester : IDisposable, IAsyncDisposable
{
    private readonly SingletonDictionary<HttpClient> _clients;

    public HttpRequester()
    {
        // This func will lazily execute once it's retrieved the first time.
        // Other threads calling this at the same moment will asynchronously wait,
        // and then utilize the HttpClient that was created from the first caller.
        _clients = new SingletonDictionary<HttpClient>((args) =>
        {
            var socketsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                MaxConnectionsPerServer = 10
            };

            HttpClient client = new HttpClient(socketsHandler);
            client.Timeout = TimeSpan.FromSeconds((int)args[0]);

            return client;
        });
    }

    public async ValueTask Get()
    {
        // retrieve the singleton async, thus not blocking the calling thread
        await (await _client.Get("100", 100)).GetAsync("https://google.com");
    }

    // Disposal is not necessary for AsyncSingleton unless the type used is IDisposable/IAsyncDisposable
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(false);

        return _client.DisposeAsync();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(false);
        
        _client.Dispose();
    }
}
```