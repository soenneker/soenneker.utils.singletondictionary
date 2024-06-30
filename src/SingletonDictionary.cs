using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.SingletonDictionary.Abstract;
using Soenneker.Utils.SingletonDictionary.Enums;

namespace Soenneker.Utils.SingletonDictionary;

///<inheritdoc cref="ISingletonDictionary{T}"/>
public class SingletonDictionary<T> : ISingletonDictionary<T>
{
    private ConcurrentDictionary<string, T>? _dictionary;

    private readonly AsyncLock _lock;

    private Func<string, object[], ValueTask<T>>? _asyncKeyInitializationFunc;
    private Func<string, object[], T>? _keyInitializationFunc;
    private Func<object[], ValueTask<T>>? _asyncInitializationFunc;
    private Func<object[], T>? _initializationFunc;

    private bool _disposed;

    private InitializationType? _initializationType;

    /// <summary>
    /// If an async initialization func is used, it's recommend that GetSync() NOT be used.
    /// </summary>
    public SingletonDictionary(Func<string, object[], ValueTask<T>> asyncInitializationFunc) : this()
    {
        _initializationType = InitializationType.AsyncKey;
        _asyncKeyInitializationFunc = asyncInitializationFunc;
    }

    /// <summary>
    /// If an async initialization func is used, it's recommend that GetSync() NOT be used.
    /// </summary>
    public SingletonDictionary(Func<object[], ValueTask<T>> asyncInitializationFunc) : this()
    {
        _initializationType = InitializationType.Async;
        _asyncInitializationFunc = asyncInitializationFunc;
    }

    public SingletonDictionary(Func<string, object[], T> initializationFunc) : this()
    {
        _initializationType = InitializationType.SyncKey;
        _keyInitializationFunc = initializationFunc;
    }

    public SingletonDictionary(Func<object[], T> initializationFunc) : this()
    {
        _initializationType = InitializationType.Sync;
        _initializationFunc = initializationFunc;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    /// If this is used, be sure to set the initialization func, see SetInitialization or use another constructor.
    /// </summary>
    public SingletonDictionary()
    {
        _lock = new AsyncLock();
        _dictionary = new ConcurrentDictionary<string, T>();
    }

    public async ValueTask<T> Get(string key, params object[] objects)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        if (_dictionary!.TryGetValue(key, out T? instance))
            return instance;

        using (await _lock.LockAsync().ConfigureAwait(false))
        {
            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            instance = await GetInternal(key, objects).NoSync();
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    public T GetSync(string key, params object[] objects)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        if (_dictionary!.TryGetValue(key, out T? instance))
            return instance;

        using (_lock.Lock())
        {
            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            instance = GetInternalSync(key, objects);
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    private async ValueTask<T> GetInternal(string key, object[] objects)
    {
        switch (_initializationType)
        {
            case InitializationType.AsyncKey:
                if (_asyncKeyInitializationFunc == null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return await _asyncKeyInitializationFunc(key, objects).NoSync();
            case InitializationType.Async:
                if (_asyncInitializationFunc == null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return await _asyncInitializationFunc(objects).NoSync();
            case InitializationType.Sync:
                if (_initializationFunc == null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _initializationFunc(objects);
            case InitializationType.SyncKey:
                if (_keyInitializationFunc == null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _keyInitializationFunc(key, objects);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private T GetInternalSync(string key, object[] objects)
    {
        switch (_initializationType)
        {
            case InitializationType.AsyncKey:
                if (_asyncKeyInitializationFunc == null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncKeyInitializationFunc(key, objects).NoSync().GetAwaiter().GetResult();
            case InitializationType.Async:
                if (_asyncInitializationFunc == null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncInitializationFunc(objects).NoSync().GetAwaiter().GetResult();
            case InitializationType.SyncKey:
                if (_keyInitializationFunc == null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _keyInitializationFunc(key, objects);
            case InitializationType.Sync:
                if (_initializationFunc == null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _initializationFunc(objects);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetInitialization(Func<string, object[], ValueTask<T>> asyncKeyInitializationFunc)
    {
        if (_initializationType != null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.AsyncKey;
        _asyncKeyInitializationFunc = asyncKeyInitializationFunc;
    }

    public void SetInitialization(Func<object[], ValueTask<T>> asyncInitializationFunc)
    {
        if (_initializationType != null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.Async;
        _asyncInitializationFunc = asyncInitializationFunc;
    }

    public void SetInitialization(Func<object[], T> initializationFunc)
    {
        if (_initializationType != null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.Sync;
        _initializationFunc = initializationFunc;
    }

    public void SetInitialization(Func<string, object[], T> keyInitializationFunc)
    {
        if (_initializationType != null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.SyncKey;
        _keyInitializationFunc = keyInitializationFunc;
    }

    public async ValueTask Remove(string key)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        // Double lock removal

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

        // Double lock removal

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
        if (_disposed)
            return;

        _disposed = true;

        // Don't use .IsNullOrEmpty() due to unique ConcurrentDictionary properties
        if (_dictionary != null && !_dictionary.IsEmpty)
        {
            foreach (KeyValuePair<string, T> kvp in _dictionary)
            {
                DisposeInstance(kvp.Key, kvp.Value);
            }
        }

        _dictionary = null;
        GC.SuppressFinalize(this);
    }

    private void DisposeInstance(string key, T instance)
    {
        switch (instance)
        {
            case IDisposable disposable:
                disposable.Dispose();
                break;
            case IAsyncDisposable asyncDisposable:
                // Kind of a weird situation - the instance is IAsyncDisposable but the dictionary is being disposed synchronously (which can happen).
                asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
                break;
        }

        _dictionary?.TryRemove(key, out _);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Don't use .IsNullOrEmpty() due to unique ConcurrentDictionary properties
        if (_dictionary != null && _dictionary.IsEmpty)
        {
            foreach (KeyValuePair<string, T> kvp in _dictionary)
            {
                await DisposeInstanceAsync(kvp.Key, kvp.Value).NoSync();
            }
        }

        _dictionary = null;
        GC.SuppressFinalize(this);
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

        _dictionary?.TryRemove(key, out _);
    }
}