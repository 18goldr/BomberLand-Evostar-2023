#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Cysharp.Threading.Tasks;
using GP;
using GP.GpWorldState;
using GP.Interfaces;
using Newtonsoft.Json;
using STGP_Sharp.Fitness;
using STGP_Sharp.Fitness_and_Gp_Results;
using STGP_Sharp.Interfaces;
using STGP_Sharp.Utilities.GeneralCSharp;
using STGP_Sharp.Utilities.GP;
using Random = System.Random;
// ReSharper disable SuspiciousTypeConversion.Global

namespace STGP_Sharp
{
    public struct TimeoutInfo
    {
        public int timeLimitInSeconds;
        public DateTime runStartTime;
        public bool ignoreGenerationsUseTimeout;
        public CancellationTokenSource cancelTokenSource;

        public bool ShouldTimeout =>
            (ignoreGenerationsUseTimeout && GeneralCSharpUtilities.SecondsElapsedSince(runStartTime) > timeLimitInSeconds) ||
            cancelTokenSource.IsCancellationRequested;
    }


    // TODO currently GpRunner is basically used as a heavily parameterized static class, ie. it doesn't maintain any state of generated populations, etc.
    // The only state it maintains right now is verbose info
    // Maybe it's just better to change it to a static class?
    public partial class GpRunner
    {
        public readonly IFitnessFunction fitnessFunction;
        public readonly IAlgorithmWrapperToBeUsedWithGp? otherAlgorithm;
        public readonly GpPopulationParameters populationParameters;
        public readonly Random rand;
        public readonly int? randomSeed;
        public readonly IGpWorldStateEvaluationParametersWrapper? evaluationParameters;
        public readonly Type solutionReturnType;
        public readonly TimeoutInfo timeoutInfo;
        public readonly VerboseInfo verbose;
        public readonly IGpWorldStateWrapper? worldState;
        public readonly GpExperimentProgressAbstract? progress;
        public readonly bool multithread;
        public readonly PositionalArguments? positionalArguments;
        private readonly Type _fitnessType;
        private readonly Type _gpResultsStatsType;
        public readonly bool coevolve;
        private readonly string? checkPointSaveFile;
        // Action to perform after evaluating a population.
        private Action? _postFitnessEvaluationFunction;
        // Action to perform at the end of each generation.
        private Action? _postGenerationFunction;

        public GpRunner(
            IFitnessFunction fitnessFunction,
            GpPopulationParameters populationParameters,
            Type solutionReturnType,
            TimeoutInfo timeoutInfo,
            IGpWorldStateWrapper? simWorldState = null,
            IGpWorldStateEvaluationParametersWrapper? evaluationParameters = null,
            IAlgorithmWrapperToBeUsedWithGp? otherAlgorithm = null,
            int? randomSeed = null,
            bool verbose = false,
            GpExperimentProgressAbstract? progress = null,
            bool multithread = true,
            PositionalArguments? positionalArguments = null,
            string? checkPointSaveFile = null,
            Action? postFitnessEvaluationFunction = null,
            Action? postGenerationFunction = null)
        {
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            
            this.fitnessFunction = fitnessFunction;
            this.worldState = simWorldState;
            this.solutionReturnType = solutionReturnType;
            this.evaluationParameters = evaluationParameters;
            this.timeoutInfo = timeoutInfo;
            this.populationParameters = populationParameters;
            this.randomSeed = randomSeed;
            this.verbose = verbose;
            this.rand = this.randomSeed != null ? new Random(this.randomSeed.Value) : new Random();
            this.otherAlgorithm = otherAlgorithm;
            this.progress = progress;
            this.multithread = multithread;
            this.positionalArguments = positionalArguments;
            this.checkPointSaveFile = checkPointSaveFile;
            this._postFitnessEvaluationFunction = postFitnessEvaluationFunction;
            this._postGenerationFunction = postGenerationFunction;

            this._fitnessType = this.fitnessFunction.FitnessType;
            
            GpResultsUtility.ValidateFitnessFunctionAndRelatedClasses(this.fitnessFunction);

            this._gpResultsStatsType = GpResultsUtility.GetGpResultsStatsType(this._fitnessType);

            this.coevolve = this.fitnessFunction is IFitnessFunctionCoevolutionSync or IFitnessFunctionCoevolutionAsync;
            
            // NOTE: Probability distribution must be defined before the helper
            SetMinTreeDictionariesForSatisfiableNodeTypes();
        }


