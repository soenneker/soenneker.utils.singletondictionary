using Nito.AsyncEx;
using Soenneker.Atomics.ValueBools;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.SingletonDictionary.Abstract;
using Soenneker.Utils.SingletonDictionary.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.SingletonDictionary;

public sealed partial class SingletonDictionary<T, T1> : ISingletonDictionary<T, T1>
{
    private ConcurrentDictionary<string, T>? _dictionary;
    private readonly AsyncLock _lock;

    private Func<string, CancellationToken, T1, ValueTask<T>>? _asyncKeyTokenFunc;
    private Func<string, CancellationToken, T1, T>? _keyTokenFunc;

    private Func<string, T1, ValueTask<T>>? _asyncKeyFunc;
    private Func<string, T1, T>? _keyFunc;

    private Func<T1, ValueTask<T>>? _asyncFunc;
    private Func<T1, T>? _func;

    private ValueAtomicBool _disposed;
    private InitializationType? _initializationType;

    public SingletonDictionary()
    {
        _lock = new AsyncLock();
        _dictionary = new ConcurrentDictionary<string, T>();
    }

    public SingletonDictionary(Func<string, T1, ValueTask<T>> func) : this()
    {
        _initializationType = InitializationType.AsyncKey;
        _asyncKeyFunc = func;
    }

    public SingletonDictionary(Func<string, CancellationToken, T1, ValueTask<T>> func) : this()
    {
        _initializationType = InitializationType.AsyncKeyToken;
        _asyncKeyTokenFunc = func;
    }

    public SingletonDictionary(Func<T1, ValueTask<T>> func) : this()
    {
        _initializationType = InitializationType.Async;
        _asyncFunc = func;
    }

    public SingletonDictionary(Func<string, T1, T> func) : this()
    {
        _initializationType = InitializationType.SyncKey;
        _keyFunc = func;
    }

    public SingletonDictionary(Func<string, CancellationToken, T1, T> func) : this()
    {
        _initializationType = InitializationType.SyncKeyToken;
        _keyTokenFunc = func;
    }

