using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GP;
using STGP_Sharp.Fitness;
using STGP_Sharp.Fitness.Sample_Fitness_Functions;
using STGP_Sharp.GpBuildingBlockTypes;
using STGP_Sharp.Utilities.GeneralCSharp;

namespace STGP_Sharp
{
    public static class SampleExperiments
    {
        public static async Task SampleExperiment()
        {
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            
            var gpParams = new GpPopulationParameters(
                probabilityDistribution: new ProbabilityDistribution(new List<Type>
                {
                    typeof(And), typeof(Not), typeof(BooleanPositionalArgument)
                }),
                maxDepth: 4
            );

            var gpRunner = new GpRunner(
                new EquivalentToOr(),
                gpParams,
                typeof(bool),
                new TimeoutInfo() { cancelTokenSource = new CancellationTokenSource() },
                positionalArguments: new PositionalArguments()
            );

            var results = await gpRunner.RunAsync();
            CustomPrinter.PrintLine(results.bestEver?.genome);
            CustomPrinter.PrintLine(results.bestEver?.fitness?.ToString());
        }
        
        public static void Test()
        {
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            
            var gpParams = new GpPopulationParameters(
                probabilityDistribution: new ProbabilityDistribution(new List<Type>
                {
                    typeof(And), typeof(Not), typeof(BooleanPositionalArgument)
                }),
                maxDepth: 3
            );
            
            var gpRunner = new GpRunner(
                new EquivalentToOr(),
                gpParams,
                typeof(bool),
                new TimeoutInfo() { cancelTokenSource = new CancellationTokenSource() },
                positionalArguments: new PositionalArguments()
            );


            var i = new Individual(new Not(
                new And(
                    new Not(new BooleanPositionalArgument(0)),
                    new Not(new BooleanPositionalArgument(1)))));
            var fitnessFunction = new EquivalentToOr();
            
            CustomPrinter.PrintLine(fitnessFunction.GetFitnessOfIndividual(gpRunner, i));
        }

        static async Task Main(string[] args)
        {
            Test();
            await SampleExperiment();

            Console.ReadLine();
        }
    }
}