using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AzuriteUI.Web.Services.CacheDb.Comparers;

/// <summary>
/// A value comparer for <see cref="IDictionary{TKey,TValue}"/> properties with string keys and values.
/// This ensures Entity Framework Core can properly detect changes to dictionary properties.
/// </summary>
public class DictionaryValueComparer : ValueComparer<IDictionary<string, string>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryValueComparer"/> class.
    /// </summary>
    public DictionaryValueComparer()
        : base(
            (d1, d2) => CompareDictionaries(d1, d2),
            d => GetDictionaryHashCode(d),
            d => SnapshotDictionary(d))
    {
    }

    /// <summary>
    /// Compares two dictionaries for equality.
    /// </summary>
    /// <param name="d1">The first dictionary.</param>
    /// <param name="d2">The second dictionary.</param>
    /// <returns>True if the dictionaries are equal, false otherwise.</returns>
    private static bool CompareDictionaries(IDictionary<string, string>? d1, IDictionary<string, string>? d2)
    {
        if (d1 == null && d2 == null)
            return true;

        if (d1 == null || d2 == null)
            return false;

        if (d1.Count != d2.Count)
            return false;

        foreach (var kvp in d1)
        {
            if (!d2.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Computes a hash code for the dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to hash.</param>
    /// <returns>The hash code.</returns>
    private static int GetDictionaryHashCode(IDictionary<string, string> dictionary)
    {
        if (dictionary == null)
            return 0;

        var hash = new HashCode();
        foreach (var kvp in dictionary.OrderBy(x => x.Key))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Creates a snapshot (deep copy) of the dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to snapshot.</param>
    /// <returns>A new dictionary with the same contents.</returns>
    private static IDictionary<string, string> SnapshotDictionary(IDictionary<string, string> dictionary)
    {
        return dictionary == null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(dictionary);
    }
}
