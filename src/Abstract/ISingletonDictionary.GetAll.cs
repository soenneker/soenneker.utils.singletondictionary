using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.SingletonDictionary.Abstract;

public partial interface ISingletonDictionary<T>
{
    /// <summary>
    /// Retrieves all key-value pairs currently in the dictionary.
    /// </summary>
    /// <returns>A new dictionary containing all keys and instances.</returns>
    [Pure]
    Dictionary<string, T> GetAllSync();

    /// <summary>
    /// Asynchronously retrieves all key-value pairs currently in the dictionary.
    /// </summary>
    /// <param name="cancellationToken">An optional token to cancel the operation.</param>
    /// <returns>A new dictionary containing all keys and instances.</returns>
    [Pure]
    ValueTask<Dictionary<string, T>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all keys currently in the dictionary.
    /// </summary>
    /// <returns>A list of all keys.</returns>
    [Pure]
    List<string> GetKeysSync();

    /// <summary>
    /// Asynchronously retrieves all keys currently in the dictionary.
    /// </summary>
    /// <param name="cancellationToken">An optional token to cancel the operation.</param>
    /// <returns>A list of all keys.</returns>
    [Pure]
    ValueTask<List<string>> GetKeys(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all values currently in the dictionary.
    /// </summary>
    /// <returns>A list of all instances.</returns>
    [Pure]
    List<T> GetValuesSync();

    /// <summary>
    /// Asynchronously retrieves all values currently in the dictionary.
    /// </summary>
    /// <param name="cancellationToken">An optional token to cancel the operation.</param>
    /// <returns>A list of all instances.</returns>
    [Pure]
    ValueTask<List<T>> GetValues(CancellationToken cancellationToken = default);
}