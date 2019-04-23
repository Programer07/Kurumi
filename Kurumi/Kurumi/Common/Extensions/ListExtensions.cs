using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Common.Extensions
{
    public static class ListExtensions
    {
        public static int FindIndexN<T>(this List<T> list, Predicate<T> Match, int n, int StartIndex = 0)
        {
            int i = list.FindIndex(StartIndex, Match);
            if (i == -1)
                return StartIndex - 1;

            n--;
            if (n == 0)
            {
                return i;
            }
            return list.FindIndexN(Match, n, i + 1);
        }
    }
}