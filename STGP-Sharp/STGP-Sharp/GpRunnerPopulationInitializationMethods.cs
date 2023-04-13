#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
// using Cysharp.Threading.Tasks;
using GP;
using STGP_Sharp.Utilities.GeneralCSharp;

namespace STGP_Sharp
{
    public abstract class PopulationInitializationMethod
    {
        public int maxDepth;

        // Purely for use with Odin when defining a Node
        // TODO do these need to be serialized?
        public ProbabilityDistribution? probabilityDistribution;

        // TODO remove this, it's bad style. This creates a cyclic coupling with GpPopulationParameters.
        protected PopulationInitializationMethod(GpPopulationParameters populationParameters) :
            this(populationParameters.probabilityDistribution, populationParameters.maxDepth)
        {
        }

        protected PopulationInitializationMethod(ProbabilityDistribution? probabilityDistribution, int maxDepth)
        {
            this.probabilityDistribution = probabilityDistribution;
            this.maxDepth = maxDepth;
        }

        public virtual Task<List<Individual>> GetPopulation<T>(STGP_Sharp.GpRunner gp, TimeoutInfo timeoutInfo)
        {
            return Task.FromResult(new List<Individual>());
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        public static List<Type>? GetPopulationInitializationMethodTypes()
        {
            var types = GpReflectionCache.GetAllSubTypes(typeof(PopulationInitializationMethod)).ToList();
            return !types.Any() ? null : types;
        }

        
        protected bool GenomeIsNullOrValid(Node? genome)
        {
            return genome == null || GenomeIsValid(genome);
        }

        
        protected bool GenomeIsValid(Node genome)
        {
            return probabilityDistribution != null && STGP_Sharp.GpRunner.IsValidTree(genome,
                probabilityDistribution, maxDepth);
        }
    }

    public class RampedPopulationInitialization : PopulationInitializationMethod
    {
        public Node? genomeToStartInPopulation;

        public RampedPopulationInitialization(GpPopulationParameters populationParameters) : base(populationParameters)
        {
        }

        public RampedPopulationInitialization(ProbabilityDistribution probabilityDistribution, int maxDepth)
            : base(probabilityDistribution, maxDepth)
        {
        }

        // TODO can't run this function with filters
        public override async Task<List<Individual>>
            GetPopulation<T>(STGP_Sharp.GpRunner gp, TimeoutInfo timeoutInfo) // Get around T by using MakeClosedGenericType
        {
            // TODO Assert we have a distribution of heights?
            // Don't know if we should do this because can't always do that if building block min tree height is lower than the ramped depth

            var population = new List<Individual>();
            var uniqueNodes = new List<Node>();
            var n = gp.populationParameters.populationSize * 10; // TODO replace with parameter
            for (
                var individualNumber = 0;
                individualNumber < n && population.Count < gp.populationParameters.populationSize &&
                !timeoutInfo.ShouldTimeout;
                individualNumber++)
            {
                // Ramp the depth
                var t = typeof(T);
                var isSubclassOfGpBuildingBlock = STGP_Sharp.GpRunner.IsSubclassOfGpBuildingBlock(t);
                var minTreeData = isSubclassOfGpBuildingBlock
                    ? gp.nodeTypeToMinTreeDictionary[t]
                    : gp.nodeReturnTypeToMinTreeDictionary[new ReturnTypeSpecification(t, null)];

                var possibleRampedDepth = individualNumber % gp.populationParameters.maxDepth + 1;
                var currentMaxDepth =
                    gp.populationParameters.ramp
                        ? Math.Max(minTreeData.heightOfMinTree + 1, possibleRampedDepth)
                        : gp.populationParameters.maxDepth;

                var randomTree = gp.GenerateRandomTreeFromTypeOrReturnType<T>(currentMaxDepth, gp.rand.NextBool());
                if (uniqueNodes.Contains(randomTree, new NodeComparer()))
                {
                    CustomPrinter.PrintLine("Duplicate node found in initialization");
                    continue;
                }

                uniqueNodes.Add(randomTree);

                foreach (var child in randomTree.children)
                    foreach (var node in child.IterateNodes())
                    {
                        var tp = gp.populationParameters.probabilityDistribution.distribution
                            .First(typeProbability => typeProbability.type == node.GetType());
                        Debug.Assert(tp.probability != 0);
                    }


                Debug.Assert(randomTree.GetHeight() <= gp.populationParameters.maxDepth);

                var ind = new Individual(randomTree);
                population.Add(ind);
            }

            if (null != genomeToStartInPopulation)
            {
                var goalIndividual = new Individual(genomeToStartInPopulation);
                population.Add(goalIndividual);
            }

            await gp.EvaluatePopulation(population);
            // for (var i = 0; i < population.Count; i++)
            // {
            //     await gp.EvaluateFitnessOfIndividual(i, population);
            // }

            return population;
        }
    }


    public class RandomPopulationInitialization : PopulationInitializationMethod
    {
        public RandomPopulationInitialization(GpPopulationParameters populationParameters) : base(populationParameters)
        {
        }

        public RandomPopulationInitialization(ProbabilityDistribution probabilityDistribution, int maxDepth)
            : base(probabilityDistribution, maxDepth)
        {
        }

        // TODO can't run this function with filters
        public override async Task<List<Individual>>
            GetPopulation<T>(STGP_Sharp.GpRunner gp, TimeoutInfo timeoutInfo) // Get around T by using MakeClosedGenericType
        {
            var population = new List<Individual>();
            for (var i = 0; i < gp.populationParameters.populationSize && !timeoutInfo.ShouldTimeout; i++)
            {
                var randomTree =
                    gp.GenerateRandomTreeFromTypeOrReturnType<T>(gp.populationParameters.maxDepth, gp.rand.NextBool());
                var ind = new Individual(randomTree);
                population.Add(ind); 
            }

            await gp.EvaluatePopulation(population);

            return population;
        }
    }
}