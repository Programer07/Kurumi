using MongoDB.Driver;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Services.Database
{
    public abstract class KurumiDatabase<T> where T : class, new()
    {
        public static ConcurrentDictionary<ulong, T> Cache;

        /// <summary>
        /// Sets an entry in the database at Key to Value, if value is null => remove
        /// </summary>
        /// <param name="Key">The key in the database</param>
        /// <param name="Value">The value it will be set to</param>
        public static void Set(ulong Key, T Value)
        {
            if (Cache.ContainsKey(Key))
            {
                if (Value == null)
                    Cache.TryRemove(Key, out _);
                else
                    Cache[Key] = Value;
            }
            else if (Value != null)
                Cache.TryAdd(Key, Value);
        }
        /// <summary>
        /// Gets the value from the database
        /// </summary>
        /// <param name="Key">The key of the value</param>
        /// <returns>The value of the key</returns>
        public static T Get(ulong Key)
        {
            if (Cache.ContainsKey(Key))
                return Cache[Key];
            return null;
        }
        /// <summary>
        /// Gets the value from the database. If the value is null, a new instance of T will be added and returned.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static T GetOrCreate(ulong Key)
        {
            if (Cache.ContainsKey(Key))
                return Cache[Key];

            var New = new T();
            Set(Key, New);
            return New;
        }
        /// <summary>
        /// Tries to get the value, if the key is not in the database, return a new.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static T GetOrFake(ulong Key)
        {
            if (Cache.ContainsKey(Key))
                return Cache[Key];
            return new T();
        }
    }
}