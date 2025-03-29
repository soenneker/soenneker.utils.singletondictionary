using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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

    private Func<string, CancellationToken, object[], ValueTask<T>>? _asyncKeyTokenFunc;
    private Func<string, CancellationToken, object[], T>? _keyTokenFunc;

    private Func<string, object[], ValueTask<T>>? _asyncKeyFunc;
    private Func<string, object[], T>? _keyFunc;

    private Func<object[], ValueTask<T>>? _asyncFunc;
    private Func<object[], T>? _func;

    private bool _disposed;

    private InitializationType? _initializationType;

    /// <summary>
    /// If an async initialization func is used, it's recommend that GetSync() NOT be used.
    /// </summary>
    public SingletonDictionary(Func<string, object[], ValueTask<T>> func) : this()
    {
        _initializationType = InitializationType.AsyncKey;
        _asyncKeyFunc = func;
    }

    public SingletonDictionary(Func<string, CancellationToken, object[], ValueTask<T>> func) : this()
    {
        _initializationType = InitializationType.AsyncKeyToken;
        _asyncKeyTokenFunc = func;
    }

    /// <summary>
    /// If an async initialization func is used, it's recommend that GetSync() NOT be used.
    /// </summary>
    public SingletonDictionary(Func<object[], ValueTask<T>> func) : this()
    {
        _initializationType = InitializationType.Async;
        _asyncFunc = func;
    }

    public SingletonDictionary(Func<string, object[], T> func) : this()
    {
        _initializationType = InitializationType.SyncKey;
        _keyFunc = func;
    }

    public SingletonDictionary(Func<string, CancellationToken, object[], T> func) : this()
    {
        _initializationType = InitializationType.SyncKeyToken;
        _keyTokenFunc = func;
    }

    public SingletonDictionary(Func<object[], T> func) : this()
    {
        _initializationType = InitializationType.Sync;
        _func = func;
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

    public ValueTask<T> Get(string key, params object[] objects)
    {
        return Get(key, CancellationToken.None, objects);
    }

    public async ValueTask<T> Get(string key, CancellationToken cancellationToken, params object[] objects)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        if (_dictionary!.TryGetValue(key, out T? instance))
            return instance;

        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            instance = await GetInternal(key, cancellationToken, objects).NoSync();
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

    private async ValueTask<T> GetInternal(string key, CancellationToken cancellationToken, object[] objects)
    {
        switch (_initializationType)
        {
            case InitializationType.AsyncKey:
                if (_asyncKeyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return await _asyncKeyFunc(key, objects).NoSync();
            case InitializationType.AsyncKeyToken:
                if (_asyncKeyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return await _asyncKeyTokenFunc(key, cancellationToken, objects).NoSync();
            case InitializationType.Async:
                if (_asyncFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return await _asyncFunc(objects).NoSync();
            case InitializationType.Sync:
                if (_func is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _func(objects);
            case InitializationType.SyncKeyToken:
                if (_keyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _keyTokenFunc(key, cancellationToken, objects);
            case InitializationType.SyncKey:
                if (_keyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _keyFunc(key, objects);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private T GetInternalSync(string key, object[] objects)
    {
        switch (_initializationType)
        {
            case InitializationType.AsyncKey:
                if (_asyncKeyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncKeyFunc(key, objects).AwaitSync();
            case InitializationType.AsyncKeyToken:
                if (_asyncKeyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncKeyTokenFunc(key, CancellationToken.None, objects).AwaitSync();
            case InitializationType.Async:
                if (_asyncFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncFunc(objects).AwaitSync();
            case InitializationType.SyncKey:
                if (_keyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _keyFunc(key, objects);
            case InitializationType.SyncKeyToken:
                if (_keyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _keyTokenFunc(key, CancellationToken.None, objects);
            case InitializationType.Sync:
                if (_func is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _func(objects);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetInitialization(Func<string, object[], ValueTask<T>> func)
    {
        if (_initializationType is not null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.AsyncKey;
        _asyncKeyFunc = func;
    }

    public void SetInitialization(Func<string, CancellationToken, object[], ValueTask<T>> func)
    {
        if (_initializationType is not null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.AsyncKeyToken;
        _asyncKeyTokenFunc = func;
    }

    public void SetInitialization(Func<object[], ValueTask<T>> func)
    {
        if (_initializationType is not null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.Async;
        _asyncFunc = func;
    }

    public void SetInitialization(Func<object[], T> func)
    {
        if (_initializationType is not null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.Sync;
        _func = func;
    }

    public void SetInitialization(Func<string, object[], T> func)
    {
        if (_initializationType is not null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.SyncKey;
        _keyFunc = func;
    }

    public void SetInitialization(Func<string, CancellationToken, object[], T> func)
    {
        if (_initializationType is not null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.SyncKeyToken;
        _keyTokenFunc = func;
    }

    public ValueTask Remove(string key)
    {
        return Remove(key, CancellationToken.None);
    }

    public async ValueTask Remove(string key, CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        // Double lock removal

        if (_dictionary!.TryGetValue(key, out T? instance))
        {
            await DisposeInstanceAsync(key, instance).NoSync();
            return;
        }

        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_dictionary.TryGetValue(key, out instance))
            {
                await DisposeInstanceAsync(key, instance).NoSync();
            }
        }
    }

    public void RemoveSync(string key)
    {
        RemoveSync(key, CancellationToken.None);
    }

    public void RemoveSync(string key, CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        // Double lock removal

        if (_dictionary!.TryGetValue(key, out T? instance))
        {
            DisposeInstance(key, instance);
            return;
        }

        using (_lock.Lock(cancellationToken))
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
        if (_dictionary is not null && !_dictionary.IsEmpty)
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
                asyncDisposable.DisposeAsync().AwaitSync();
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
        if (_dictionary is not null && !_dictionary.IsEmpty)
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