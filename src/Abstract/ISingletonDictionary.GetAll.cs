using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.SingletonDictionary.Abstract;

public partial interface ISingletonDictionary<T>
{
    /// <summary>
    /// Asynchronously retrieves all stored instances of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A list of all stored instances of <typeparamref name="T"/>. Returns an empty list if none exist.</returns>
    [Pure]
    ValueTask<List<T>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves all stored instances of <typeparamref name="T"/> with their associated keys.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A dictionary containing all keys and instances of <typeparamref name="T"/>. Returns an empty dictionary if none exist.</returns>
    [Pure]
    ValueTask<Dictionary<string, T>> GetAllWithKeys(CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously retrieves all stored instances of <typeparamref name="T"/>.
    /// </summary>
    /// <returns>A list of all stored instances of <typeparamref name="T"/>. Returns an empty list if none exist.</returns>
    [Pure]
    List<T> GetAllSync();

    /// <summary>
    /// Synchronously retrieves all stored instances of <typeparamref name="T"/> with their associated keys.
    /// </summary>
    /// <returns>A dictionary containing all keys and instances of <typeparamref name="T"/>. Returns an empty dictionary if none exist.</returns>
    [Pure]
    Dictionary<string, T> GetAllWithKeysSync();
}