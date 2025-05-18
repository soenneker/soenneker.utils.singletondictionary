using Soenneker.Extensions.ValueTask;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.SingletonDictionary;

public sealed partial class SingletonDictionary<T>
{
    public void ClearSync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

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
        if (_disposed)
            throw new ObjectDisposedException(nameof(SingletonDictionary<T>));

        using (await _lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_dictionary is null || _dictionary.IsEmpty)
                return;

            foreach (KeyValuePair<string, T> kvp in _dictionary)
            {
                await DisposeInstance(kvp.Key, kvp.Value).NoSync();
            }
        }
    }
}