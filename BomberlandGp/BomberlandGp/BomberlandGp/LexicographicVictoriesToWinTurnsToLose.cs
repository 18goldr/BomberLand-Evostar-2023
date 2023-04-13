
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using CliWrap.EventStream;
using Cysharp.Threading.Tasks;
using STGP_Sharp;
using STGP_Sharp.Fitness;
using STGP_Sharp.Fitness_and_Gp_Results;
using STGP_Sharp.GpBuildingBlockTypes;

#nullable enable

namespace BomberLandGp
{
    // TODO rewrite to one matrix which uses a struct for each cell.
    public class LexicographicVictoriesTurnsToWinTurnsToLose : IFitnessFunction, IFitnessFunctionCoevolutionAsync
    {
        // TODO would ideally be internal, but can't because NUnit test cases need this
        public struct ParsedResults
        {
            public int Turns;
            public int? IndexOfWhoWon;
            public int? IndexOfWhoLost;
        }

        internal int[,]? VictoriesMatrix;
        internal double[,]? TurnsToLoseMatrix;
        internal double[,]? TurnsToWinMatrix;

        private readonly string _pathPartial;
        private readonly int _numberBattleSimulations;
        private readonly int _numThreads;

        // TODO add as parameters
        private const string BomberLandPythonFolder = "BomberlandEnvironments/bomberland";
        private const string LogFile = "log.txt";
        private const string TreeToCodeFile = "treeToCode.py";
        private const string RetrieveFitnessFile = "retrieveFitness.py";
        private const string RandomSeedFile = "random_seed.py";

        private readonly ConcurrentQueue<int> _dockerContainersAvailable; 
        

        private static readonly List<FitnessLexicographic.MinOrMax> MinMaxMapping =
            new List<FitnessLexicographic.MinOrMax>
            {
                FitnessLexicographic.MinOrMax.Max, // Victories
                FitnessLexicographic.MinOrMax.Min, // # times always lose
                FitnessLexicographic.MinOrMax.Min, // Average turns to win
                FitnessLexicographic.MinOrMax.Max, // # times always win
                FitnessLexicographic.MinOrMax.Max // Average turns to lose
            };
        

        public Type FitnessType { get; } = typeof(FitnessLexicographic);

        public LexicographicVictoriesTurnsToWinTurnsToLose(string projectDirectory, int numberBattleSimulations, int numThreads)
        {
            this._pathPartial = Path.Combine(projectDirectory, BomberLandPythonFolder);
            this._numberBattleSimulations = numberBattleSimulations;
            this._numThreads = numThreads;
            this._dockerContainersAvailable = new ConcurrentQueue<int>(Enumerable.Range(0, this._numThreads));
        }

        private string GetResultsPath(int dockerContainerNumber)
        {
            return Path.Combine(this.GetMyPath(dockerContainerNumber), LogFile);
        }
        
        private string GetMyPath(int dockerContainerNumber)
        {
            return Utilities.AppendNumberToFileName(this._pathPartial, dockerContainerNumber);
        }

        private string GetMyTreeToCodePath(int dockerContainerNumber)
        {
            return Path.Combine(this.GetMyPath(dockerContainerNumber), TreeToCodeFile);
        }

        private string GetRetrieveFitnessPath(int dockerContainerNumber)
        {
            return Path.Combine(this.GetMyPath(dockerContainerNumber), RetrieveFitnessFile);
        }

        private string GetRandomSeedPath(int dockerContainerNumber)
        {
            return Path.Combine(this.GetMyPath(dockerContainerNumber), RandomSeedFile);
        }
        
        
        private IEnumerable<(int, int)> GetBattlePairings(List<Individual> population)
        {
            var pairings = new List<(int, int)>();

            for (int i = 0; i < population.Count; i++)
            {
                for (int j = 0; j < population.Count; j++)
                {
                    if (i < j)
                    {
                        for (int k = 0; k < this._numberBattleSimulations; k++)
                        {
                            pairings.Add((i, j));
                        }
                    }
                }
            }

            return pairings;
        }
        
