using System;
using System.Collections.Generic;

namespace TeamSpeak3QueryApi.Net;

/// <summary>
/// Extensions for dealing with <see cref="Dictionary{TKey,TValue}"/>
/// Ref: https://stackoverflow.com/a/61066641
/// </summary>
public static class DictionaryExtensions
{
    public static TValue GetOrAddNew<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default)
        where TValue : new()
        => dict.GetOrAdd(key, (values, innerKey) => EqualityComparer<TValue>.Default.Equals(default, defaultValue) ? new TValue() : defaultValue);

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default)
        => dict.GetOrAdd(key, (values, innerKey) => defaultValue);

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> valueProvider)
        => dict.GetOrAdd(key, (values, innerKey) => valueProvider());

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> valueProvider)
        => dict.GetOrAdd(key, (values, innerKey) => valueProvider(key));

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<IDictionary<TKey, TValue>, TKey, TValue> valueProvider)
    {
        if (dict == null) throw new ArgumentNullException(nameof(dict));
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (valueProvider == null) throw new ArgumentNullException(nameof(valueProvider));

        if (dict.TryGetValue(key, out var foundValue))
            return foundValue;

        dict[key] = valueProvider(dict, key);
        return dict[key];
    }
}
