using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.SingletonDictionary.Abstract;

/// <summary>
/// An externally initializing singleton dictionary that uses double-check asynchronous locking, with optional async and sync disposal
/// </summary>
/// <remarks>Be sure to dispose of this gracefully if using a Disposable type</remarks>
public partial interface ISingletonDictionary<T> : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Utilizes double-check async locking to guarantee there's only one instance of the object. It's lazy; it's initialized only when retrieving.
    /// This method should be called even if the initialization func was synchronous.
    /// </summary>
    /// <remarks>The initialization func needs to be set before calling this, either in the ctor or via the other methods</remarks>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="NullReferenceException"></exception>
    [Pure]
    ValueTask<T> Get(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to retrieve a value from the dictionary without initializing if it doesn't exist.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the dictionary has been disposed.</exception>
    [Pure]
    bool TryGet(string key, out T? value);

    /// <summary>
    /// <see cref="Get(string, CancellationToken)"/> should be used instead of this if possible. This method can block the calling thread! It's lazy; it's initialized only when retrieving.
    /// This can still be used with an async initialization func, but it will block on the func.
    /// </summary>
    /// <remarks>The initialization func needs to be set before calling this, either in the ctor or via the other methods</remarks>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="NullReferenceException"></exception>
    [Pure]
    T GetSync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the initialization function that will be used to create instances when they are requested.
    /// The function receives the key as a parameter and returns a ValueTask with the instance.
    /// </summary>
    /// <param name="func">The async function that takes a key and returns the instance to be cached.</param>
    /// <exception cref="Exception">Thrown when attempting to set initialization after it has already been set.</exception>
    void SetInitialization(Func<string, ValueTask<T>> func);

    /// <summary>
    /// Sets the initialization function that will be used to create instances when they are requested.
    /// The function receives the key and cancellation token as parameters and returns a ValueTask with the instance.
    /// </summary>
    /// <param name="func">The async function that takes a key and cancellation token, and returns the instance to be cached.</param>
    /// <exception cref="Exception">Thrown when attempting to set initialization after it has already been set.</exception>
    void SetInitialization(Func<string, CancellationToken, ValueTask<T>> func);

    /// <summary>
    /// Sets the initialization function that will be used to create instances when they are requested.
    /// The function does not receive a key parameter and returns a ValueTask with the instance.
    /// </summary>
    /// <param name="func">The async function that returns the instance to be cached.</param>
    /// <exception cref="Exception">Thrown when attempting to set initialization after it has already been set.</exception>
    void SetInitialization(Func<ValueTask<T>> func);

    /// <summary>
    /// Sets the initialization function that will be used to create instances when they are requested.
    /// The function does not receive a key parameter and returns the instance synchronously.
    /// </summary>
    /// <param name="func">The synchronous function that returns the instance to be cached.</param>
    /// <exception cref="Exception">Thrown when attempting to set initialization after it has already been set.</exception>
    void SetInitialization(Func<T> func);

    /// <summary>
    /// Sets the initialization function that will be used to create instances when they are requested.
    /// The function receives the key as a parameter and returns the instance synchronously.
    /// </summary>
    /// <param name="func">The synchronous function that takes a key and returns the instance to be cached.</param>
    /// <exception cref="Exception">Thrown when attempting to set initialization after it has already been set.</exception>
    void SetInitialization(Func<string, T> func);

    /// <summary>
    /// Sets the initialization function that will be used to create instances when they are requested.
    /// The function receives the key and cancellation token as parameters and returns the instance synchronously.
    /// </summary>
    /// <param name="func">The synchronous function that takes a key and cancellation token, and returns the instance to be cached.</param>
    /// <exception cref="Exception">Thrown when attempting to set initialization after it has already been set.</exception>
    void SetInitialization(Func<string, CancellationToken, T> func);

    /// <summary>
    /// Includes disposal of the key if applicable. Recommended over <see cref="RemoveSync(string,CancellationToken)"/>
    /// </summary>
    ValueTask Remove(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Includes disposal of the key if applicable. Async removal <see cref="Remove(string,CancellationToken)"/> is recommended instead of this.
    /// </summary>
    void RemoveSync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// If the instance is an IDisposable, Dispose will be called on the method (and DisposeAsync will not) <para/>
    /// If the instance is ONLY an IAsyncDisposable and this is called, it will block while disposing. You should try to avoid this. <para/>
    /// </summary>
    /// <remarks>Disposal is not necessary unless the object's type is IDisposable/IAsyncDisposable</remarks>
    new void Dispose();

    /// <summary>
    /// This is the preferred method of disposal. This will asynchronously dispose of the instance if the object is an IAsyncDisposable <para/>
    /// There shouldn't be a need to call ConfigureAwait(false) on this. <para/>
    /// </summary>
    /// <remarks>Disposal is not necessary unless the object's type is IDisposable/IAsyncDisposable</remarks>
    new ValueTask DisposeAsync();
}


