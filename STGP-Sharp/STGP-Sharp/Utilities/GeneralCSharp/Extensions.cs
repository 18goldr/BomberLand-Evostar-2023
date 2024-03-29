﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Utilities.GeneralCSharp;
using Formatting = System.Xml.Formatting;
using Random = System.Random;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

#nullable enable

namespace STGP_Sharp.Utilities.GeneralCSharp
{
    public static class Extensions
    {
        
        public static Vector3 Centroid(this IEnumerable<Vector3> vectors)
        {
            var i = 0;
            var sum = new Vector3(new Vector2(0, 0), 0);
            foreach (var v in vectors)
            {
                ++i;
                sum += v;
            }

            if (i == 0) return new Vector3(new Vector2(0, 0), 0);

            return sum / i;
        }

        public static Vector2 Centroid(this IEnumerable<Vector2> vectors)
        {
            var i = 0;
            Vector2 sum = new Vector2(0, 0);
            foreach (var v in vectors)
            {
                ++i;
                sum += v;
            }

            if (i == 0) return new Vector2(0, 0);

            return sum / i;
        }

        
        // public static Vector2 ShiftVector2IntoRange(this Vector2 v, Vector2 min, Vector2 max)
        // {
            // v.X = GeneralCSharpUtilities.ShiftNumberIntoRange(v.X, min.X, max.X);
            // v.Y = GeneralCSharpUtilities.ShiftNumberIntoRange(v.Y, min.Y, max.Y);
            // return v;
        // }

        // public static bool IsNear(this Vector2 v, Vector2 v2, float kmDistance)
        // {
            // var delta = v - v2;
            // return delta.SqrMagnitude() <= kmDistance * kmDistance;
        // }

        // public static bool IsApproximately(this Vector2 a, Vector2 b)
        // {
            // return Mathf.Approximately(a.X, b.X)
                   // && Mathf.Approximately(a.Y, b.Y);
        // }

        
        // public static Vector3 WithY(this Vector3 v, float newY)
        // {
            // return new Vector3(v.X, newY, v.Z);
        // }

        // public static Vector2 NextVector2(this Random random)
        // {
            // return new Vector2(
                // random.NextFloat(),
                // random.NextFloat()
            // );
        // }

        // public static Vector3 NextVector3(this Random random)
        // {
            // return new Vector3(
                // random.NextFloat(),
                // random.NextFloat(),
                // random.NextFloat()
            // );
        // }

        public static float NextFloat(this Random random)
        {
            return (float)random.NextDouble();
        }

        public static float NextFloat(this Random random, float min, float max)
        {
            float zeroToOne = random.NextFloat();
            float result = min + zeroToOne * (max - min);
            return result;
        }
        
        public static IEnumerable<T> GetRandomElements<T>(this IEnumerable<T> list, int elementsCount, Random rand)
        {
            return list.OrderBy(_ => rand.Next()).Take(elementsCount);
        }

        public static T GetRandomEntry<T>(this T[] a, Random rand)
        {
            if (a.Length == 0) throw new Exception("Getting random entry from zero-length array");
            return a[rand.Next(0, a.Length)];
        }

        public static T GetRandomEntryOrValue<T>(this T[] a, Random rand, T t)
        {
            if (a.Length == 0) return t;
            return a[rand.Next(0, a.Length)];
        }


        public static T GetRandomEntry<T>(this List<T> list, Random rand)
        {
            return GetRandomEntry(list.ToArray(), rand);
        }

        public static T GetRandomEntry<T>(this IEnumerable<T> enumerable, Random rand)
        {
            return GetRandomEntry(enumerable.ToArray(), rand);
        }

        public static T GetRandomEntryOrValue<T>(this IEnumerable<T> enumerable, Random rand, T t)
        {
            return GetRandomEntryOrValue(enumerable.ToArray(), rand, t);
        }


        public static bool NextBool(this Random rand)
        {
            return rand.NextDouble() > 0.5;
        }


        //http://www.glennslayden.com/code/linq/handy-extension-methods
        public static int IndexOfMax<TSrc, TArg>(this IEnumerable<TSrc> ie, Converter<TSrc, TArg> fn)
            where TArg : IComparable<TArg>
        {
            using var e = ie.GetEnumerator();
            if (!e.MoveNext())
                return -1;

            var maxIx = 0;
            var t = e.Current;
            if (!e.MoveNext())
                return maxIx;

            var maxVal = fn(t);
            var i = 1;
            do
            {
                TArg tx;
                if ((tx = fn(e.Current)).CompareTo(maxVal) > 0)
                {
                    maxVal = tx;
                    maxIx = i;
                }

                i++;
            } while (e.MoveNext());

            return maxIx;
        }

        //http://www.glennslayden.com/code/linq/handy-extension-methods
        /// <summary>
        ///     Returns the index of the element which is smallest
        /// </summary>
        /// <typeparam name="TSrc"></typeparam>
        /// <typeparam name="TArg"></typeparam>
        /// <param name="ie"></param>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static int IndexOfMin<TSrc, TArg>(this IEnumerable<TSrc> ie, Converter<TSrc, TArg> fn)
            where TArg : IComparable<TArg>
        {
            using var e = ie.GetEnumerator();
            if (!e.MoveNext())
                return -1;

            var minIx = 0;
            var t = e.Current;
            if (!e.MoveNext())
                return minIx;

            var minVal = fn(t);
            var i = 1;
            do
            {
                TArg tx;
                if ((tx = fn(e.Current)).CompareTo(minVal) < 0)
                {
                    minVal = tx;
                    minIx = i;
                }

                i++;
            } while (e.MoveNext());

            return minIx;
        }

