using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.SingletonDictionary;

public sealed partial class SingletonDictionary<T>
{
    public async ValueTask<Dictionary<string, T>> GetAll(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(true, _disposed);

        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            return _dictionary is null ? new Dictionary<string, T>() : new Dictionary<string, T>(_dictionary);
        }
    }

    public async ValueTask<List<string>> GetKeys(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(true, _disposed);

        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            return _dictionary?.Keys is { } keys ? [.. keys] : [];
        }
    }

    public async ValueTask<List<T>> GetValues(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(true, _disposed);

        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            return _dictionary?.Values is { } values ? [.. values] : [];
        }
    }

    public Dictionary<string, T> GetAllSync()
    {
        ObjectDisposedException.ThrowIf(true, _disposed);

        using (_lock.Lock())
        {
            return _dictionary is null ? new Dictionary<string, T>() : new Dictionary<string, T>(_dictionary);
        }
    }

    public List<string> GetKeysSync()
    {
        ObjectDisposedException.ThrowIf(true, _disposed);

        using (_lock.Lock())
        {
            return _dictionary?.Keys is { } keys ? [.. keys] : [];
        }
    }

    public List<T> GetValuesSync()
    {
        ObjectDisposedException.ThrowIf(true, _disposed);

        using (_lock.Lock())
        {
            return _dictionary?.Values is { } values ? [.. values] : [];
        }
    }
}