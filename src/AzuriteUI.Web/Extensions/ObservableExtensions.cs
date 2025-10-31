using System.Reactive.Linq;

namespace AzuriteUI.Web.Extensions;

/// <summary>
/// Extension methods for working with the <see cref="IObservable{T}"/> interface.
/// </summary>
public static class ObservableExtensions
{
    /// <summary>
    /// Converts an IAsyncEnumerable to an IObservable.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The async enumerable source.</param>
    /// <returns>An observable sequence that yields elements from the async enumerable.</returns>
    public static IObservable<T> ToObservable<T>(this IAsyncEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Observable.Create<T>(async (observer, cancellationToken) =>
        {
            try
            {
                await foreach (var item in source.WithCancellation(cancellationToken))
                {
                    observer.OnNext(item);
                }
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });
    }
}