        public async Task<GeneratedPopulations> RunAsync()
        {
            if (null != progress)
            {
                progress.generationsInRunCount = populationParameters.numberGenerations;
                progress.generationsInRunCompleted = 0;
                progress.status = "Init population";
            }

            var initializationMethodType = populationParameters.populationInitializationMethod.GetType();
            var initializationMethodInfo =
                initializationMethodType.GetMethod("GetPopulation") ??
                throw new Exception($"GetPopulation is not defined in the class {initializationMethodType.Name}");

            var population = await (Task<List<Individual>>)(initializationMethodInfo
                .MakeGenericMethod(solutionReturnType)
                .Invoke(
                    populationParameters.populationInitializationMethod,
                    new object[] { this, timeoutInfo }) ?? throw new Exception("Could not initialize method"));
            
            if (timeoutInfo.ShouldTimeout || this.populationParameters.numberGenerations == 0)
                return new GeneratedPopulations(
                    new[] { population }.ToNestedList(),
                    GpResultsUtility.GetDetailedSummary(
                        this._gpResultsStatsType,
                        population),
                    timeoutInfo.runStartTime,
                    DateTime.Now,
                    population.SortedByFitness().FirstOrDefault(),
                    verbose
                );
            

            this.MaybeSaveProgressToCheckpointJson(population);

            if (null != progress) progress.status = "Evolving";
            var allPopulationsFromRun = await this.SearchLoop(population);
            return allPopulationsFromRun;
        }

        private void ThrowErrorIfAnyIndividualHasNullFitness(IEnumerable<Individual> population)
        {
            var nullFitness = population.Where(i => null == i.fitness).ToList();
            if (nullFitness.Any())
            {
                throw new Exception($"Fitness is null for individuals {string.Join(", ", nullFitness)}");
            }
        }
        
        private async Task<GeneratedPopulations> SearchLoop(
            List<Individual> population)
        {
            // The caller is required to already have evaluated the given population
            Debug.Assert(population.All(i => null != i.fitness));

            if (null != progress)
            {
                progress.generationsInRunCount = populationParameters.numberGenerations;
                progress.generationsInRunCompleted = 0;    
            }

            var allPopulations = new List<List<Individual>>();
            

            population = population.SortedByFitness();

            allPopulations.Add(population);

            var bestEver = population.FirstOrDefault() ??
                           throw new Exception("Population is empty");

            this.ThrowErrorIfAnyIndividualHasNullFitness(population);
            
            for (var generationIndex = 0;
                 timeoutInfo.ignoreGenerationsUseTimeout ||
                 generationIndex < populationParameters.numberGenerations;
                 generationIndex++)
            {
                List<Individual> newPopulation = await GenerateNewPopulation(population);
 
                newPopulation = newPopulation.SortedByFitness();
                bestEver = newPopulation.First();

                allPopulations.Add(newPopulation);

                ThrowErrorIfAnyIndividualHasNullFitness(newPopulation);

                if (null != progress) progress.generationsInRunCompleted = generationIndex + 1;

                if (timeoutInfo.ShouldTimeout) break;
                
                
                // Only add checkpoint save if this isn't the last generation
                //if (generationIndex + 1 <= populationParameters.numberGenerations) 
                this.MaybeSaveProgressToCheckpointJson(allPopulations.Flatten());
                
                this._postGenerationFunction?.Invoke();
                
                CustomPrinter.PrintLine($"Generation {generationIndex}");
            }

            return new GeneratedPopulations(
                allPopulations,
                GpResultsUtility.GetDetailedSummary(
                    this._gpResultsStatsType, 
                    allPopulations.Flatten()),
                timeoutInfo.runStartTime,
                DateTime.Now,
                bestEver,
                verbose);
        }

        partial void SetMinTreeDictionariesForSatisfiableNodeTypes();

