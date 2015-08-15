﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EasyNetQ
{
    public static class Extentions
    {
        public static void SafeDispose(this IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch
            {
                // Ignore any exceptions
            }
        }

        public static TimeSpan Double(this TimeSpan timeSpan)
        {
            return timeSpan + timeSpan;
        }

        public static void Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source, TKey key)
        {
            ((IDictionary<TKey, TValue>) source).Remove(key);
        }

        public static void Add<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source, TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)source).Add(key, value);        
        }
    }

}