using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.SingletonDictionary;

public sealed partial class SingletonDictionary<T>
{
    public async ValueTask<List<T>> GetAll(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            return _dictionary?.Values is { } values ? [..values] : [];
        }
    }

    public async ValueTask<Dictionary<string, T>> GetAllWithKeys(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            return _dictionary is null ? new Dictionary<string, T>() : new Dictionary<string, T>(_dictionary);
        }
    }

    public List<T> GetAllSync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        using (_lock.Lock())
        {
            return _dictionary?.Values is { } values ? [.. values] : [];
        }
    }

    public Dictionary<string, T> GetAllWithKeysSync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        using (_lock.Lock())
        {
            return _dictionary is null ? new Dictionary<string, T>() : new Dictionary<string, T>(_dictionary);
        }
    }
}