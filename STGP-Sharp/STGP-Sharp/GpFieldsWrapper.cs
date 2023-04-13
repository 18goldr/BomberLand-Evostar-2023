#nullable enable

using System;
using GP;
using GP.GpWorldState;
using GP.Interfaces;

namespace STGP_Sharp
{
    /// <summary>
    ///     Wrapper to hold relevant GP fields for genome and fitness evaluation.
    /// </summary>
    public class GpFieldsWrapper
    {
        public readonly IAlgorithmWrapperToBeUsedWithGp? otherAlgorithm;
        public readonly GpPopulationParameters populationParameters;
        public readonly Random rand;
        public readonly IGpWorldStateEvaluationParametersWrapper? evaluationParametersWrapper;  
        public readonly TimeoutInfo timeoutInfo;
        public readonly bool verbose;
        public readonly IGpWorldStateWrapper? worldState;
        public readonly PositionalArguments? positionalArguments;

        public GpFieldsWrapper(STGP_Sharp.GpRunner gp) : this(gp, gp?.worldState, gp?.positionalArguments)
        {
        }

        public GpFieldsWrapper(GpRunner gp, 
            PositionalArguments positionalArguments) : this(gp, gp?.worldState, positionalArguments)
        {
            
        }
        
        public GpFieldsWrapper(GpRunner gp, 
            IGpWorldStateWrapper gpWorldState) : this(gp, gpWorldState, gp.positionalArguments)
        {
            
        }

        public GpFieldsWrapper(STGP_Sharp.GpRunner gp, 
            IGpWorldStateWrapper? worldState, 
            PositionalArguments? positionalArguments)
        {
            this.rand = gp.rand;
            this.timeoutInfo = gp.timeoutInfo;
            this.worldState = worldState?.DeepCopy();
            this.populationParameters = gp.populationParameters;
            this.otherAlgorithm = gp.otherAlgorithm;
            this.evaluationParametersWrapper = gp.evaluationParameters;
            this.verbose = gp.verbose;
            this.positionalArguments = positionalArguments;
        }
    }
}