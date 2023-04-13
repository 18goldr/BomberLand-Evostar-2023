#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace STGP_Sharp.Utilities.GeneralCSharp
{
    public static class GeneralExtensions
    {
        public static double SecondsSince(this DateTime then)
        {
            var since = DateTime.UtcNow - then;
            return since.TotalSeconds;
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> items) where T : class
        {
            return items.Where(t => null != t)!;
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> items) where T : struct
        {
            return items.Where(i => i.HasValue).Select(i => i!.Value);
        }

        public static IEnumerable<T> ToSingleElementEnumerable<T>(this T t)
        {
            yield return t;
        }

        public static T[] ToSingleElementArray<T>(this T t)
        {
            return new[] { t };
        }
    }
}