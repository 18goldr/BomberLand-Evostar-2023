#nullable enable
using System;
using GP;
using STGP_Sharp.Fitness_and_Gp_Results;
using STGP_Sharp.GpBuildingBlockTypes;

namespace STGP_Sharp.Fitness.Sample_Fitness_Functions
{
    public class EquivalentToOr : IFitnessFunction, IFitnessFunctionSync
    {
        public Type FitnessType { get; } = typeof(FitnessStandard);
        
        public FitnessBase GetFitnessOfIndividual(GpRunner gp, Individual i)
        {
            if (i.genome is not GpBuildingBlock<bool> evaluateBoolean)
                throw new Exception("Genome does not evaluate to boolean");
            
            float fitnessScoreSoFar = 0;
            foreach (var b1 in new [] {true, false})
            {
                foreach (var b2 in new[] { true, false })
                {
                    var gpFieldsWrapper = new GpFieldsWrapper(gp, new PositionalArguments(b1, b2));
                    if ((b1 || b2) == evaluateBoolean.Evaluate(gpFieldsWrapper))
                    {
                        fitnessScoreSoFar++;
                    }
                }
            }
            fitnessScoreSoFar /= 4;
            return new FitnessStandard(fitnessScoreSoFar);
        }


    }
}