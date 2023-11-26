using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.SingletonDictionary.Abstract;

namespace Soenneker.Utils.SingletonDictionary;

///<inheritdoc cref="ISingletonDictionary{T}"/>
public class SingletonDictionary<T> : ISingletonDictionary<T>
{
    private ConcurrentDictionary<string, T>? _dictionary;

    private readonly AsyncLock _lock;

    private Func<object[]?, ValueTask<T>>? _asyncInitializationFunc;
    private Func<object[]?, T>? _initializationFunc;

    private bool _disposed;

    public SingletonDictionary(Func<object[]?, ValueTask<T>> asyncInitializationFunc) : this()
    {
        _asyncInitializationFunc = asyncInitializationFunc;
    }

    public SingletonDictionary(Func<object[]?, T> initializationFunc) : this()
    {
        _initializationFunc = initializationFunc;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    /// If this is used, be sure to set the initialization func, see <see cref="SetAsyncInitialization"/> or <see cref="SetInitialization"/> or use another constructor.
    /// </summary>
    public SingletonDictionary()
    {
        _lock = new AsyncLock();
        _dictionary = new ConcurrentDictionary<string, T>();
    }

    public async ValueTask<T> Get(string key, params object[]? objects)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        if (_dictionary!.TryGetValue(key, out T? instance))
            return instance;

        using (await _lock.LockAsync().ConfigureAwait(false))
        {
            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            if (_asyncInitializationFunc != null)
            {
                instance = await _asyncInitializationFunc(objects).NoSync();
            }
            else if (_initializationFunc != null)
            {
                instance = _initializationFunc(objects);
            }
            else
                throw new NullReferenceException($"Initialization func for {nameof(SingletonDictionary<T>)} cannot be null");

            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    public T GetSync(string key, params object[]? objects)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        if (_dictionary!.TryGetValue(key, out T? instance))
            return instance;

        using (_lock.Lock())
        {
            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            if (_initializationFunc != null)
            {
                instance = _initializationFunc(objects);
            }
            else if (_asyncInitializationFunc != null)
            {
                // Not a great situation here - we only have async initialization but we're calling this synchronously... so we'll block

                return _asyncInitializationFunc(objects).AsTask().GetAwaiter().GetResult();
            }
            else
                throw new NullReferenceException("Initialization func for AsyncSingleton cannot be null");

            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    public void SetAsyncInitialization(Func<object[]?, ValueTask<T>> asyncInitializationFunc)
    {
        _asyncInitializationFunc = asyncInitializationFunc;
    }

    public void SetInitialization(Func<object[]?, T> initializationFunc)
    {
        _initializationFunc = initializationFunc;
    }

    public async ValueTask Remove(string key)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        if (_dictionary!.TryGetValue(key, out T? instance))
        {
            await DisposeInstanceAsync(key, instance).NoSync();
            return;
        }

        using (await _lock.LockAsync().ConfigureAwait(false))
        {
            if (_dictionary.TryGetValue(key, out instance))
            {
                await DisposeInstanceAsync(key, instance).NoSync();
            }
        }
    }

    public void RemoveSync(string key)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        if (_dictionary!.TryGetValue(key, out T? instance))
        {
            DisposeInstance(key, instance);
            return;
        }

        using (_lock.Lock())
        {
            if (_dictionary.TryGetValue(key, out instance))
            {
                DisposeInstance(key, instance);
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;

        GC.SuppressFinalize(this);

        // Don't use .IsNullOrEmpty() due to unique ConcurrentDictionary properties
        if (_dictionary == null || _dictionary.IsEmpty)
            return;

        foreach (KeyValuePair<string, T> kvp in _dictionary)
        {
            DisposeInstance(kvp.Key, kvp.Value);
        }

        _dictionary = null;
    }

    private void DisposeInstance(string key, T instance)
    {
        switch (instance)
        {
            case IDisposable disposable:
                disposable.Dispose();
                break;
            case IAsyncDisposable asyncDisposable:
                // Kind of a weird situation - basically the instance is IAsyncDisposable but it's being disposed synchronously (which can happen).
                // Hopefully this object is IDisposable because this is not guaranteed to block, but we'll try anyways
                _ = asyncDisposable.DisposeAsync();
                break;
        }

        if (_dictionary == null || _dictionary.IsEmpty)
            return;

        _dictionary.TryRemove(key, out _);
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        GC.SuppressFinalize(this);

        // Don't use .IsNullOrEmpty() due to unique ConcurrentDictionary properties
        if (_dictionary == null || _dictionary.IsEmpty)
            return;

        foreach (KeyValuePair<string, T> kvp in _dictionary)
        {
            await DisposeInstanceAsync(kvp.Key, kvp.Value).NoSync();
        }

        _dictionary = null;
    }

    private async ValueTask DisposeInstanceAsync(string key, T instance)
    {
        switch (instance)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync().NoSync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }

        if (_dictionary == null || _dictionary.IsEmpty)
            return;

        _dictionary.TryRemove(key, out _);
    }
}