        private async Task RunAllBattles(IEnumerable<(int, int)> allBattlePairings, List<Individual> population)
        {
            var allTasks = allBattlePairings.Select(pairing => 
                this.SimulateBattle(pairing.Item1, pairing.Item2, population));
            await Task.WhenAll(allTasks);
        }

        public async Task<List<FitnessBase>> GetFitnessOfPopulationUsingCoevolutionAsync(GpRunner? gp,
            List<Individual> population)
        {

            var allBattlePairings = this.GetBattlePairings(population);

            return await GetFitnessOfPopulationUsingCoevolutionAsync(gp, allBattlePairings, population);
        } 
        
        public async Task<List<FitnessBase>> GetFitnessOfPopulationUsingCoevolutionAsync(GpRunner? gp, 
            IEnumerable<(int, int)> battlePairings, List<Individual> population)
        {
            this.VictoriesMatrix ??= new int[population.Count, population.Count];
            this.TurnsToLoseMatrix ??= new double[population.Count, population.Count];
            this.TurnsToWinMatrix ??= new double[population.Count, population.Count];

            // var hack = Enumerable.Range(0, population.Count)
            //     .Select(i => new FitnessLexicographic(new List<double>() { 0, 0, 0, 0, 0 },
            //         MinMaxMapping))
            //     .Cast<FitnessBase>();
            // return hack.ToList();

            
            await this.RunAllBattles(battlePairings, population);
            
            var results = Enumerable.Range(0, population.Count)
                .Select(i => this.GetResultVector(i, population))
                .Select(f => new FitnessLexicographic(f, MinMaxMapping))
                .Cast<FitnessBase>()
                .ToList();
            
            return results;
        } 
        

        public async Task SimulateBattle(
            int myIndividualIndex,
            int otherIndividualIndex,
            List<Individual> population)
        {
            int dockerContainerNumber;
            while (!this._dockerContainersAvailable.TryDequeue(out dockerContainerNumber))
            {
            }

            Console.WriteLine($"Simulating battle Individual {myIndividualIndex} vs Individual {otherIndividualIndex}");
            Individual myIndividual = population[myIndividualIndex];
            Individual otherIndividual = population[otherIndividualIndex];

            TryCastIndividualGenomeToCorrectType(otherIndividual, out var otherGenome);
            TryCastIndividualGenomeToCorrectType(myIndividual, out var myGenome);
            
            var myBehavior = myGenome.ToString();
            var otherBehavior = otherGenome.ToString();

            var invalidFloatRegex = new Regex(@"0,\d+,");
            Debug.Assert(!invalidFloatRegex.IsMatch(myBehavior));
            Debug.Assert(!invalidFloatRegex.IsMatch(otherBehavior));
            
            await this.GetAndAddResultsToMatrices(myBehavior, otherBehavior, myIndividualIndex, otherIndividualIndex, dockerContainerNumber);

            this._dockerContainersAvailable.Enqueue(dockerContainerNumber);
        }

        public void PostFitnessEvaluationFunction()
        {
            this.ThrowExceptionIfAnyMatrixIsNull();
            
            lock (this.VictoriesMatrix!.SyncRoot) 
            lock (this.TurnsToLoseMatrix!.SyncRoot)
            lock (this.TurnsToWinMatrix!.SyncRoot)
            {

                // Debug.Assert(
                //     Utilities.SumMirroredAcrossDiagonalIsSameAs(
                //         this.VictoriesMatrix,
                //         this._numberBattleSimulations));
                // Debug.Assert(Utilities.AreFrequenciesTheSamePerColumn(this.TurnsToLoseMatrix, this.TurnsToWinMatrix));

                // TODO rewrite this because it doesn't seem like good style for this class to store the current fitness values
                this.VictoriesMatrix = null;
                this.TurnsToLoseMatrix = null;
                this.TurnsToWinMatrix = null;
            }
        }

