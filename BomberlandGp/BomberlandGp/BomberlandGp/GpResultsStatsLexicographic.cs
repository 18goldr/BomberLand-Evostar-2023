#nullable enable
using System.Diagnostics;
using STGP_Sharp;
using STGP_Sharp.Fitness.Fitness_Stats;
using STGP_Sharp.Utilities.GeneralCSharp;

namespace BomberLandGp
{
    public class GpResultsStatsLexicographic : GpResultsStatsBase
    {
        public GpResultsStatsLexicographic(IEnumerable<Individual> individuals) : base(individuals)
        {
            if (null == fitnessValues?.Cast<FitnessLexicographic>())
                throw new Exception($"The fitness values passed in are not of type {typeof(FitnessLexicographic)}");
        }

        public override DetailedSummary GetDetailedSummary()
        {
            return new DetailedSummaryLexicographic(this.individuals ?? 
                throw new NullReferenceException(nameof(this.individuals)));
        }

        [Serializable]
        public class DetailedSummaryLexicographic : DetailedSummary
        {
            public List<SimpleStats.Summary> summariesForEachDimensionOfLexicographicFitness;
            public List<Dictionary<double, int>> frequenciesForEachDimensionOfLexicographicFitness; // TODO add this to STGP-Sharp
            public SimpleStats.Summary genomeSizeSummaries;
            public SimpleStats.Summary genomeHeightSummaries;

            // JSon constructor
            public DetailedSummaryLexicographic()
            {
                this.summariesForEachDimensionOfLexicographicFitness = new List<SimpleStats.Summary>();
                this.frequenciesForEachDimensionOfLexicographicFitness = new List<Dictionary<double, int>>();
                this.genomeHeightSummaries = new SimpleStats.Summary();
                this.genomeSizeSummaries = new SimpleStats.Summary();
            }

            public DetailedSummaryLexicographic(IEnumerable<Individual> individuals)
            {
                this.summariesForEachDimensionOfLexicographicFitness = new List<SimpleStats.Summary>();
                this.frequenciesForEachDimensionOfLexicographicFitness = new List<Dictionary<double, int>>();

                var individualsAsArray = individuals as Individual[] ?? individuals.ToArray();
                var lexicographicFitnessValues = individualsAsArray
                    .Select(i => i.fitness)
                    .Cast<FitnessLexicographic>()
                    .Select(i => i.lexicographicFitness).ToList();

                var dimensionCount = lexicographicFitnessValues.First().Count;

                for (int i = 0; i < dimensionCount; i++)
                {
                    (SimpleStats.Summary summary, Dictionary<double, int> frequencies) = 
                        GetSummaryAndFrequencyPerDimension(lexicographicFitnessValues, i);
                    this.summariesForEachDimensionOfLexicographicFitness.Add(summary);
                    this.frequenciesForEachDimensionOfLexicographicFitness.Add(frequencies);
                }

                var genomeSizes = individualsAsArray
                    .Select(i => (double)i.genome.GetSize());
                this.genomeSizeSummaries = new SimpleStats(genomeSizes).GetSummary();

                var genomeHeights = individualsAsArray
                    .Select(i => (double)i.genome.GetHeight());
                this.genomeHeightSummaries = new SimpleStats(genomeHeights).GetSummary();
                
                
            }

            public static (SimpleStats.Summary, Dictionary<double, int>) GetSummaryAndFrequencyPerDimension(List<List<double>> fitnessList, int dimension)
            {
                var valuesForDimensionWithInfinity = fitnessList
                    .Select(sublist => sublist[dimension]).ToArray();
                var valuesForDimensionWithoutInfinity = valuesForDimensionWithInfinity
                    .Where(f => !double.IsInfinity(f))
                    .ToArray();

                var frequencies = Utilities.GetFrequencyTable(valuesForDimensionWithInfinity);
                return (new SimpleStats(valuesForDimensionWithoutInfinity).GetSummary(), frequencies);
            }

            public override string ToString()
            {
                var s = $"Summary for each dimension of lexicographic fitness:\n";

                for (var i = 0; i < this.summariesForEachDimensionOfLexicographicFitness.Count; i++)
                {
                    var summary = this.summariesForEachDimensionOfLexicographicFitness[i];
                    s += $"{GeneralCSharpUtilities.Indent(2)}Dimension {i}:\n" +
                        $"{summary.ToString(indent: 4)}\n";
                }

                // s += $"Number of infinities for each dimension: {string.Join(", ", this.numInfinityPerDimension)}\n\n";
                s += "Frequencies for each dimension:\n";
                for (var i = 0; i < this.frequenciesForEachDimensionOfLexicographicFitness.Count; i++)
                {
                    Dictionary<double, int> f = this.frequenciesForEachDimensionOfLexicographicFitness[i];
                    s += $"  Dimension {i}:\n    {string.Join("\n    ", f)}\n";
                }

                s += $"\nSummary for genome sizes:\n"
                     + $"{this.genomeSizeSummaries.ToString(indent: 2)}\n\n"
                     + "Summary for genome heights:\n"
                     + $"{this.genomeHeightSummaries.ToString(indent: 2)}";

                return s;
            }
        }
    }
}
