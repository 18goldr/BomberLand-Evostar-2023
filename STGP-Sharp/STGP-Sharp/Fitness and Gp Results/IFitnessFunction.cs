#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GP.Interfaces;
using STGP_Sharp.Fitness;

// TODO can you force subclass of IFitnessFunction to implement either IFitnessFunctionSync xor IFitnessFunctionAsync? Same with the other two interfaces
// TODO can we just create interfaces for sync, async, coevolution, instead of needing 4 different ones?
namespace STGP_Sharp.Fitness_and_Gp_Results
{
    public interface IFitnessFunction
    {
        // TODO would ideally have this be a template variable or static,
        // but that makes most every other class more complicated, as far as a I can see.
        // TODO somehow verify this in the GetFitnessOfIndividual function
        public Type FitnessType { get; } 
    }

    public interface IFitnessFunctionSync
    {
        public FitnessBase GetFitnessOfIndividual(GpRunner gp, Individual i);
    }

    public interface IFitnessFunctionAsync
    {
        public Task<FitnessBase> GetFitnessOfIndividualAsync(GpRunner gp, Individual i);
    }

    public interface IFitnessFunctionWhichCreatesSimStatesToEvaluate
    {
        public IEnumerable<IGpWorldStateWrapper> CreateEvaluationWorldStates();
    }

    public interface IFitnessFunctionWhichUsesASuppliedSimWorldState
    {
    }
    
    public interface IFitnessFunctionCoevolutionSync
    {
        public List<FitnessBase> GetFitnessOfPopulationUsingCoevolution(GpRunner gp,
            List<Individual> population);
    }
    
    public interface IFitnessFunctionCoevolutionAsync
    {
        public Task<List<FitnessBase>> GetFitnessOfPopulationUsingCoevolutionAsync(GpRunner gp,
            List<Individual> population);
    }
    

}