        public void PostGenerationFunction(ref GpExperimentProgress gpExperimentProgress)
        {
            // TODO would ideally have this be a enabled by verbosity levels in STGP-Sharp
            Console.WriteLine($"Generation {gpExperimentProgress.generationsInRunCompleted}/" +
                              $"{gpExperimentProgress.generationsInRunCount} complete\n");
            Console.WriteLine("============================================================================================\n\n");
        }

        private static void TryCastIndividualGenomeToCorrectType(Individual i, out BomberLandAgentBehavior genome)
        {
            genome = i.genome as BomberLandAgentBehavior
                ?? throw new Exception($"Genome is not of type {nameof(GpBuildingBlock<BomberLandAgentAction.BomberLandAgentActionEnum>)}");
        }

        private List<double> GetResultVector(int myIndividualIndex, List<Individual> population)
        {
            this.ThrowExceptionIfAnyMatrixIsNull();
            double totalVictories = 0;
            double averageTurnsToLose = 0;
            double averageTurnsToWin = 0;
            double numberAlwaysLose = 0;
            double numberAlwaysWin = 0;
            
            lock (this.VictoriesMatrix!.SyncRoot)
            lock (this.TurnsToLoseMatrix!.SyncRoot)
            lock (this.TurnsToWinMatrix!.SyncRoot)
            {
                for (int i = 0; i < population.Count; i++)
                {
                    if (myIndividualIndex == i) continue;
                    
                    var victories = this.VictoriesMatrix![myIndividualIndex, i];
                    totalVictories += victories;
                    numberAlwaysLose += victories == 0 ? 1 : 0;
                    numberAlwaysWin += victories == this._numberBattleSimulations ? 1 : 0;
                    averageTurnsToLose += this.TurnsToLoseMatrix![myIndividualIndex, i];
                    averageTurnsToWin += this.TurnsToWinMatrix![myIndividualIndex, i];
                }
            }


            var numBattles = this._numberBattleSimulations * (population.Count - 1);
            averageTurnsToLose /= numBattles;
            averageTurnsToWin /= numBattles;
            
            return new List<double>
            {
                totalVictories, 
                numberAlwaysLose, 
                averageTurnsToWin,
                numberAlwaysWin,
                averageTurnsToLose
            };
        }

        private async Task GetAndAddResultsToMatrices(
            string myBehavior, string otherBehavior, 
            int myIndividualIndex, int otherIndividualIndex,
            int dockerContainerNumber)
        {
            this.ThrowExceptionIfAnyMatrixIsNull();
            
            var fitnessResultsAsString = await this.GetResultsFromBomberLandRun(myBehavior, otherBehavior, dockerContainerNumber);
            var results = ParseResults(fitnessResultsAsString, myIndividualIndex, otherIndividualIndex);

            if (results.IndexOfWhoLost == null || results.IndexOfWhoWon == null)
            {
                
            }
            else if (otherIndividualIndex != myIndividualIndex)
            {
                Debug.Assert(results.IndexOfWhoWon != results.IndexOfWhoLost);
            }
            else
            {
                Debug.Assert(results.IndexOfWhoLost == results.IndexOfWhoWon);
            }
            
            this.AddResultsToMatrices(results);
        }

        internal void AddResultsToMatrices(ParsedResults results)
        {
            if (results.IndexOfWhoLost == null || results.IndexOfWhoWon == null) return;
            this.ThrowExceptionIfAnyMatrixIsNull();
            
            lock (this.VictoriesMatrix!.SyncRoot)
            lock (this.TurnsToLoseMatrix!.SyncRoot)
            lock (this.TurnsToWinMatrix!.SyncRoot)
            {
                this.VictoriesMatrix![(int)results.IndexOfWhoWon, (int)results.IndexOfWhoLost]++;
                this.TurnsToWinMatrix![(int)results.IndexOfWhoWon, (int)results.IndexOfWhoLost] += results.Turns;
                this.TurnsToLoseMatrix![(int)results.IndexOfWhoLost, (int)results.IndexOfWhoWon] += results.Turns;
            }
        }
        