    public SingletonDictionary(Func<T1, T> func) : this()
    {
        _initializationType = InitializationType.Sync;
        _func = func;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<T> Get(string key, T1 arg, CancellationToken cancellationToken = default)
        => GetCore(key, arg, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(string key, out T? value)
    {
        ThrowIfDisposed();

        ConcurrentDictionary<string, T>? dict = _dictionary;
        if (dict is null)
        {
            value = default;
            return false;
        }

        return dict.TryGetValue(key, out value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<T> Get(string key, Func<T1> argFactory, CancellationToken cancellationToken = default)
        => GetCore(key, argFactory, cancellationToken);

    public async ValueTask<T> GetCore(string key, Func<T1> argFactory, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_dictionary!.TryGetValue(key, out T? instance))
            return instance;

        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            ThrowIfDisposed();

            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            T1 arg = argFactory();

            instance = await GetInternal(key, cancellationToken, arg).NoSync();
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetSync(string key, Func<T1> argFactory, CancellationToken cancellationToken = default)
        => GetCoreSync(key, argFactory, cancellationToken);

    public T GetCoreSync(string key, Func<T1> argFactory, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_dictionary!.TryGetValue(key, out T? instance))
            return instance;

        using (_lock.Lock(cancellationToken))
        {
            ThrowIfDisposed();

            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            T1 arg = argFactory();

            instance = GetInternalSync(key, cancellationToken, arg);
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    public async ValueTask<T> GetCore(string key, T1 arg, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_dictionary!.TryGetValue(key, out T? instance))
            return instance;

        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            ThrowIfDisposed();

            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            instance = await GetInternal(key, cancellationToken, arg).NoSync();
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    public T GetSync(string key, T1 arg, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_dictionary!.TryGetValue(key, out T? instance))
            return instance;

        using (_lock.Lock(cancellationToken))
        {
            ThrowIfDisposed();

            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            instance = GetInternalSync(key, cancellationToken, arg);
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    private ValueTask<T> GetInternal(string key, CancellationToken cancellationToken, T1 arg)
    {
        if (_initializationType is null)
            throw new InvalidOperationException("Initialization func for SingletonDictionary cannot be null");

        switch (_initializationType.Value)
        {
            case InitializationType.AsyncKeyValue:
                if (_asyncKeyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncKeyFunc(key, arg);

            case InitializationType.AsyncKeyTokenValue:
                if (_asyncKeyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncKeyTokenFunc(key, cancellationToken, arg);

            case InitializationType.AsyncValue:
                if (_asyncFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncFunc(arg);

            case InitializationType.SyncValue:
                if (_func is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return new ValueTask<T>(_func(arg));

            case InitializationType.SyncKeyTokenValue:
                if (_keyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return new ValueTask<T>(_keyTokenFunc(key, cancellationToken, arg));

            case InitializationType.SyncKeyValue:
                if (_keyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return new ValueTask<T>(_keyFunc(key, arg));

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private T GetInternalSync(string key, CancellationToken cancellationToken, T1 arg)
    {
        if (_initializationType is null)
            throw new InvalidOperationException("Initialization func for SingletonDictionary cannot be null");

        switch (_initializationType.Value)
        {
            case InitializationType.AsyncKeyValue:
                if (_asyncKeyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncKeyFunc(key, arg).AwaitSync();

            case InitializationType.AsyncKeyTokenValue:
                if (_asyncKeyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncKeyTokenFunc(key, cancellationToken, arg).AwaitSync();

            case InitializationType.AsyncValue:
                if (_asyncFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _asyncFunc(arg).AwaitSync();

            case InitializationType.SyncKeyValue:
                if (_keyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _keyFunc(key, arg);

            case InitializationType.SyncKeyTokenValue:
                if (_keyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _keyTokenFunc(key, cancellationToken, arg);

            case InitializationType.SyncValue:
                if (_func is null)
                    throw new NullReferenceException("Initialization func for SingletonDictionary cannot be null");

                return _func(arg);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetInitialization(Func<string, T1, ValueTask<T>> func)
    {
        if (_initializationType is not null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.AsyncKey;
        _asyncKeyFunc = func;
    }

    public void SetInitialization(Func<string, CancellationToken, T1, ValueTask<T>> func)
    {
        if (_initializationType is not null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.AsyncKeyToken;
        _asyncKeyTokenFunc = func;
    }

    public void SetInitialization(Func<T1, ValueTask<T>> func)
    {
        if (_initializationType is not null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.Async;
        _asyncFunc = func;
    }

    public void SetInitialization(Func<T1, T> func)
    {
        if (_initializationType is not null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.Sync;
        _func = func;
    }

    public void SetInitialization(Func<string, T1, T> func)
    {
        if (_initializationType is not null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.SyncKey;
        _keyFunc = func;
    }

    public void SetInitialization(Func<string, CancellationToken, T1, T> func)
    {
        if (_initializationType is not null)
            throw new Exception("Setting the initialization of an SingletonDictionary after it's already has been set is not allowed");

        _initializationType = InitializationType.SyncKeyToken;
        _keyTokenFunc = func;
    }

    public async ValueTask Remove(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_dictionary!.TryRemove(key, out T? instance))
        {
            await DisposeRemovedInstance(instance).NoSync();
            return;
        }

        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            ThrowIfDisposed();

            if (_dictionary is not null && _dictionary.TryRemove(key, out instance))
                await DisposeRemovedInstance(instance).NoSync();
        }
    }

    public void RemoveSync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_dictionary!.TryRemove(key, out T? instance))
        {
            DisposeRemovedInstanceSync(instance);
            return;
        }

        using (_lock.Lock(cancellationToken))
        {
            ThrowIfDisposed();

            if (_dictionary is not null && _dictionary.TryRemove(key, out instance))
                DisposeRemovedInstanceSync(instance);
        }
    }

    public void Dispose()
    {
        if (!_disposed.TrySetTrue())
            return;

        ConcurrentDictionary<string, T>? dict = _dictionary;
        _dictionary = null;

        if (dict is null || dict.IsEmpty)
            return;

        foreach (KeyValuePair<string, T> kvp in dict)
        {
            if (dict.TryRemove(kvp.Key, out T? instance))
                DisposeRemovedInstanceSync(instance);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed.TrySetTrue())
            return;

        ConcurrentDictionary<string, T>? dict = _dictionary;
        _dictionary = null;

        if (dict is null || dict.IsEmpty)
            return;

        foreach (KeyValuePair<string, T> kvp in dict)
        {
            if (dict.TryRemove(kvp.Key, out T? instance))
                await DisposeRemovedInstance(instance).NoSync();
        }
    }

    private static void DisposeRemovedInstanceSync(T instance)
    {
        switch (instance)
        {
            case IDisposable disposable:
                disposable.Dispose();
                break;
            case IAsyncDisposable asyncDisposable:
                asyncDisposable.DisposeAsync().AwaitSync();
                break;
        }
    }

    private static async ValueTask DisposeRemovedInstance(T instance)
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed.Value)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T, T1>));
    }
}