        public async Task EvaluatePopulation(List<Individual> population)
        {
            IEnumerable<FitnessBase> results;
            var enumerable = Enumerable.Range(0, population.Count);


            try
            {
                switch (this.fitnessFunction)
                {
                    case IFitnessFunctionCoevolutionSync coevolutionFitnessFunction:
                        results =
                            coevolutionFitnessFunction.GetFitnessOfPopulationUsingCoevolution(this, population);
                        break;
                    case IFitnessFunctionSync syncFitnessFunction:
                        results = enumerable.Select(i => 
                            syncFitnessFunction.GetFitnessOfIndividual(this, population[i]));
                        break;
                    case IFitnessFunctionCoevolutionAsync coevolutionAsyncFitnessFunction:
                        // TODO make note that user will define whether it is a long running task or not
                        results = await coevolutionAsyncFitnessFunction.GetFitnessOfPopulationUsingCoevolutionAsync(this,
                            population);
                        break;
                    case IFitnessFunctionAsync asyncFitnessFunction:
                        // TODO make note that user will define whether it is a long running task or not
                        var asyncTasks = enumerable.Select(i => 
                            asyncFitnessFunction.GetFitnessOfIndividualAsync(this, population[i]));
                        results = await Task.WhenAll(asyncTasks);
                        break;
                    default:
                        throw new Exception(
                            "The given fitness function does not implement either IFitnessFunctionAsync," +
                            "IFitnessFunctionCoevolutionSync, IFitnessFunctionCoevolutionSync, or IFitnessFunctionSync.");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                MaybeSaveProgressToCheckpointJson(population);
                throw;
            }
           
            var resultsAsList = results.ToList();

            for (int i = 0; i < population.Count; i++)
            {
                population[i].fitness = resultsAsList[i];
            }
            
            this._postFitnessEvaluationFunction?.Invoke();
        }

        // TODO move this to Node.cs so you can define your own crossover operator per type
        private (Node, Node)? OnePointCrossoverChildren(Node t1, Node t2) // TODO wish we could be more specific 
        {
            var a = t1.DeepCopy();
            var b = t2.DeepCopy();

            var xPoints = GpRunner.GetLegalCrossoverPointsInChildren(a, b);
            if (!xPoints.Any())
            {
                if (verbose) CustomPrinter.PrintLine("No legal crossover points. Skipped crossover");
                verbose.numberOfTimesNoLegalCrossoverPoints++;
                return null;
            }

            var xaRand = xPoints.Keys.GetRandomEntry(rand);
            var xbRand = xPoints[xaRand].GetRandomEntry(rand);

            var xaSubTreeNodeWrapper = a.GetNodeWrapperAtIndex(xaRand);
            var xbSubTreeNodeWrapper = b.GetNodeWrapperAtIndex(xbRand);

            if (xaSubTreeNodeWrapper.child.Equals(xbSubTreeNodeWrapper.child))
            {
                if (verbose) CustomPrinter.PrintLine("Nodes swapped are equivalent");
                verbose.numberOfTimesCrossoverSwappedEquivalentNode++;
                return null;
            }

            if (xaSubTreeNodeWrapper.child.GetHeight() + b.GetDepthOfNodeAtIndex(xbRand) >
                populationParameters.maxDepth ||
                xbSubTreeNodeWrapper.child.GetHeight() + a.GetDepthOfNodeAtIndex(xaRand) >
                populationParameters.maxDepth)
            {
                if (verbose)
                {
                    CustomPrinter.PrintLine("Crossover too deep");
                    CustomPrinter.PrintLine($"     Height: {a.GetDepthOfNodeAtIndex(xaRand)} --");
                    xaSubTreeNodeWrapper.child.PrintAsList("     ");
                    CustomPrinter.PrintLine($"     Height: {b.GetDepthOfNodeAtIndex(xbRand)} -- ");
                    xbSubTreeNodeWrapper.child.PrintAsList("     ");
                }

                verbose.numberOfTimesCrossoverWasTooDeep++;
                return null;
            }

            var tmp = xbSubTreeNodeWrapper.child;
            xbSubTreeNodeWrapper.ReplaceWith(xaSubTreeNodeWrapper.child);
            xaSubTreeNodeWrapper.ReplaceWith(tmp);
            Debug.Assert(xaSubTreeNodeWrapper.child.returnType == xbSubTreeNodeWrapper.child.returnType);

            return (a, b);
        }

        public void Mutate(Node root)
        {
            // TODO can we do some type checking here? (ie. root.GetType() is TypedRootNode<>)
            var nodesToChooseFrom = root.IterateNodeWrapperWithoutRoot().ToList();

            // Hack to not mutate frequency if it's a canMove primitive in bomberland gp
            var bomberlandHacksNodesToChooseFrom = nodesToChooseFrom
                .Where(n => 
                    n.child.returnType != typeof(float)
                    && (!n.parent?.symbol.Contains("canMove") ?? false))
                .ToList();
            var oldNode = bomberlandHacksNodesToChooseFrom.GetRandomEntry(rand);
            var randomTreeMaxDepth = root.GetHeight() - root.GetDepthOfNode(oldNode.child);

            if (oldNode.parent == null) throw new NullReferenceException("Old node parent cannot be null.");
            var randomNode = oldNode.child.Mutate(this, randomTreeMaxDepth);

            if (oldNode.child.Equals(randomNode))
            {
                if (verbose) CustomPrinter.PrintLine("Mutated node is equivalent to old node.");
                return;
            }

            oldNode.ReplaceWith(randomNode);
        }

        public List<Individual> TournamentSelection(List<Individual> population)
        {
            Debug.Assert(population.All(i => null != i.fitness));

            var winners = new List<Individual>();

            for (var tournamentNumber = 0;
                 tournamentNumber < populationParameters.populationSize;
                 tournamentNumber++)
            {
                var tmpPopulation = population.ToList();
                var competitors = new List<Individual>();
                // Populate competitors
                for (var competitorNumber = 0;
                     competitorNumber < populationParameters.tournamentSize;
                     competitorNumber++)
                {
                    var competitor = tmpPopulation.GetRandomEntry(rand);
                    competitors.Add(competitor);
                    tmpPopulation.Remove(competitor);
                }

                competitors = competitors.SortedByFitness();
                var winner = competitors.FirstOrDefault() ??
                             throw new Exception("List of competitors cannot be empty.");
                winners.Add(winner);
            }

            return winners;
        }

        public void GenerationalReplacement(ref List<Individual> newPop, List<Individual> oldPop)
        {
            // TODO add unit tests

            // Sort the population
            oldPop = oldPop.SortedByFitness();
            newPop = newPop.SortedByFitness();

            // Store the original old population so we can check that individuals have been
            // propagated to the new population correctly.
            var originalOldPop = oldPop.ToList();

            // Append "elite size" best solutions from the old population to the new population.
            newPop.AddRange(oldPop.Take(populationParameters.eliteSize));
            // Remove those solutions from the old population so that they are not selected
            // again when checking whether the new population is the correct size.
            oldPop.RemoveRange(0, populationParameters.eliteSize);

            // Check if the new population has the correct size.
            // If not, add to the new population however many are missing from the old population 
            if (newPop.Count < populationParameters.populationSize)
            {
                var numberMissing = populationParameters.populationSize - newPop.Count;
                newPop.AddRange(oldPop.Take(numberMissing));
            }

            newPop = newPop.SortedByFitness();

            // We may have added more individuals than allowed, so only take "population size" individuals
            if (newPop.Count > populationParameters.populationSize)
                newPop = newPop.Take(populationParameters.populationSize).ToList();
            // TODO need to remove from oldPop? re oldPop.RemoveRange(0, this.populationParameters.eliteSize);
            Debug.Assert(newPop.Count == populationParameters.populationSize);

            var bestNewPop = newPop.First().fitness ?? throw new InvalidOperationException();
            var bestOldPop = originalOldPop.SortedByFitness().First().fitness ?? throw new InvalidOperationException();
            if (bestNewPop.LessThan(bestOldPop))
                throw new Exception("New population best is worse than old population best");
        }
        

        public async Task<List<Individual>> GenerateNewPopulation(List<Individual> oldPopulation)
        {

            #region Selection
            var parents = TournamentSelection(oldPopulation);
            #endregion

            #region Variation — Generate new individuals
            // Crossover
            var newPopulation = CrossoverListOfParents(parents);

            // Mutation
            newPopulation.ForEach(i =>
            {
                if (rand.NextDouble() > populationParameters.mutationProbability)
                {
                    if (verbose) CustomPrinter.PrintLine("Skipped mutation");
                    verbose.numberOfTimesMutationSkipped++;
                }
                else
                {
                    Mutate(i.genome);
                }
            });

            if (this.verbose && newPopulation.Count < populationParameters.populationSize)
                CustomPrinter.PrintLine(
                    $"{populationParameters.populationSize - newPopulation.Count} individuals removed in deny list");
            #endregion

            #region Evaluation
            await this.EvaluatePopulation(newPopulation);
            #endregion

            #region Generational replacement
            // Replace worst performing new individuals with the best performing old individuals
            GenerationalReplacement(ref newPopulation, oldPopulation);
            Debug.Assert(newPopulation.Count == populationParameters.populationSize);
            #endregion

            return newPopulation;
        }

        public List<Individual> CrossoverListOfParents(List<Individual> parents)
        {
            var newPopulation = new List<Individual>();
            // Ensure within size constraint
            while (newPopulation.Count < populationParameters.populationSize)
            {
                var parent1 = parents.GetRandomEntry(rand).DeepCopy();
                var tmpParents = parents.ToList();
                tmpParents.Remove(parent1);
                var parent2 = tmpParents.GetRandomEntry(rand).DeepCopy();

                if (rand.NextDouble() > populationParameters.crossoverProbability)
                {
                    if (verbose) CustomPrinter.PrintLine("Skipped crossover");
                    verbose.numberOfTimesCrossoverSkipped++;
                    newPopulation.Add(parent1);
                    if (newPopulation.Count < populationParameters.populationSize) newPopulation.Add(parent2);

                    continue;
                }

                var crossoverChildren = OnePointCrossoverChildren(parent1.genome, parent2.genome) ??
                                        (parent1.genome.DeepCopy(), parent2.genome.DeepCopy());
                var (child1, child2) = crossoverChildren;

                var individual1 = new Individual(child1);
                newPopulation.Add(individual1);

                if (newPopulation.Count >= populationParameters.populationSize) continue;

                var individual2 = new Individual(child2);
                newPopulation.Add(individual2);
            }

            return newPopulation;
        }
    }
}