        internal static ParsedResults ParseResults(string fitnessResultsAsString, int myIndividualIndex, int otherIndividualIndex)
        {
            var fitnessResults = fitnessResultsAsString.Split(' ');
            var winner = fitnessResults[0];
            var turns = int.Parse(fitnessResults[1]);
            
            Debug.Assert(turns > 0);

            ParsedResults results = winner switch
            {
                "None" => new ParsedResults(),
                "a" => new ParsedResults()
                {
                    Turns = turns, IndexOfWhoWon = myIndividualIndex, IndexOfWhoLost = otherIndividualIndex
                },
                "b" => new ParsedResults()
                {
                    Turns = turns, IndexOfWhoWon = otherIndividualIndex, IndexOfWhoLost = myIndividualIndex
                },
                _ => throw new Exception($"Winner was neither a, b or None, Winner was {winner}")
            };
            
            Console.WriteLine($"Winner = {results.IndexOfWhoWon}, Loser = {results.IndexOfWhoLost}, Turns = {turns}");

            return results;
        }
        
        private void ThrowExceptionIfAnyMatrixIsNull()
        {
            if (null == this.VictoriesMatrix) throw new Exception($"{nameof(this.VictoriesMatrix)} is undefined");
            if (null == this.TurnsToLoseMatrix)
                throw new Exception($"{nameof(this.TurnsToLoseMatrix)} is undefined");
            if (null == this.TurnsToWinMatrix) throw new Exception($"{nameof(this.TurnsToWinMatrix)} is undefined");
        }

        private async Task<string> GetResultsFromBomberLandRun(string behavior, string otherBehavior, int dockerContainerNumber)
        {

            await this.RunRandomSeed(dockerContainerNumber);
            await this.RunTreeToCode(behavior, otherBehavior, dockerContainerNumber);
            await this.RunBomberLandDocker(dockerContainerNumber);
            await this.RunRetrieveFitness(dockerContainerNumber);
            string results = this.GetResultsFromLog(dockerContainerNumber);
            await File.WriteAllTextAsync(this.GetResultsPath(dockerContainerNumber), String.Empty);
            return results;
        }

        private string GetResultsFromLog(int dockerContainerNumber)
        {
            string[] lines = File.ReadAllLines(this.GetResultsPath(dockerContainerNumber));
            return lines[0];
        }

        private async Task RunBomberLandDocker(int dockerContainerNumber)
        {
            try
            {
                // var args = new [] { "up", "--build", "--abort-on-container-exit", "--force-recreate" };
                // await Cli.Wrap("docker-compose")
                //     .WithArguments(args)
                //     .WithWorkingDirectory(this.GetMyPath(dockerContainerNumber))
                //     .ExecuteBufferedAsync();

                var args = new[] { "up", "--build", "--abort-on-container-exit", "--force-recreate" };
                var cmd = Cli.Wrap("docker-compose")
                    .WithArguments(args)
                    .WithWorkingDirectory(this.GetMyPath(dockerContainerNumber));

                // .ExecuteBufferedAsync();

                // await cmd;

                // Console.WriteLine("Docker Output:");
                await foreach (var cmdEvent in cmd.ListenAsync())
                {
                    switch (cmdEvent)
                    {
                        case StandardOutputCommandEvent stdOut:
                            // Console.WriteLine($"Out> {stdOut.Text}");
                            break;
                        case StandardErrorCommandEvent stdErr:
                            // Console.WriteLine($"Err> {stdErr.Text}");
                            break;
                    }
                }
            }
            catch (CliWrap.Exceptions.CommandExecutionException e)
            {
                // Console.WriteLine(e);
                Console.WriteLine($"Exit code: {e.ExitCode}");
                // Console.WriteLine(e.Data.);
                Console.WriteLine(e.Message);
                Console.WriteLine($"Docker container number = {dockerContainerNumber}");
                
                throw;
            }

        }

