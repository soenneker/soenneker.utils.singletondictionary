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

    void SetInitialization(Func<string, ValueTask<T>> func);

    void SetInitialization(Func<string, CancellationToken, ValueTask<T>> func);

    void SetInitialization(Func<ValueTask<T>> func);

    void SetInitialization(Func<T> func);

    void SetInitialization(Func<string, T> func);

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

