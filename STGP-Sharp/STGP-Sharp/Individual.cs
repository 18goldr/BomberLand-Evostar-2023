#nullable enable

using System;
using System.Collections.Generic;
using GP;
using STGP_Sharp.Fitness;
// using STGP_Sharp.FitnessFunctions;
using STGP_Sharp.Utilities.GeneralCSharp;

namespace STGP_Sharp
{
    public class IndividualComparer : IEqualityComparer<Individual>
    {
        public bool Equals(Individual? i1, Individual? i2)
        {
            return i1?.Equals(i2) ?? null == i2;
        }

        public int GetHashCode(Individual? i)
        {
            if (null == i) return 0;

            return GeneralCSharpUtilities.CombineHashCodes(
                new[]
                {
                    new NodeComparer()
                        .GetHashCode(i
                            .genome), // this cannot be i.genome.GetHashCode() because that will not take into account whether node child order matters.
                    i.fitness?.GetHashCode() ?? 0
                }
            );
        }
    }

    public class Individual
    {
        public readonly Node genome;

        public FitnessBase? fitness; // TODO should this ever be null?

        public string guid;

        // TODO should genome ever be null?
        public Individual(Node? genome, FitnessBase? fitness = null)
        {
            this.genome = genome ?? throw new ArgumentNullException(nameof(genome));
            this.fitness = fitness;
            this.guid = Guid.NewGuid().ToString();
        }

        public Individual DeepCopy()
        {
            return new Individual(genome.DeepCopy(), fitness?.DeepCopy());
        }

        public bool Equals(Individual? otherIndividual)
        {
            if (null == otherIndividual) return false;
            return genome.Equals(otherIndividual.genome) &&
                   (fitness?.Equals(otherIndividual.fitness) ?? null == otherIndividual.fitness);
        }
    }
}