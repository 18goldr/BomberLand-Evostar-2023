using NUnit.Framework;
using NUnit.Framework.Internal;

namespace BomberLandGp.Tests;

[TestFixture]
public static class LexicographicFitnessFunctionTest
{
    public struct ExpectedAverageResults
    {
        public double avgTurnsForMeToLose;
        public double avgTurnsForOtherToLose;
        public double avgTurnsForMeToWin;
        public double avgTurnsForOtherToWin;
        public int myVictories;
        public int otherVictories;
    }

    public struct InputMatrices
    {
        public int[,] victoriesMatrix;
        public double[,] turnsToLoseMatrix;
        public double[,] turnsToWinMatrix;
    }

    public static readonly object[] TestCasesForAverages =
    {
        new object[]
        {
            new LexicographicVictoriesTurnsToWinTurnsToLose("TEST", 1, 1),
            0, 1,
            new InputMatrices()
            {
                victoriesMatrix = new int[,]
                {
                    { 0, 1 },
                    { 0, 0 }
                },
                turnsToLoseMatrix = new double[,]
                {
                    { 0, 0 },
                    { 52, 0 }
                },
                turnsToWinMatrix = new double[,]
                {
                    { 0, 52 },
                    { 0, 0 }
                }
            },
            new ExpectedAverageResults()
            {
                avgTurnsForMeToWin = 52,
                avgTurnsForMeToLose = 0,
                avgTurnsForOtherToWin = 0,
                avgTurnsForOtherToLose = 52,
                myVictories = 1,
                otherVictories = 0
                
            }
        },
        new object[]
        {
            new LexicographicVictoriesTurnsToWinTurnsToLose("TEST", 2, 1),
            0, 1,
            new InputMatrices()
            {
                victoriesMatrix = new int[,]
                {
                    { 0, 1 },
                    { 1, 0 }
                },
                turnsToLoseMatrix = new double[,]
                {
                    { 0, 48 },
                    { 52, 0 }
                },
                turnsToWinMatrix = new double[,]
                {
                    { 0, 52 },
                    { 48, 0 }
                }
            },
            new ExpectedAverageResults()
            {
                avgTurnsForMeToWin = 52,
                avgTurnsForMeToLose = 48,
                avgTurnsForOtherToWin = 48,
                avgTurnsForOtherToLose = 52,
                myVictories = 1,
                otherVictories = 1
            }
        },
        new object[]
        {
            new LexicographicVictoriesTurnsToWinTurnsToLose("TEST", 5, 1),
            1, 2,
            new InputMatrices()
            {
                victoriesMatrix = new int[,]
                {
                    { 0, 0, 3},
                    { 5, 0, 4},
                    { 2, 1, 0}
                },
                turnsToLoseMatrix = new double[,]
                {
                    { 0,   500, 200},
                    { 0,   0,   100},
                    { 300, 400, 0}
                },
                turnsToWinMatrix = new double[,]
                {
                    { 0,   0,   300},
                    { 500, 0,   400},
                    { 200, 100, 0}
                }
            },
            new ExpectedAverageResults()
            {
                avgTurnsForMeToWin = 100,
                avgTurnsForMeToLose = 100,
                avgTurnsForOtherToWin = 100,
                avgTurnsForOtherToLose = 100,
                myVictories = 4,
                otherVictories = 1
            }
        }
    };
    
