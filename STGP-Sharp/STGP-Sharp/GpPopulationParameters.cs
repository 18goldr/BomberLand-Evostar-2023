using System.Collections.Generic;
using System.Diagnostics;
using Vector2 = System.Numerics.Vector2;

#nullable enable

namespace STGP_Sharp
{
    // TODO remove a bunch of these and make this class extendable.
    public class GpPopulationParameters
    {
        public const int DEFAULT_MAX_DEPTH = 5;
        public const int DEFAULT_POPULATION_SIZE = 100;
        public const int DEFAULT_NUMBER_GENERATIONS = 20;
        public const double DEFAULT_CROSSOVER_PROBABILITY = 0.8;
        public const double DEFAULT_MUTATION_PROBABILITY = 0.2;
        public const int DEFAULT_TOURNAMENT_SIZE = 3;
        public const int DEFAULT_ELITE_SIZE = 2;
        public const int DEFAULT_SEQ_MAX_SIZE = 2;
        public const bool DEFAULT_RAMP = true;
        public const float DEFAULT_FLOAT_MIN = -0.5f;
        public const float DEFAULT_FLOAT_MAX = 0.5f;
        public const int DEFAULT_NUMBER_EXECUTIONS_FOR_MULTIPLE_EXECUTION_FITNESS_FUNCTION = 10;
        public const int DEFAULT_NUMBER_DISCRETE_FLOAT_STEPS = 4;
        public static readonly Vector2 DEFAULT_VECTOR2_FLOAT_MIN_VALUES = new Vector2(-0.5f, -0.5f);
        public static readonly Vector2 DEFAULT_VECTOR2_FLOAT_MAX_VALUES = new Vector2(0.5f, 0.5f);
        public readonly double crossoverProbability;
        public readonly int eliteSize;
        public readonly float floatMax;
        public readonly float floatMin;
        public readonly int maxDepth;

        public readonly double mutationProbability;

        public readonly int numberDiscreteFloatSteps;

        // ReSharper disable once NotAccessedField.Global
        public readonly int numberExecutionsForMultipleExecutionFitnessFunction;
        public readonly int numberGenerations;

        public readonly PopulationInitializationMethod populationInitializationMethod;

        public readonly int populationSize;
        public readonly ProbabilityDistribution probabilityDistribution;
        public readonly bool ramp;

        // ReSharper disable once NotAccessedField.Global
        public readonly int sequenceMaxNumberOfChildren;
        public readonly int tournamentSize;
        public readonly Vector2 vector2FloatMaxValues;

        public readonly Vector2 vector2FloatMinValues;

        public GpPopulationParameters(
            int eliteSize = DEFAULT_ELITE_SIZE,
            int tournamentSize = DEFAULT_TOURNAMENT_SIZE,
            int populationSize = DEFAULT_POPULATION_SIZE,
            int maxDepth = DEFAULT_MAX_DEPTH,
            int numberGenerations = DEFAULT_NUMBER_GENERATIONS,
            double crossoverProbability = DEFAULT_CROSSOVER_PROBABILITY,
            double mutationProbability = DEFAULT_MUTATION_PROBABILITY,
            int sequenceMaxNumberOfChildren = DEFAULT_SEQ_MAX_SIZE,
            bool ramp = DEFAULT_RAMP,
            float floatMin = DEFAULT_FLOAT_MIN,
            float floatMax = DEFAULT_FLOAT_MAX,
            int numberDiscreteFloatSteps = DEFAULT_NUMBER_DISCRETE_FLOAT_STEPS,
            int numberExecutionsForMultipleExecutionFitnessFunction =
                DEFAULT_NUMBER_EXECUTIONS_FOR_MULTIPLE_EXECUTION_FITNESS_FUNCTION,
            Vector2? vector2FloatMinValues = null,
            Vector2? vector2FloatMaxValues = null,
            ProbabilityDistribution? probabilityDistribution = null,
            PopulationInitializationMethod? populationInitializationMethod = null)
        {
            this.eliteSize = eliteSize;
            this.tournamentSize = tournamentSize;
            this.populationSize = populationSize;
            this.maxDepth = maxDepth;
            this.numberGenerations = numberGenerations;
            this.crossoverProbability = crossoverProbability;
            this.mutationProbability = mutationProbability;
            this.sequenceMaxNumberOfChildren = sequenceMaxNumberOfChildren;
            this.ramp = ramp;
            this.floatMin = floatMin;
            this.floatMax = floatMax;
            this.numberDiscreteFloatSteps = numberDiscreteFloatSteps;

            this.vector2FloatMinValues = vector2FloatMinValues ?? DEFAULT_VECTOR2_FLOAT_MIN_VALUES;
            this.vector2FloatMaxValues = vector2FloatMaxValues ?? DEFAULT_VECTOR2_FLOAT_MAX_VALUES;
            if (vector2FloatMaxValues == vector2FloatMinValues && vector2FloatMaxValues == new Vector2(0, 0))
            {
                // Almost certainly deserialized from old Vector3 data format
                this.vector2FloatMinValues = DEFAULT_VECTOR2_FLOAT_MIN_VALUES;
                this.vector2FloatMaxValues = DEFAULT_VECTOR2_FLOAT_MAX_VALUES;
            }

            this.numberExecutionsForMultipleExecutionFitnessFunction =
                numberExecutionsForMultipleExecutionFitnessFunction;

            this.probabilityDistribution = probabilityDistribution ??
                                           new ProbabilityDistribution(new List<TypeProbability>
                                               { new TypeProbability() });
            this.populationInitializationMethod =
                populationInitializationMethod ?? new RampedPopulationInitialization(this);
           
            this.AssertInputsAreValid();
        }

        private void AssertInputsAreValid()
        {
            Debug.Assert(this.populationSize > 0);
            Debug.Assert(this.eliteSize >= this.populationSize);
            Debug.Assert(this.tournamentSize >= this.populationSize);
            Debug.Assert(this.maxDepth > 0);
            Debug.Assert(this.numberExecutionsForMultipleExecutionFitnessFunction > 0);
            Debug.Assert(this.sequenceMaxNumberOfChildren > 0);
            Debug.Assert(this.crossoverProbability is >= 0 and <= 1);
            Debug.Assert(this.mutationProbability is >= 0 and <= 1);
            Debug.Assert(this.numberGenerations >= 0);
            Debug.Assert(this.numberDiscreteFloatSteps >= 1);
        }
    }
}