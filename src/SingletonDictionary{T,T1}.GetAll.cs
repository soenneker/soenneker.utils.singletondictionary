using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.SingletonDictionary;

public sealed partial class SingletonDictionary<T, T1>
{
    public async ValueTask<Dictionary<string, T>> GetAll(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using (await _lock.Lock(cancellationToken)
                          .NoSync())
        {
            ThrowIfDisposed();

            return _dictionary is null ? new Dictionary<string, T>() : new Dictionary<string, T>(_dictionary);
        }
    }

    public async ValueTask<List<string>> GetKeys(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using (await _lock.Lock(cancellationToken)
                          .NoSync())
        {
            ThrowIfDisposed();

            return _dictionary?.Keys is { } keys ? [.. keys] : [];
        }
    }

    public async ValueTask<List<T>> GetValues(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using (await _lock.Lock(cancellationToken)
                          .NoSync())
        {
            ThrowIfDisposed();

            return _dictionary?.Values is { } values ? [.. values] : [];
        }
    }

    public Dictionary<string, T> GetAllSync()
    {
        ThrowIfDisposed();

        using (_lock.LockSync())
        {
            ThrowIfDisposed();

            return _dictionary is null ? new Dictionary<string, T>() : new Dictionary<string, T>(_dictionary);
        }
    }

    public List<string> GetKeysSync()
    {
        ThrowIfDisposed();

        using (_lock.LockSync())
        {
            ThrowIfDisposed();

            return _dictionary?.Keys is { } keys ? [.. keys] : [];
        }
    }

    public List<T> GetValuesSync()
    {
        ThrowIfDisposed();

        using (_lock.LockSync())
        {
            ThrowIfDisposed();

            return _dictionary?.Values is { } values ? [.. values] : [];
        }
    }
}