        public static int IndexOf<TSrc>(this IEnumerable<TSrc> ie, TSrc matchMe)
            where TSrc : IEquatable<TSrc>
        {
            var index = 0;
            foreach (var guy in ie)
            {
                if (matchMe.Equals(guy)) return index;
                ++index;
            }

            return -1;
        }

        public static int IndexOfReference<TSrc>(this IEnumerable<TSrc> ie, TSrc matchMe)
            where TSrc : class
        {
            var index = 0;
            foreach (var guy in ie)
            {
                if (matchMe == guy) return index;
                ++index;
            }

            return -1;
        }


        //http://www.glennslayden.com/code/linq/handy-extension-methods
        public static TSrc ArgMax<TSrc, TArg>(this IEnumerable<TSrc> ie, Converter<TSrc, TArg> fn)
            where TArg : IComparable<TArg>
        {
            using var e = ie.GetEnumerator();
            if (!e.MoveNext())
                throw new InvalidOperationException("Sequence has no elements.");

            var t = e.Current;
            if (!e.MoveNext())
                return t;

            var maxVal = fn(t);
            do
            {
                TSrc tTry;
                TArg v;
                if ((v = fn(tTry = e.Current)).CompareTo(maxVal) > 0)
                {
                    t = tTry;
                    maxVal = v;
                }
            } while (e.MoveNext());

            return t;
        }


        //http://www.glennslayden.com/code/linq/handy-extension-methods
        public static TSrc? ArgMaxOrDefault<TSrc, TArg>(this IEnumerable<TSrc> ie, Converter<TSrc, TArg> fn)
            where TSrc : class
            where TArg : IComparable<TArg>
        {
            using var e = ie.GetEnumerator();
            if (!e.MoveNext())
                return default;

            var t = e.Current;
            if (!e.MoveNext())
                return t;

            var maxVal = fn(t);
            do
            {
                TSrc? tTry;
                TArg v;
                if ((v = fn(tTry = e.Current)).CompareTo(maxVal) > 0)
                {
                    t = tTry;
                    maxVal = v;
                }
            } while (e.MoveNext());

            return t;
        }

        //http://www.glennslayden.com/code/linq/handy-extension-methods
        public static TSrc ArgMin<TSrc, TArg>(this IEnumerable<TSrc> ie, Converter<TSrc, TArg> fn)
            where TArg : IComparable<TArg>
        {
            using var e = ie.GetEnumerator();
            if (!e.MoveNext())
                throw new InvalidOperationException("Sequence has no elements.");

            var t = e.Current;
            if (!e.MoveNext())
                return t;

            var minVal = fn(t);
            do
            {
                TSrc tTry;
                TArg v;
                if ((v = fn(tTry = e.Current)).CompareTo(minVal) < 0)
                {
                    t = tTry;
                    minVal = v;
                }
            } while (e.MoveNext());

            return t;
        }

        //http://www.glennslayden.com/code/linq/handy-extension-methods
        public static TSrc? ArgMinOrDefault<TSrc, TArg>(this IEnumerable<TSrc> ie, Converter<TSrc, TArg> fn)
            where TSrc : class
            where TArg : IComparable<TArg>
        {
            using var e = ie.GetEnumerator();
            if (!e.MoveNext())
                return default;

            var t = e.Current;
            if (!e.MoveNext())
                return t;

            var minVal = fn(t);
            do
            {
                TSrc? tTry;
                TArg v;
                if ((v = fn(tTry = e.Current)).CompareTo(minVal) < 0)
                {
                    t = tTry;
                    minVal = v;
                }
            } while (e.MoveNext());

            return t;
        }

        public static IEnumerable<TChildType> ConditionalCast<TParentType, TChildType>(
            this IEnumerable<TParentType> parents)
            where TChildType : class, TParentType
        {
            foreach (var parent in parents)
                if (parent is TChildType child)
                    yield return child;
        }

        public static T? MaybeGet<T>(this List<T?> list, int index) where T : class
        {
            return !list.ContainsIndex(index) ? default : list[index];
        }

        public static bool ContainsIndex<T>(this List<T> list, int index)
        {
            if (index < 0) return false;
            if (index >= list.Count) return false;
            return true;
        }

        public static List<List<T>> ToNestedList<T>(this IEnumerable<IEnumerable<T>> nestedEnumerable)
        {
            return nestedEnumerable.Select(e => e.ToList()).ToList();
        }

        public static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new[] { t });

            var array = list as T[] ?? list.ToArray();
            return GetPermutations(array, length - 1)
                .SelectMany(t => array.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new[] { t2 }));
        }
        
        public static float Magnitude(this Vector2 v)
        {
            
            return (float)Math.Sqrt(v.SqrMagnitude());
        }
        
        public static float SqrMagnitude(this Vector2 v)
        {
            return (float)(Math.Pow(v.X, 2) + Math.Pow(v.Y, 2));
        }

        public static List<T> Flatten<T>(this IEnumerable<IEnumerable<T>> listOfLists)
        {
            return listOfLists.SelectMany(x => x).ToList();
        }
    }
}