#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using STGP_Sharp.Fitness;
using STGP_Sharp.Fitness.Fitness_Stats;
using STGP_Sharp.Utilities.GeneralCSharp;

namespace STGP_Sharp.Utilities.GP.Fitness_Stats
{

    public class GpResultsStatsStandard : GpResultsStatsBase
    {
        public GpResultsStatsStandard(IEnumerable<FitnessStandard>? fitnessValues) : base(fitnessValues)
        {
        }
        
        
        // It is necessary to define a more general constructor that dynamically checks if the
        // input fitness values are of the correct type.
        // Unfortunately, this is a consequence of C#8 not supporting contra/covariant types.
        // I would use a more recent version of C#, but I want this library to be compatible with Unity.
        
        public GpResultsStatsStandard(IEnumerable<FitnessBase> fitnessValues) : base(
            fitnessValues?.Cast<FitnessStandard>() 
            ?? throw new Exception($"The fitness values passed in are not of type {typeof(GpResultsStatsStandard)}"))
        {
        }

        public override DetailedSummary GetDetailedSummary()
        {
            return new DetailedSummaryStandard(this.fitnessValues?.Cast<FitnessStandard>() ??
                throw new NullReferenceException(nameof(this.fitnessValues)));
        }

        public class DetailedSummaryStandard : DetailedSummary
        {
            public SimpleStats.Summary fitnessScoreSummary;

            public DetailedSummaryStandard(IEnumerable<FitnessStandard> fitnessValues)
            {
                this.fitnessScoreSummary = new SimpleStats(
                        fitnessValues.Select(f => f.fitnessScore))
                    .GetSummary();
            }

            public override string ToString()
            {
                return $"Total Fitness Summary: {fitnessScoreSummary.ToString()}";
            }
        }
    }
}