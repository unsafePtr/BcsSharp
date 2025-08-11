namespace BcsSharp.Core.Extensions;
public static class DictionaryExtensions
{
    private static readonly Lock _lock = new();

    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out TValue? value))
        {
            return value;
        }

        lock (_lock)
        {
            if (dictionary.TryGetValue(key, out value))
            {
                return value; // Another thread added it while we were waiting
            }

            value = valueFactory(key);
            dictionary.Add(key, value);
        }

        return value;
    }
}
