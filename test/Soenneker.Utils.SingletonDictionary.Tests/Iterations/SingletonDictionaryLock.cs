using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.SingletonDictionary.Enums;

namespace Soenneker.Utils.SingletonDictionary.Tests.Iterations;

public sealed partial class SingletonDictionaryLock<T> : IDisposable, IAsyncDisposable
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
    public SingletonDictionaryLock(Func<string, object[], ValueTask<T>> func) : this()
    {
        _initializationType = InitializationType.AsyncKey;
        _asyncKeyFunc = func;
    }

    public SingletonDictionaryLock(Func<string, CancellationToken, object[], ValueTask<T>> func) : this()
    {
        _initializationType = InitializationType.AsyncKeyToken;
        _asyncKeyTokenFunc = func;
    }

    /// <summary>
    /// If an async initialization func is used, it's recommend that GetSync() NOT be used.
    /// </summary>
    public SingletonDictionaryLock(Func<object[], ValueTask<T>> func) : this()
    {
        _initializationType = InitializationType.Async;
        _asyncFunc = func;
    }

    public SingletonDictionaryLock(Func<string, object[], T> func) : this()
    {
        _initializationType = InitializationType.SyncKey;
        _keyFunc = func;
    }

    public SingletonDictionaryLock(Func<string, CancellationToken, object[], T> func) : this()
    {
        _initializationType = InitializationType.SyncKeyToken;
        _keyTokenFunc = func;
    }

    public SingletonDictionaryLock(Func<object[], T> func) : this()
    {
        _initializationType = InitializationType.Sync;
        _func = func;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    /// If this is used, be sure to set the initialization func, see SetInitialization or use another constructor.
    /// </summary>
    public SingletonDictionaryLock()
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
        ObjectDisposedException.ThrowIf(_disposed, nameof(SingletonDictionaryLock<T>));

        if (_dictionary!.TryGetValue(key, out T? instance))
            return instance;

        using (await _lock.LockAsync(cancellationToken)
                          .ConfigureAwait(false))
        {
            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            instance = await GetInternal(key, cancellationToken, objects)
                .NoSync();
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    public T GetSync(string key, params object[] objects)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SingletonDictionaryLock<T>));

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
            case nameof(InitializationType.AsyncKey):
                if (_asyncKeyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return await _asyncKeyFunc(key, objects)
                    .NoSync();
            case nameof(InitializationType.AsyncKeyToken):
                if (_asyncKeyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return await _asyncKeyTokenFunc(key, cancellationToken, objects)
                    .NoSync();
            case nameof(InitializationType.Async):
                if (_asyncFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return await _asyncFunc(objects)
                    .NoSync();
            case nameof(InitializationType.Sync):
                if (_func is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _func(objects);
            case nameof(InitializationType.SyncKeyToken):
                if (_keyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _keyTokenFunc(key, cancellationToken, objects);
            case nameof(InitializationType.SyncKey):
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
            case nameof(InitializationType.AsyncKey):
                if (_asyncKeyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncKeyFunc(key, objects)
                    .AwaitSync();
            case nameof(InitializationType.AsyncKeyToken):
                if (_asyncKeyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncKeyTokenFunc(key, CancellationToken.None, objects)
                    .AwaitSync();
            case nameof(InitializationType.Async):
                if (_asyncFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncFunc(objects)
                    .AwaitSync();
            case nameof(InitializationType.SyncKey):
                if (_keyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _keyFunc(key, objects);
            case nameof(InitializationType.SyncKeyToken):
                if (_keyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _keyTokenFunc(key, CancellationToken.None, objects);
            case nameof(InitializationType.Sync):
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

    public async ValueTask Remove(string key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SingletonDictionaryLock<T>));

        // Double lock removal

        if (_dictionary!.TryGetValue(key, out T? instance))
        {
            await DisposeInstance(key, instance)
                .NoSync();
            return;
        }

        using (await _lock.LockAsync(cancellationToken)
                          .ConfigureAwait(false))
        {
            if (_dictionary.TryGetValue(key, out instance))
            {
                await DisposeInstance(key, instance)
                    .NoSync();
            }
        }
    }

    public void RemoveSync(string key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SingletonDictionaryLock<T>));

        // Double lock removal

        if (_dictionary!.TryGetValue(key, out T? instance))
        {
            DisposeInstanceSync(key, instance);
            return;
        }

        using (_lock.Lock(cancellationToken))
        {
            if (_dictionary.TryGetValue(key, out instance))
            {
                DisposeInstanceSync(key, instance);
            }
        }
    }

    private void DisposeInstanceSync(string key, T instance)
    {
        switch (instance)
        {
            case IDisposable disposable:
                disposable.Dispose();
                break;
            case IAsyncDisposable asyncDisposable:
                // Kind of a weird situation - the instance is IAsyncDisposable but the dictionary is being disposed synchronously (which can happen).
                asyncDisposable.DisposeAsync()
                               .AwaitSync();
                break;
        }

        _dictionary?.TryRemove(key, out _);
    }

    private async ValueTask DisposeInstance(string key, T instance)
    {
        switch (instance)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync()
                                     .NoSync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }

        _dictionary?.TryRemove(key, out _);
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
                DisposeInstanceSync(kvp.Key, kvp.Value);
            }
        }

        _dictionary = null;
        GC.SuppressFinalize(this);
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
                await DisposeInstance(kvp.Key, kvp.Value)
                    .NoSync();
            }
        }

        _dictionary = null;
        GC.SuppressFinalize(this);
    }

    public async ValueTask<Dictionary<string, T>> GetAll(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SingletonDictionaryLock<T>));

        using (await _lock.LockAsync(cancellationToken)
                          .ConfigureAwait(false))
        {
            return _dictionary is null ? new Dictionary<string, T>() : new Dictionary<string, T>(_dictionary);
        }
    }

    public async ValueTask<List<string>> GetKeys(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SingletonDictionaryLock<T>));

        using (await _lock.LockAsync(cancellationToken)
                          .ConfigureAwait(false))
        {
            return _dictionary?.Keys is { } keys ? [.. keys] : [];
        }
    }

    public async ValueTask<List<T>> GetValues(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SingletonDictionaryLock<T>));

        using (await _lock.LockAsync(cancellationToken)
                          .ConfigureAwait(false))
        {
            return _dictionary?.Values is { } values ? [.. values] : [];
        }
    }

    public Dictionary<string, T> GetAllSync()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SingletonDictionaryLock<T>));

        using (_lock.Lock())
        {
            return _dictionary is null ? new Dictionary<string, T>() : new Dictionary<string, T>(_dictionary);
        }
    }

    public List<string> GetKeysSync()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SingletonDictionaryLock<T>));

        using (_lock.Lock())
        {
            return _dictionary?.Keys is { } keys ? [.. keys] : [];
        }
    }

    public List<T> GetValuesSync()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SingletonDictionaryLock<T>));

        using (_lock.Lock())
        {
            return _dictionary?.Values is { } values ? [.. values] : [];
        }
    }

    public void ClearSync()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SingletonDictionaryLock<T>));

        using (_lock.Lock())
        {
            if (_dictionary is null || _dictionary.IsEmpty)
                return;

            foreach (KeyValuePair<string, T> kvp in _dictionary)
            {
                DisposeInstanceSync(kvp.Key, kvp.Value);
            }
        }
    }

    public async ValueTask Clear(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SingletonDictionaryLock<T>));

        using (await _lock.LockAsync(cancellationToken)
                          .ConfigureAwait(false))
        {
            if (_dictionary is null || _dictionary.IsEmpty)
                return;

            foreach (KeyValuePair<string, T> kvp in _dictionary)
            {
                await DisposeInstance(kvp.Key, kvp.Value)
                    .NoSync();
            }
        }
    }
}