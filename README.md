[![](https://img.shields.io/nuget/v/Soenneker.Utils.SingletonDictionary.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.SingletonDictionary/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.singletondictionary/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.singletondictionary/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Utils.SingletonDictionary.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.SingletonDictionary/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.SingletonDictionary

### A flexible singleton dictionary with double-check async locking, sync/async disposal, and external initialization

---

## Features

* ✅ Async and sync initialization patterns
* ✅ Optional cancellation support
* ✅ Async and sync access methods
* ✅ Fully disposable (sync and async)
* ✅ Thread-safe with `AsyncLock` from [Nito.AsyncEx](https://github.com/StephenCleary/AsyncEx)

---

## Installation

```bash
dotnet add package Soenneker.Utils.SingletonDictionary
```

---

## ✨ Example Usage

Here’s an example using `SingletonDictionary` to manage singleton `HttpClient` instances keyed by configuration (e.g. timeout):

```csharp
public class HttpRequester : IDisposable, IAsyncDisposable
{
    private readonly SingletonDictionary<HttpClient> _clients;

    public HttpRequester()
    {
        _clients = new SingletonDictionary<HttpClient>((args) =>
        {
            var socketsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                MaxConnectionsPerServer = 10
            };

            var client = new HttpClient(socketsHandler)
            {
                Timeout = TimeSpan.FromSeconds((int)args[0])
            };

            return client;
        });
    }

    public async ValueTask Get()
    {
        var client = await _clients.Get("100", 100);
        await client.GetAsync("https://google.com");
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await _clients.DisposeAsync();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _clients.Dispose();
    }
}
```

---

## 🔍 Internals

`SingletonDictionary<T>` is backed by a `ConcurrentDictionary<string, T>` and supports:

* Multiple constructor overloads for async/sync factory functions
* Internal double-checked locking on access
* Deferred/lazy factory execution
* Proper disposal of values (both sync and async interfaces)
* Safe mutation via `SetInitialization` before first use

Example constructor overloads include:

```csharp
new SingletonDictionary<T>(Func<string, object[], ValueTask<T>> factory);
new SingletonDictionary<T>(Func<object[], T> factory);
new SingletonDictionary<T>(Func<string, CancellationToken, object[], ValueTask<T>> factory);
// And more...
```

You can also initialize manually:

```csharp
var dict = new SingletonDictionary<MyService>();
dict.SetInitialization((args) => new MyService(args));
```

---

## 🛡️ Thread Safety

This library uses [`AsyncLock`](https://github.com/StephenCleary/AsyncEx) for safe concurrent access in async contexts, and synchronously via `Lock()` for blocking methods. This avoids race conditions and guarantees safe singleton creation.