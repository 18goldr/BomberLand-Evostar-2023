#nullable enable

using STGP_Sharp.Fitness;
using STGP_Sharp.Utilities.GeneralCSharp;

namespace BomberLandGp
{
    public class FitnessLexicographic : FitnessBase
    {
        public enum MinOrMax
        {
            Min,
            Max
        }
        
        // ReSharper disable once UnusedMember.Global
        public new static Type GpResultsStatsType { get; } = typeof(GpResultsStatsLexicographic);
        
        public readonly List<double> lexicographicFitness;

        public readonly List<MinOrMax> minMaxLexicographicFitnessMapping;

        public FitnessLexicographic(List<double> lexicographicFitness, List<MinOrMax> minMaxLexicographicFitnessMapping)
        {
            this.lexicographicFitness = lexicographicFitness;
            this.minMaxLexicographicFitnessMapping = minMaxLexicographicFitnessMapping;
        }
        
        // Json Constructor 
        // public FitnessLexicographic() { }
        
        public static void ThrowExceptionIfInvalidFitness(FitnessBase? f, out FitnessLexicographic fs)
        {
            ThrowExceptionIfInvalidFitnessForComparison(f, out fs);
        }
        
        public override string ToString()
        {
            return $"Lexicographic Fitness Values: {string.Join(", ", this.lexicographicFitness)},\n" +
                   $"Min-Max Mapping: {string.Join(", ", this.minMaxLexicographicFitnessMapping)}";
        }
        
        public override bool LessThan(FitnessBase f)
        {
            return this.CompareTo(f) < 0;
        }

        public override bool GreaterThan(FitnessBase f)
        {
            return this.CompareTo(f) > 0;
        }

        public override bool Equals(FitnessBase? other)
        {
            if (null == other) return false;
            ThrowExceptionIfInvalidFitness(other, out var otherLexicographic);
            return !this.lexicographicFitness
                .Where((t, i) => 
                    Math.Abs(t - otherLexicographic.lexicographicFitness[i]) > 0.00001)
                .Any();
        }

        public override int CompareTo(FitnessBase? other)
        {
            ThrowExceptionIfInvalidFitness(other, out var otherLexicographic);
            for (int i = 0; i < this.lexicographicFitness.Count; i++)
            {
                var resultForDimension = this.lexicographicFitness[i].CompareTo(otherLexicographic.lexicographicFitness[i]);
                if (0 == resultForDimension) continue;
                
                var minOrMax = this.minMaxLexicographicFitnessMapping[i];

                return minOrMax switch
                {
                    MinOrMax.Max => resultForDimension > 0 ? 1 : -1,
                    MinOrMax.Min => resultForDimension < 0 ? 1 : -1,
                    _ => throw new ArgumentOutOfRangeException(nameof(this.minMaxLexicographicFitnessMapping), 
                        "Invalid mapping for lexicographic fitness dimension")
                };
            }

            return 0;
        }

        public override FitnessBase DeepCopy()
        {
            return new FitnessLexicographic(this.lexicographicFitness, this.minMaxLexicographicFitnessMapping);
        }

        public override int GetHashCode()
        {
            return GeneralCSharpUtilities.CombineHashCodes(new[]
                { this.lexicographicFitness.GetHashCode() });
        }
    }
}