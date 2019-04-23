using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kurumi.Common
{
    public class ConcurrentList<T>
    {
        public readonly List<T> baseList;
        public readonly object Lock;

        public ConcurrentList()
        { 
            baseList = new List<T>();
            Lock = new object();
        }
        public ConcurrentList(ConcurrentList<T> list)
        { 
            baseList = new List<T>(list.baseList);
            Lock = new object();
        }
        public ConcurrentList(List<T> list)
        {
            baseList = new List<T>(list);
            Lock = new object();
        }

        public T this[int index]
        {
            get
            {
                lock(Lock)
                {
                    return baseList[index];
                }
            }

            set
            {
                lock(Lock)
                {
                    baseList[index] = value;
                }
            }
        }
        public void Add(T item)
        {
            lock(Lock)
            {
                baseList.Add(item);
            }
        }
        public void Remove(T item)
        {
            lock (Lock)
            {
                baseList.Remove(item);
            }
        }
        public int Count => baseList.Count;
        public bool Contains(T item) => baseList.Contains(item);
    }

    public static class ConcurrentListExtensions
    {
        public static T FirstOrDefault<T>(this ConcurrentList<T> list, Func<T, bool> Predicate)
        {
            lock (list.Lock)
            {
                return list.baseList.FirstOrDefault(Predicate);
            }
        }
        public static int Count<T>(this ConcurrentList<T> list, Func<T, bool> Predicate)
        {
            lock (list.Lock)
            {
                return list.baseList.Count(Predicate);
            }
        }
        public static List<T> Where<T>(this ConcurrentList<T> list, Func<T, bool> Predicate)
        {
            lock (list.Lock)
            {
                return list.baseList.Where(Predicate).ToList();
            }
        }
    }
}