    public static readonly object[] TestCasesForParseAndMatrices =
    {
        new object[]
        {
            new LexicographicVictoriesTurnsToWinTurnsToLose("TEST", 1, 1),
            "a 52",
            new LexicographicVictoriesTurnsToWinTurnsToLose.ParsedResults
            {
                Turns = 52,
                IndexOfWhoLost = 1,
                IndexOfWhoWon = 0
            },
            0, 1,
            new InputMatrices
            {
                victoriesMatrix = new int[,]
                {
                    { 0, 0 },
                    { 0, 0 }
                },
                turnsToLoseMatrix = new double[,]
                {
                    { 0, 0 },
                    { 0, 0 }
                },
                turnsToWinMatrix = new double[,]
                {
                    { 0, 0 },
                    { 0, 0 }
                }
            },
            new InputMatrices
            {
                victoriesMatrix = new int[,]
                {
                    { 0, 1 },
                    { 0, 0 }
                },
                turnsToLoseMatrix = new double[,]
                {
                    { 0,  0 },
                    { 52, 0 }
                },
                turnsToWinMatrix = new double[,]
                {
                    { 0, 52 },
                    { 0, 0 }
                }
            }
        },
        new object[]
        {
            new LexicographicVictoriesTurnsToWinTurnsToLose("TEST", 1, 1),
            "b 52",
            new LexicographicVictoriesTurnsToWinTurnsToLose.ParsedResults
            {
                Turns = 52,
                IndexOfWhoLost = 0,
                IndexOfWhoWon = 1
            },
            0, 1,
            new InputMatrices
            {
                victoriesMatrix = new int[,]
                {
                    { 0, 0 },
                    { 0, 0 }
                },
                turnsToLoseMatrix = new double[,]
                {
                    { 0, 0 },
                    { 0, 0 }
                },
                turnsToWinMatrix = new double[,]
                {
                    { 0, 0 },
                    { 0, 0 }
                }
            },
            new InputMatrices
            {
                victoriesMatrix = new int[,]
                {
                    { 0, 0 },
                    { 1, 0 }
                },
                turnsToLoseMatrix = new double[,]
                {
                    { 0, 52 },
                    { 0, 0 }
                },
                turnsToWinMatrix = new double[,]
                {
                    { 0,  0 },
                    { 52, 0 }
                }
            }
        }
    };

    
    [TestCaseSource(typeof(LexicographicFitnessFunctionTest), nameof(TestCasesForParseAndMatrices))]
    public static void TestParseAndAddResultsToMatrices(
        LexicographicVictoriesTurnsToWinTurnsToLose fitnessFunction,
        string resultsString,
        LexicographicVictoriesTurnsToWinTurnsToLose.ParsedResults expectedParsedResults,
        int myIndividualIndex, int otherIndividualIndex,
        InputMatrices inputMatrices,
        InputMatrices expectedMatrixResults)
    {
        var results = LexicographicVictoriesTurnsToWinTurnsToLose.ParseResults(resultsString, myIndividualIndex, otherIndividualIndex);
        
        Assert.That(results.Turns, Is.EqualTo(expectedParsedResults.Turns));
        Assert.That(results.IndexOfWhoLost, Is.EqualTo(expectedParsedResults.IndexOfWhoLost));
        Assert.That(results.IndexOfWhoWon, Is.EqualTo(expectedParsedResults.IndexOfWhoWon));

        fitnessFunction.VictoriesMatrix = inputMatrices.victoriesMatrix;
        fitnessFunction.TurnsToLoseMatrix = inputMatrices.turnsToLoseMatrix;
        fitnessFunction.TurnsToWinMatrix = inputMatrices.turnsToWinMatrix;
        
        fitnessFunction.AddResultsToMatrices(results);
        
        Assert.That(
            fitnessFunction.VictoriesMatrix![myIndividualIndex, otherIndividualIndex],
            Is.EqualTo(expectedMatrixResults.victoriesMatrix![myIndividualIndex, otherIndividualIndex]));
        Assert.That(
            fitnessFunction.TurnsToLoseMatrix![myIndividualIndex, otherIndividualIndex],
            Is.EqualTo(expectedMatrixResults.turnsToLoseMatrix![myIndividualIndex, otherIndividualIndex])
                .Within(0.00001));
        Assert.That(
            fitnessFunction.TurnsToWinMatrix![myIndividualIndex, otherIndividualIndex],
            Is.EqualTo(expectedMatrixResults.turnsToWinMatrix![myIndividualIndex, otherIndividualIndex])
                .Within(0.00001));
            
        Assert.That(
            fitnessFunction.VictoriesMatrix![otherIndividualIndex, myIndividualIndex],
            Is.EqualTo(expectedMatrixResults.victoriesMatrix![otherIndividualIndex, myIndividualIndex]));
        Assert.That(
            fitnessFunction.TurnsToLoseMatrix![otherIndividualIndex, myIndividualIndex],
            Is.EqualTo(expectedMatrixResults.turnsToLoseMatrix![otherIndividualIndex, myIndividualIndex])
                .Within(0.00001));
        Assert.That(
            fitnessFunction.TurnsToWinMatrix![otherIndividualIndex, myIndividualIndex],
            Is.EqualTo(expectedMatrixResults.turnsToWinMatrix![otherIndividualIndex, myIndividualIndex])
                .Within(0.00001));

    }
}