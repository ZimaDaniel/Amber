﻿namespace Amber.Common;

public static class Extensions
{
	public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFactory)
		where TKey : notnull
	{
		if (!dictionary.TryGetValue(key, out var value))
		{
			value = valueFactory();
			dictionary.Add(key, value);
		}

		return value;
	}

	public static TValue GetOrThrow<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<Exception> throwFactory)
		where TKey : notnull
	{
		if (!dictionary.TryGetValue(key, out var value))
		{
			throw throwFactory();
		}

		return value;
	}

	public static void SetAllNull<T>(this T?[] collection)
		where T: notnull
	{
        for (int i = 0; i < collection.Length; i++)
        {
            collection[i] = default;
        }
    }
}
