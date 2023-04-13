using System.Diagnostics;
using System.Globalization;
using BomberlandGp;
using BomberLandGp;
using STGP_Sharp;
using Newtonsoft.Json;
using STGP_Sharp.Utilities.GeneralCSharp;



const int numberOfRuns = 5;
const int maxDepth = 5;
const int popSize = 10;
const int numberBattleSimulations = 10;
const int numThreads = 12;
const int eliteSize = 3;
const int tournamentSize = 5;
const int generations = 20;

for (int run = 0; run < numberOfRuns; run++)
{
    try
    {
        Console.WriteLine($"---------------------------------------- Run #{run} ------------------------------------");
        
        var currentDirectory = AppDomain.CurrentDomain.BaseDirectory
            .Split(new[] { "BomberlandGp" }, StringSplitOptions.None)
            .First();
        var currentTimeString = DateTime.Now.ToString("s", CultureInfo.GetCultureInfo("en-us")).Replace(":", "-");
        var resultsFile = $@"GpResults/Run{run}/gpResults_{currentTimeString}.json";
        var checkPointSaveFile = $"GpResults/Run{run}/checkPoint.json";
        var checkPointSaveFilePath = Path.Combine(currentDirectory, checkPointSaveFile);
        var resultsFilePath = Path.Combine(currentDirectory, resultsFile);

        var probabilityDistribution = new ProbabilityDistribution(new List<Type>
        {
            typeof(BomberLandActionConstant),
            typeof(BomberLandAgentBehavior),
            typeof(BomberLandAgentAttributeConstant),
            typeof(FloatConstant)
        });
        
        var populationInitializationMethod = new RampedPopulationInitialization(probabilityDistribution, maxDepth);
        var gpParams = new GpPopulationParameters(
            probabilityDistribution: probabilityDistribution,
            numberGenerations: generations,
            populationSize: popSize,
            eliteSize: eliteSize,
            tournamentSize: tournamentSize,
            floatMax: 1f,
            floatMin: 0f,
            maxDepth: maxDepth,
            ramp: true,
            populationInitializationMethod: populationInitializationMethod
        );


        var fitnessFunction = new LexicographicVictoriesTurnsToWinTurnsToLose(
            currentDirectory, numberBattleSimulations, numThreads);

        var progress = new GpExperimentProgress();

        var gpRunner = new GpRunner(
            fitnessFunction,
            gpParams,
            typeof(BomberLandAgentBehavior),
            new TimeoutInfo { cancelTokenSource = new CancellationTokenSource() },
            checkPointSaveFile: checkPointSaveFilePath,
            progress: progress,
            multithread: false,
            postFitnessEvaluationFunction: () => fitnessFunction.PostFitnessEvaluationFunction(),
            postGenerationFunction: () => fitnessFunction.PostGenerationFunction(ref progress)
        );

        GeneratedPopulations results = await gpRunner.RunAsync();

        Console.WriteLine(results.fitnessSummary);

        var json = JsonConvert.SerializeObject(results, Formatting.Indented);
        GeneralCSharpUtilities.CreateAndGetDirectoryForFileIfNotExist(resultsFilePath);
        File.WriteAllText(resultsFilePath, json);
        Console.WriteLine($"Results saved to {resultsFile}");
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
    
}

