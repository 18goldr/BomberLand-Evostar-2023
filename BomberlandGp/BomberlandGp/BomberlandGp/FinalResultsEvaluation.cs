using BomberLandGp;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using STGP_Sharp;
using STGP_Sharp.Utilities.GP;

namespace BomberlandGp;

public static class FinalResultsEvaluation
{
    public class FitnessBaseConverter : CustomCreationConverter<FitnessLexicographic>
    {
        public override FitnessLexicographic Create(Type objectType)
        {
            return new FitnessLexicographic(new List<double>(){}, new List<FitnessLexicographic.MinOrMax>() {});
        }
    }
    
    public struct SerializationDataOneIndividual
    {
        public string stringRepresentation;
        public int height;
        public int size;
        public List<double> fitnessLexicographic;

        public SerializationDataOneIndividual(Individual ind)
        {
            this.stringRepresentation = ind.genome.ToString();
            this.height = ind.genome.GetHeight();
            this.size = ind.genome.GetSize();
            this.fitnessLexicographic = (ind.fitness as FitnessLexicographic)?.lexicographicFitness ?? throw new Exception("Cant cast"); 
        }
    }
    
    public struct SerializationData
    {
        public IEnumerable<SerializationDataOneIndividual> canMove;
        public IEnumerable<SerializationDataOneIndividual> cannotMove;
        public string fitnessSummary;
    }

    public static async Task EvaluateBestEversCanMoveVsCannotMove()
    {
        const int numBattleSimulations = 10;
        const int numThreads = 12;
        
        var currentDirectory = AppDomain.CurrentDomain.BaseDirectory
            .Split(new[] { "BomberlandGp" }, StringSplitOptions.None)
            .First();

        var canMoveResultsFolder = Path.Combine(currentDirectory, "GpResults/CANMOVERESULTS");
        var cannotMoveResultsFolder = Path.Combine(currentDirectory, "GpResults/CANNOTMOVERESULTS");
        
        var canMoveDirectory = new DirectoryInfo(canMoveResultsFolder);
        var cannotMoveDirectory = new DirectoryInfo(cannotMoveResultsFolder);

        var cannotMoveResults = cannotMoveDirectory.GetFiles("gpResults*.json", SearchOption.AllDirectories);
        
        var canMoveResults = canMoveDirectory.GetFiles("gpResults*.json", SearchOption.AllDirectories);
        var bestEverCanMove = canMoveResults.Select(f =>
        {
            var file = File.ReadAllText(f.FullName);
            var best = JObject.Parse(file);
            best.TryGetValue("bestEver", out var bestEver);
            var genome = bestEver?.SelectToken("genome") ?? throw new Exception();
            var cast = genome.ToObject<BomberLandAgentBehavior>() ?? throw new Exception();
            return cast;
        }).ToList();
        
        var bestEverCannotMove = canMoveResults.Select(f =>
        {
            var file = File.ReadAllText(f.FullName);
            var best = JObject.Parse(file);
            best.TryGetValue("bestEver", out var bestEver);
            var genome = bestEver?.SelectToken("genome") ?? throw new Exception();
            var cast = genome.ToObject<BomberLandAgentBehavior>() ?? throw new Exception();
            return cast;
        }).ToList();
        var population = bestEverCanMove.ToList();
        population.AddRange(bestEverCannotMove);

        var battlePairings = new List<(int, int)>();

        for (int i = 0; i < cannotMoveResults.Length; i++)
        {
            for (int j = 0; j < canMoveResults.Length; j++)
            {
                for (int battle = 0; battle < numBattleSimulations; battle++)
                {
                    var pairing = (i, canMoveResults.Length + j);
                    battlePairings.Add(pairing);
                }
                    
            }
        }

        var fitnessFunction = new LexicographicVictoriesTurnsToWinTurnsToLose(
            currentDirectory, numBattleSimulations, numThreads);

        var inds = population.Select(b => new Individual(b, new FitnessLexicographic(new List<double>() { 1, 1, 1, 1 },
            new List<FitnessLexicographic.MinOrMax>()
            {
                FitnessLexicographic.MinOrMax.Max,
                FitnessLexicographic.MinOrMax.Min,
                FitnessLexicographic.MinOrMax.Min,
                FitnessLexicographic.MinOrMax.Max,
                FitnessLexicographic.MinOrMax.Max
            }))).ToList();


        var results = await fitnessFunction.GetFitnessOfPopulationUsingCoevolutionAsync(null, battlePairings, inds);

        for (int i = 0; i < results.Count; i++)
        {
            inds[i].fitness = results[i];
        }

        
        var canMoveSerializationData = Enumerable.Range(0, bestEverCanMove.Count).Select(i => new SerializationDataOneIndividual(inds[i]));
        var cannotMoveSerializationData = Enumerable.Range(bestEverCanMove.Count, bestEverCannotMove.Count).Select(i => new SerializationDataOneIndividual(inds[i]));
        var summary = GpResultsUtility.GetDetailedSummary(typeof(GpResultsStatsLexicographic), inds);

        var serializationData = new SerializationData()
        {
            canMove = canMoveSerializationData,
            cannotMove = cannotMoveSerializationData,
            fitnessSummary = summary.ToString()
        };
        
        var bestOverall = inds.SortedByFitness().First();
        var json = JsonConvert.SerializeObject(serializationData);
        await File.WriteAllTextAsync("bestRivals.json", json);

    }

}