using Microsoft.Extensions.Configuration;

namespace AzuriteUI.Web.UnitTests.Helpers;

[ExcludeFromCodeCoverage(Justification = "Test Helper")]
public static class Utils
{
    /// <summary>
    /// Creates an <see cref="IAsyncEnumerable{T}"/> from the provided items.
    /// </summary>
    /// <typeparam name="T">The type of the enumerable to create.</typeparam>
    /// <param name="items">The list of items.</param>
    /// <returns>The <see cref="IAsyncEnumerable{T}"/> containing the provided items.</returns>
    public static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    /// <summary>
    /// Creates an <see cref="IAsyncEnumerable{T}"/> that throws the specified exception when enumerated.
    /// </summary>
    /// <typeparam name="T">The type of the enumerable to create.</typeparam>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>The <see cref="IAsyncEnumerable{T}"/> that throws the specified exception.</returns>
    public static async IAsyncEnumerable<T> CreateAsyncEnumerableWithException<T>(Exception exception)
    {
        await Task.Yield();
        throw exception;
#pragma warning disable CS0162 // Unreachable code detected
        yield break;
#pragma warning restore CS0162 // Unreachable code detected
    }

    /// <summary>
    /// Creates an <see cref="IAsyncEnumerable{T}"/> that yields a specified number of items before throwing an exception.
    /// </summary>
    /// <param name="itemCount">The number of items to return.</param>
    /// <param name="exception">The ending exception.</param>
    /// <returns>The <see cref="IAsyncEnumerable{T}"/> that yields the specified number of items before throwing the exception.</returns>
    public static async IAsyncEnumerable<int> CreateAsyncEnumerableWithExceptionAfterItems(int itemCount, Exception exception)
    {
        for (int i = 0; i < itemCount; i++)
        {
            await Task.Yield();
            yield return i;
        }
        throw exception;
    }

    /// <summary>
    /// Creates an IConfiguration instance with optional in-memory values.
    /// </summary>
    /// <param name="values">The values to include in the configuration.</param>
    /// <returns>The IConfiguration instance.</returns>
    public static IConfiguration CreateConfiguration(Dictionary<string, string?>? values = null)
    {
        var builder = new ConfigurationBuilder();
        if (values is not null)
        {
            builder.AddInMemoryCollection(values);
        }
        return builder.Build();
    }   
}