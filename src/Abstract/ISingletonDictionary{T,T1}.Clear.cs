using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.SingletonDictionary.Abstract;

public partial interface ISingletonDictionary<T, T1>
{
    /// <summary>
    /// Clears all values from the dictionary and disposes them if disposable (sync).
    /// </summary>
    void ClearSync();

    /// <summary>
    /// Clears all values from the dictionary and disposes them if disposable (async).
    /// </summary>
    ValueTask Clear(CancellationToken cancellationToken = default);
}

