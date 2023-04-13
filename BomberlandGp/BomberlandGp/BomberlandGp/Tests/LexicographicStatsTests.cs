using NUnit.Framework;
using STGP_Sharp.Utilities.GeneralCSharp;

namespace BomberLandGp.Tests;

[TestFixture]
public static class LexicographicStatsTests
{
    private static readonly List<List<double>> FitnessValues = new List<List<double>>
    {
        new List<double> { 5,  4, 2 },
        new List<double> { 0,  1, 0 },
        new List<double> { 10, 0, 0 },
        new List<double> { 0,  0, 0 }
    };

    private static readonly object[] TestCases =
    {
        new object[]
        {
            FitnessValues, 
            0,
            new Dictionary<double, int>
            {
                {5, 1},
                {0, 2},
                {10, 1}
            },
            new SimpleStats(new double[] { 5, 0, 10, 0 }).GetSummary()
        },
        new object[]
        {
            FitnessValues, 
            1,
            new Dictionary<double, int>
            {
                {4, 1},
                {0, 2},
                {1, 1}
            },
            new SimpleStats(new double[] { 4, 1, 0, 0 }).GetSummary()
        },
        new object[]
        {
            FitnessValues, 
            2, 
            new Dictionary<double, int>
            {
                {2, 1},
                {0, 3}
            }, 
            new SimpleStats(new double[] { 2, 0, 0, 0 }).GetSummary()
        }
    };
    
    [TestCaseSource(typeof(LexicographicStatsTests), nameof(TestCases))]
    public static void TestGetSummaryAndFrequenciesForDimension(
        List<List<double>> fitnessList, 
        int dimension,
        Dictionary<double, int> expectedFrequencies,
        SimpleStats.Summary expectedSummary)
    {
        (SimpleStats.Summary summary, Dictionary<double, int> frequencies) =
            GpResultsStatsLexicographic.DetailedSummaryLexicographic.GetSummaryAndFrequencyPerDimension(
                fitnessList, dimension);
        
        Assert.That(summary.max, Is.EqualTo(expectedSummary.max).Within(0.00001));
        Assert.That(summary.mean, Is.EqualTo(expectedSummary.mean).Within(0.00001));
        Assert.That(summary.min, Is.EqualTo(expectedSummary.min).Within(0.000001));
        Assert.That(summary.variance, Is.EqualTo(expectedSummary.variance).Within(0.00001));
        Assert.That(summary.standardDeviation, Is.EqualTo(expectedSummary.standardDeviation).Within(0.00001));
        Assert.That(summary.numSamples == expectedSummary.numSamples);
        
        // Check the two dictionaries are equal
        Assert.That(frequencies.Count, Is.EqualTo(expectedFrequencies.Count));
        foreach (double key in frequencies.Keys)
        {
            Assert.That(frequencies[key], Is.EqualTo(expectedFrequencies[key]));
        }
    }
    
}