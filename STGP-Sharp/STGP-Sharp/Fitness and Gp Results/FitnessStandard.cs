#nullable enable

using System;
using STGP_Sharp.Utilities.GeneralCSharp;
using STGP_Sharp.Utilities.GP;
using STGP_Sharp.Utilities.GP.Fitness_Stats;

namespace STGP_Sharp.Fitness
{

    [Serializable]
    public class FitnessStandard : FitnessBase
    {
        // ReSharper disable once UnusedMember.Global
        public new static Type GpResultsStatsType { get; } = typeof(GpResultsStatsStandard);

        public readonly double fitnessScore;

        public FitnessStandard(double fitnessScore)
        {
            this.fitnessScore = fitnessScore;
        }

        public static void ThrowExceptionIfInvalidFitness(FitnessBase? f, out FitnessStandard fs)
        {
            ThrowExceptionIfInvalidFitnessForComparison(f, out fs);
        }

        public override int CompareTo(FitnessBase? other)
        {
            if (null == other) return this.fitnessScore.CompareTo(null);
            ThrowExceptionIfInvalidFitness(other, out var otherStandard);
            return this.fitnessScore.CompareTo(otherStandard.fitnessScore);
        }
        

        public override int GetHashCode()
        {
            return GeneralCSharpUtilities.CombineHashCodes(new[]
                { fitnessScore.GetHashCode()});
        }

        public override FitnessBase DeepCopy()
        {
            return new FitnessStandard(fitnessScore);
        }

        public FitnessBase Add(FitnessBase f)
        {
            ThrowExceptionIfInvalidFitness(f, out var fitnessStandard);
            return new FitnessStandard(fitnessScore + fitnessStandard.fitnessScore);
        }

        public FitnessBase Divide(int divisor)
        {
            return (divisor == 0
                ? this.DeepCopy()
                : new FitnessStandard(fitnessScore / divisor));
        }

        public override bool LessThan(FitnessBase f)
        {
            ThrowExceptionIfInvalidFitness(f, out var fitnessStandard);
            return this.fitnessScore < fitnessStandard.fitnessScore;
        }

        public override bool GreaterThan(FitnessBase f)
        {
            ThrowExceptionIfInvalidFitness(f, out var fitnessStandard);
            return this.fitnessScore > fitnessStandard.fitnessScore;
        }

        public override bool Equals(FitnessBase? otherFitness)
        {
            if (null == otherFitness) return false;
            ThrowExceptionIfInvalidFitness(otherFitness, out var fitnessStandard);
            return Math.Abs(fitnessScore - fitnessStandard.fitnessScore) < 0.00001;
        }

        public override string ToString()
        {
            return this.fitnessScore.ToString();
        }
    }
}