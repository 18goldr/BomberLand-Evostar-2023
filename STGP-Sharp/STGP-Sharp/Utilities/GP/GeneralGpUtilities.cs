#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace STGP_Sharp.Utilities.GP
{
    public static class GpUtility
    {
        private static readonly Dictionary<Type, string> BetterNamesDictionary = new Dictionary<Type, string>
        {
            { typeof(float), "Float" },
            { typeof(int), "Integer" }
        };

        // TODO write unit test
        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        public static List<Individual> SortedByFitness(this IEnumerable<Individual> population)
        {
            return population.ToList().SortedByFitness();
        }

        public static List<Individual> SortedByFitness(this List<Individual> population)
        {
            return population.OrderByDescending(i => i.fitness).ToList(); // TODO make this sort in place
        }

        public static string GetBetterClassName(Type t)
        {
            return BetterNamesDictionary.TryGetValue(t, out var betterName) ? betterName : t.Name;
        }
    }
}