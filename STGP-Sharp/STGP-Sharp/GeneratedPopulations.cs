#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using GP;
using STGP_Sharp.Fitness.Fitness_Stats;
using STGP_Sharp.Utilities.GeneralCSharp;
using STGP_Sharp.Utilities.GP;
using Utilities.GeneralCSharp;

// TODO move to GpRunnerHelper
namespace STGP_Sharp
{
    public class Generation // TODO use in actual gp code
    {
        public readonly string generationNumberString;

        public readonly List<Individual> population;

        public Generation(List<Individual> sortedPopulation, int generationNumber)
        {
            population = sortedPopulation;
            generationNumberString = $"Generation {generationNumber}";
        }

        public Individual? Best => population.Any() ? population[0] : null;
        public Individual? Worst => population.Any() ? population[population.Count - 1] : null;
        public Individual? Median => population.Any() ? population[population.Count / 2] : null;
    }

    public class GeneratedPopulations
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly Individual? bestEver;

        // ReSharper disable once MemberCanBePrivate.Global
        public readonly DateTime endTime;

        // ReSharper disable once MemberCanBePrivate.Global
        public readonly List<Generation> generations;

        public readonly List<List<Individual>> generationsAsNestedList;

        // ReSharper disable once MemberCanBePrivate.Global
        public readonly float secondsElapsed;

        // ReSharper disable once MemberCanBePrivate.Global
        public readonly DateTime startTime;

        public readonly VerboseInfo verboseInfo;

        // ReSharper disable once MemberCanBePrivate.Global
        public GpResultsStatsBase.DetailedSummary fitnessSummary;

        public GeneratedPopulations(
            List<List<Individual>> populations,
            GpResultsStatsBase.DetailedSummary fitnessSummary,
            DateTime startTime,
            DateTime endTime,
            Individual? bestEver,
            VerboseInfo verboseInfo)
        {
            generationsAsNestedList = populations;
            generations = new List<Generation>();
            var populationsAsList = generationsAsNestedList.ToList();
            for (var i = 0; i < populationsAsList.Count; i++)
                generations.Add(new Generation(populationsAsList[i].SortedByFitness(), i));

            this.fitnessSummary = fitnessSummary;
            this.startTime = startTime;
            this.endTime = endTime;
            secondsElapsed = GeneralCSharpUtilities.SecondsElapsed(startTime, endTime);
            this.bestEver = bestEver;
            this.verboseInfo = verboseInfo;
        }

        public Individual? GetBestEver()
        {
            return GetBestEver(generationsAsNestedList);
        }

        private static Individual? GetBestEver(IEnumerable<IEnumerable<Individual>> populations)
        {
            return populations.Last().SortedByFitness().FirstOrDefault();
        }

        public GeneratedPopulations DeepCopy()
        {
            var newPopulations = generations.Select(population =>
                    population.population.Select(individual =>
                        individual.DeepCopy()
                    ).ToList())
                .ToList();

            return new GeneratedPopulations(newPopulations, fitnessSummary, startTime, endTime, bestEver, verboseInfo);
        }
    }
}