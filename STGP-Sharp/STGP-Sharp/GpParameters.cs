// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using GP;
// using Vector2 = System.Numerics.Vector2;
//
// // ReSharper disable MemberCanBePrivate.Global
// // ReSharper disable ConvertToConstant.Global
// // ReSharper disable FieldCanBeMadeReadOnly.Global
//
// #nullable enable
//
// namespace STGP_Sharp
// {
//     public enum InputType
//     {
//         File,
//         Editor
//     }
//
//     // TODO move this into actual GP code and use it as input
//     // TODO separate into functions and data
//     public class GpParameters
//     {
//         [FoldoutGroup("Vector2 Parameters")]
//         private Vector2 _vector2FloatMaxValues = GpPopulationParameters.DEFAULT_VECTOR2_FLOAT_MAX_VALUES;
//
//         [FoldoutGroup("Vector2 Parameters")]
//         private Vector2 _vector2FloatMinValues = GpPopulationParameters.DEFAULT_VECTOR2_FLOAT_MIN_VALUES;
//
//         [FoldoutGroup("Genetic Operator Parameters")] [MinValue(0)] [MaxValue(1)]
//         public double crossoverProbability = GpPopulationParameters.DEFAULT_CROSSOVER_PROBABILITY;
//
//         [FoldoutGroup("Evolutionary Search Loop")] [MinValue(0)]
//         public int eliteSize = GpPopulationParameters.DEFAULT_ELITE_SIZE;
//
//
//         [FoldoutGroup("Fitness Function Parameters")]
//         [OnInspectorInit("InitFitnessFunctionType")]
//         [ValueDropdown("EnumerateFitnessFunctionTypes")]
//         public Type fitnessFunctionType = typeof(TestFitnessFunction);
//
//         [FoldoutGroup("Float Parameters")]
//         // [MinValue("Math.Max($floatMin, -100000)"), MaxValue(100000)]
//         [MinValue("$floatMin")]
//         [MaxValue(100000)]
//         public float floatMax = GpPopulationParameters.DEFAULT_FLOAT_MAX;
//
//         // TODO bound float min and max
//         [FoldoutGroup("Float Parameters")]
//         // [MinValue(-100000), MaxValue("Math.Min($floatMax, 100000)")]
//         [MinValue(-100000)]
//         [MaxValue("$floatMax")]
//         public float floatMin = GpPopulationParameters.DEFAULT_FLOAT_MIN;
//
//         [FoldoutGroup("Population Initialization Parameters")]
//         [ValueDropdown("$PopulationInitializationMethodInstances")]
//         [OnInspectorInit("InitPopulationInitializationMethod")]
//         public PopulationInitializationMethod? initializationMethod;
//
//         [FoldoutGroup("Evolutionary Search Loop")] [MinValue(0)]
//         public int maxDepth = GpPopulationParameters.DEFAULT_MAX_DEPTH;
//
//         [FoldoutGroup("Genetic Operator Parameters")] [MinValue(0)] [MaxValue(1)]
//         public double mutationProbability = GpPopulationParameters.DEFAULT_MUTATION_PROBABILITY;
//
//         [FoldoutGroup("Float Parameters")] [MinValue(1)]
//         public int numberDiscreteFloatSteps = GpPopulationParameters.DEFAULT_NUMBER_DISCRETE_FLOAT_STEPS;
//
//         [FoldoutGroup("Fitness Function Parameters")]
//         [ShowIf("@typeof(AverageFitnessOverMultipleExecutionsSync).IsAssignableFrom(fitnessFunctionType)")]
//         public int numberExecutionsForFitnessFunction =
//             GpPopulationParameters.DEFAULT_NUMBER_EXECUTIONS_FOR_MULTIPLE_EXECUTION_FITNESS_FUNCTION;
//
//         [FoldoutGroup("Evolutionary Search Loop")] [MinValue(0)]
//         public int numberGenerations = GpPopulationParameters.DEFAULT_NUMBER_GENERATIONS;
//
//         [FoldoutGroup("Genetic Operator Parameters")] [MinValue(0)]
//         public int numberOfTimesToRepeatCrossover = GpPopulationParameters.DEFAULT_NUMBER_OF_TIMES_TO_REPEAT_CROSSOVER;
//
//         [FoldoutGroup("Genetic Operator Parameters")] [MinValue(0)]
//         public int numberOfTimesToRepeatMutation = GpPopulationParameters.DEFAULT_NUMBER_OF_TIMES_TO_REPEAT_MUTATION;
//
//         [FoldoutGroup("Evolutionary Search Loop")] [MinValue("$tournamentSize")]
//         public int populationSize = GpPopulationParameters.DEFAULT_POPULATION_SIZE;
//
//         [FoldoutGroup("Strong Typing and Probabilities")]
//         [NonSerialized]
//         [OdinSerialize]
//         [ListDrawerSettings(AlwaysAddDefaultValue = true, CustomAddFunction = "DefaultTypeProbability")]
//         [OnInspectorInit("InitProbabilityDistributionEditor")]
//         [ShowIf("@this.probabilityDistributionInput == InputType.Editor")]
//         [Indent]
//         public List<TypeProbability>? probabilityDistributionEditor;
//
//         [Indent]
//         [FoldoutGroup("Strong Typing and Probabilities")]
//         [ShowIf("@this.probabilityDistributionInput == InputType.File")]
//         public string? probabilityDistributionFile = null;
//
//         [FoldoutGroup("Strong Typing and Probabilities")] 
//         public InputType probabilityDistributionInput;
//
//         [FoldoutGroup("Evolutionary Search Loop")]
//         public bool ramp = GpPopulationParameters.DEFAULT_RAMP;
//
//         [FoldoutGroup("Misc GP Parameters")] [ShowIf("@this.useRandomSeed == true")]
//         public int randomSeed = 0;
//
//         [FoldoutGroup("Evolutionary Search Loop")] [MinValue(2)]
//         public int sequenceMaxNumberOfChildren = GpPopulationParameters.DEFAULT_SEQ_MAX_SIZE;
//
//         [FoldoutGroup("Strong Typing and Probabilities")]
//         [ValueDropdown("EnumerateSolutionReturnTypes")]
//         [OnInspectorInit("InitSolutionReturnType")]
//         public Type solutionReturnType = typeof(MoveToSimAgent);
//
//         [FoldoutGroup("Vector2 Parameters")] public bool syncDimensions = false;
//
//         [FoldoutGroup("Evolutionary Search Loop")] [MinValue(2)]
//         public int tournamentSize = GpPopulationParameters.DEFAULT_TOURNAMENT_SIZE;
//
//         [FoldoutGroup("Misc GP Parameters")] public bool useRandomSeed = false;
//
//         [FoldoutGroup("Misc GP Parameters")] public bool verbose = false;
//
//         public ProbabilityDistribution ProbabilityDistribution =>
//             probabilityDistributionFile == null
//                 ? new ProbabilityDistribution(probabilityDistributionEditor ?? new List<TypeProbability>())
//                 : ProbabilityDistribution.GetProbabilityDistributionFromFile(probabilityDistributionFile);
//
//         private IEnumerable<PopulationInitializationMethod>? PopulationInitializationMethodInstances =>
//             PopulationInitializationMethod.GetPopulationInitializationMethodTypes()?
//                 .Select(t =>
//                     (PopulationInitializationMethod)Activator.CreateInstance(t, ProbabilityDistribution,
//                         maxDepth));
//
//         [FoldoutGroup("Float Parameters")]
//         [ShowInInspector]
//         public HashSet<float> DiscreteFloatSearchSpace // TODO this is really inefficient
//         {
//             get
//             {
//                 var floats = new HashSet<float>();
//                 if (floatMin > floatMax)
//                     CustomPrinter.PrintLine("Float min must be less than or equal to float max.");
//                 if (floatMax < floatMin)
//                     CustomPrinter.PrintLine("Float max must be greater than or equal to float min.");
//
//                 if (Math.Abs(floatMin - floatMax) < 0.00001)
//                     floats.Add(floatMax);
//                 else
//                     for (var i = floatMin; i <= floatMax; i += (floatMax - floatMin) / 100)
//                         floats.Add(i.Quantize(numberDiscreteFloatSteps, floatMin, floatMax));
//
//                 return floats;
//             }
//         }
//
//         [OdinSerialize]
//         [FoldoutGroup("Vector2 Parameters")]
//         public Vector2 Vector2FloatMinValues
//         {
//             get => _vector2FloatMinValues;
//             set => SetVector2Extremes(ref _vector2FloatMinValues, value);
//         }
//
//         [FoldoutGroup("Vector2 Parameters")]
//         [OdinSerialize]
//         public Vector2 Vector2FloatMaxValues
//         {
//             get => _vector2FloatMaxValues;
//             set => SetVector2Extremes(ref _vector2FloatMaxValues, value);
//         }
//
//         // Vector 2 Parameters
//         private void SetVector2Extremes(ref Vector2 extremesToSet, Vector2 value)
//         {
//             if (syncDimensions)
//                 extremesToSet.Y = extremesToSet.X = value.X;
//
//             else
//                 extremesToSet = value;
//         }
//         
//         public GpRunner GetGp(SimWorldState? simWorldState, // TODO should simworld state be null?
//             SimEvaluationParameters simEvaluationParameters,
//             TimeoutInfo? timeoutInfo = null,
//             ManyWorldsPlannerRunner? manyWorldsPlannerRunner =
//                 null) // TODO move this to another class. Want GpParameters to be a data only class
//         {
//             var popParams = new GpPopulationParameters(
//                 eliteSize,
//                 tournamentSize,
//                 populationSize,
//                 maxDepth,
//                 numberGenerations,
//                 crossoverProbability,
//                 mutationProbability,
//                 numberOfTimesToRepeatMutation,
//                 numberOfTimesToRepeatCrossover,
//                 sequenceMaxNumberOfChildren,
//                 ramp,
//                 floatMin,
//                 floatMax,
//                 vector2FloatMaxValues: Vector2FloatMaxValues,
//                 vector2FloatMinValues: Vector2FloatMinValues,
//                 numberExecutionsForMultipleExecutionFitnessFunction: numberExecutionsForFitnessFunction,
//                 numberDiscreteFloatSteps: numberDiscreteFloatSteps,
//                 populationInitializationMethod: initializationMethod,
//                 probabilityDistribution: ProbabilityDistribution
//             );
//
//             // Set fitness function
//             var fitnessFunction = FitnessFunctionHelper.ConstructAndVerify(fitnessFunctionType,
//                 simEvaluationParameters.primaryScoringFunction ??
//                 throw new Exception("TODO make scoring function optional"),
//                 simEvaluationParameters.gpScoringFunctionWeight);
//
//             var state =
//                 FitnessFunctionHelper.GetCorrectSimWorldStateGivenAFitnessFunction(fitnessFunction, new SimWorldStateWrapperToBeUsedWithGp(simWorldState))
//                     .DeepCopy();
//
//             var stateAsGpWrapper = ((SimWorldStateWrapperToBeUsedWithGp)state);
//
//             var otherAlgorithm = null == manyWorldsPlannerRunner
//                 ? null
//                 : new ManyWorldsPlannerWrapperWrapperToBeUsedWithGp(
//                     manyWorldsPlannerRunner.plannerParameters,
//                     stateAsGpWrapper.simWorldState,
//                     PlannerGoalBuilder.MakeGoal(stateAsGpWrapper.simWorldState, manyWorldsPlannerRunner.plannerParameters,
//                         manyWorldsPlannerRunner.plannerParameters.GoalWaypointAsCircle,
//                         manyWorldsPlannerRunner.plannerParameters.WaypointOptionsAsCircles));
//             
//             var timeoutInfoNotNull = timeoutInfo ?? new TimeoutInfo
//                 { ignoreGenerationsUseTimeout = false, cancelTokenSource = new CancellationTokenSource() };
//
//             return new GpRunner(
//                 fitnessFunction,
//                 popParams,
//                 solutionReturnType,
//                 randomSeed: useRandomSeed ? randomSeed : (int?)null,
//                 verbose: verbose,
//                 otherAlgorithm: otherAlgorithm,
//                 evaluationParameters: new SimEvaluationParametersWrapperToBeUsedWithGp(simEvaluationParameters),
//                 simWorldState: state,
//                 timeoutInfo: timeoutInfoNotNull
//             );
//         }
//
//         private static IEnumerable<Type> EnumerateSolutionReturnTypes()
//         {
//             return GpRunner.GetAllReturnTypes();
//         }
//
//         private static IEnumerable<Type> EnumerateFitnessFunctionTypes()
//         {
//             return GpRunner.GetFitnessFunctionTypes();
//         }
//
//         
//         public void InitPopulationInitializationMethod()
//         {
//             initializationMethod ??= PopulationInitializationMethodInstances?.First();
//         }
//
//         
//         public void InitSolutionReturnType()
//         {
//             // ReSharper disable once ConstantNullCoalescingCondition
//             solutionReturnType ??= EnumerateSolutionReturnTypes().First();
//         }
//
//         
//         public void InitFitnessFunctionType()
//         {
//             // ReSharper disable once ConstantNullCoalescingCondition
//             fitnessFunctionType ??= EnumerateFitnessFunctionTypes().First();
//         }
//
//         
//         public void InitProbabilityDistributionEditor()
//         {
//             probabilityDistributionEditor ??= new List<TypeProbability>();
//         }
//
//         
//         public TypeProbability DefaultTypeProbability()
//         {
//             return new TypeProbability();
//         }
//     }
// }