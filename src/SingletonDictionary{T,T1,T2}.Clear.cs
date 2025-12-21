using Soenneker.Extensions.ValueTask;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.SingletonDictionary;

public sealed partial class SingletonDictionary<T, T1, T2>
{
    public void ClearSync()
    {
        ThrowIfDisposed();

        using (_lock.LockSync())
        {
            ThrowIfDisposed();

            if (_dictionary is null || _dictionary.IsEmpty)
                return;

            foreach (KeyValuePair<string, T> kvp in _dictionary)
            {
                if (_dictionary.TryRemove(kvp.Key, out T? instance))
                    DisposeRemovedInstanceSync(instance);
            }
        }
    }

    public async ValueTask Clear(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using (await _lock.Lock(cancellationToken)
                          .NoSync())
        {
            ThrowIfDisposed();

            if (_dictionary is null || _dictionary.IsEmpty)
                return;

            foreach (KeyValuePair<string, T> kvp in _dictionary)
            {
                if (_dictionary.TryRemove(kvp.Key, out T? instance))
                    await DisposeRemovedInstance(instance).NoSync();
            }
        }
    }
}