        private async Task RunTreeToCode(string behavior, string otherBehavior, int dockerContainerNumber)
        {
            var args = new[] { this.GetMyTreeToCodePath(dockerContainerNumber), behavior, otherBehavior };
            var cmd = Cli.Wrap("python2")
                .WithArguments(args)
                .WithWorkingDirectory(this.GetMyPath(dockerContainerNumber));
                // .ExecuteBufferedAsync();
            
            await foreach (var cmdEvent in cmd.ListenAsync())
            {
                switch (cmdEvent)
                {
                    case StandardOutputCommandEvent stdOut:
                        // Console.WriteLine($"Out> {stdOut.Text}");
                        break;
                    case StandardErrorCommandEvent stdErr:
                        // Console.WriteLine($"Err> {stdErr.Text}");
                        break;
                    // case ExitedCommandEvent exitedCommandEvent:
                    //     if (exitedCommandEvent.ExitCode > 0)
                    //         throw new Exception($"Exited with error code {exitedCommandEvent.ExitCode}");
                    //     break;
                }
            }
        }

        private async Task RunRetrieveFitness(int dockerContainerNumber)
        {
            var args = new[] { this.GetRetrieveFitnessPath(dockerContainerNumber) };
            var cmd = Cli.Wrap("python2")
                .WithArguments(args)
                .WithWorkingDirectory(this.GetMyPath(dockerContainerNumber));
                // .ExecuteBufferedAsync();
            
            await foreach (var cmdEvent in cmd.ListenAsync())
            {
                switch (cmdEvent)
                {
                    case StandardOutputCommandEvent stdOut:
                        // Console.WriteLine($"Out> {stdOut.Text}");
                        break;
                    case StandardErrorCommandEvent stdErr:
                        // Console.WriteLine($"Err> {stdErr.Text}");
                        break;
                    // case ExitedCommandEvent exitedCommandEvent:
                    //     if (exitedCommandEvent.ExitCode > 0)
                    //         throw new Exception($"Exited with error code {exitedCommandEvent.ExitCode}");
                    //     break;
                }
            }
        }
        
        
          
        private async Task RunRandomSeed(int dockerContainerNumber)
        {
            // var args = new[] { this.GetRandomSeedPath(dockerContainerNumber) };
            // var result = await Cli.Wrap("python2")
            //     .WithArguments(args)
            //     .WithWorkingDirectory(this.GetMyPath(dockerContainerNumber))
            //     .ExecuteBufferedAsync();
            //
            // if (result.ExitCode > 0)
            // {
            //     throw new Exception($"Exited with exit code {result.ExitCode}");
            // }
            //
            var args = new [] { this.GetRandomSeedPath(dockerContainerNumber) };
            var cmd = Cli.Wrap("python2")
                .WithArguments(args)
                .WithWorkingDirectory(this.GetMyPath(dockerContainerNumber));
                // .ExecuteBufferedAsync();
            // await cmd;

            // Console.WriteLine("Docker Output:");
            await foreach (var cmdEvent in cmd.ListenAsync())
            {
                switch (cmdEvent)
                {
                    case StandardOutputCommandEvent stdOut:
                        Console.WriteLine($"Out> {stdOut.Text}");
                        break;
                    case StandardErrorCommandEvent stdErr:
                        Console.WriteLine($"Err> {stdErr.Text}");
                        break;
                    // case ExitedCommandEvent exitedCommandEvent:
                    //     if (exitedCommandEvent.ExitCode > 0)
                    //         throw new Exception($"Exited with error code {exitedCommandEvent.ExitCode}");
                    //     break;
                }
            }
        }
    }
}

