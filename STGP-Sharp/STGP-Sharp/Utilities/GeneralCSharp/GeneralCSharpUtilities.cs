using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities.GeneralCSharp;
using Random = System.Random;
using Vector2 = System.Numerics.Vector2;


namespace STGP_Sharp.Utilities.GeneralCSharp
{
    public static class GeneralCSharpUtilities
    {
        public static string CreateAndGetDirectoryForFileIfNotExist(string fullFilePath)
        {
            var directoryContainingFile =
                Path.GetDirectoryName(fullFilePath) ??
                throw new Exception(
                    $"Invalid path for file: {fullFilePath}");

            if (!Directory.Exists(directoryContainingFile))
            {
                Directory.CreateDirectory(directoryContainingFile);
            }

            return directoryContainingFile;
        }
        
        public static float PythagoreanTheorem(float a, float b)
        {
            return (float)Math.Sqrt(a * a + b * b);
        }

        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            // Switched to new syntax which allows for simpler swapping
            (lhs, rhs) = (rhs, lhs);
        }

        public static string GetRelativePath(string file)
        {
            string currentDirectory = Directory.GetCurrentDirectory() ??
                                      throw new Exception("Somehow the current directory is null.");

            return Path.Combine(currentDirectory, file);
        }

        public static T GetRandomElementFromDistribution<T>(Dictionary<T, double> probabilities, Random rand)
        {
            double pSum = probabilities.Sum(t => t.Value);
            if (pSum == 0) throw new Exception("Sum of the the probability distribution must be greater than zero.");
            double r = rand.NextDouble() * pSum;
            double sum = 0;

            foreach (var kvp in probabilities)
                if (r < (sum += kvp.Value))
                    return kvp.Key;

            throw new Exception($"Somehow no random type was chosen with sum = {sum}.");
        }

        public static string Indent(int count)
        {
            return "".PadLeft(count);
        }

        // TODO rename to GetHashCode
        public static int CombineHashCodes(IEnumerable<int> hashCodes)
        {
            // System.Web.Util.HashCodeCombiner.CombineHashCodes(System.Int32, System.Int32): http://referencesource.microsoft.com/#System.Web/Util/HashCodeCombiner.cs,21fb74ad8bb43f6b
            // System.Array.CombineHashCodes(System.Int32, System.Int32): http://referencesource.microsoft.com/#mscorlib/system/array.cs,87d117c8cc772cca
            var hash = 5381;

            foreach (int hashCode in hashCodes)
                hash = ((hash << 5) + hash) ^ hashCode;

            return hash;
        }

        public static float SecondsElapsedSince(DateTime start)
        {
            return (float)(DateTime.Now - start).TotalSeconds;
        }
        
        public static float SecondsElapsed(DateTime start, DateTime end)
        {
            return (float)(end - start).TotalSeconds;
        }

    }
}