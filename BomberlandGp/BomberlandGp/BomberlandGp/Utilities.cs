
using BomberLandGp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STGP_Sharp.Fitness;
using STGP_Sharp.Fitness.Fitness_Stats;
using STGP_Sharp.GpBuildingBlockTypes;

#nullable enable

namespace BomberLandGp
{
    public static class Utilities
    {
        public static float ShiftNumberIntoRange(
            float x,
            float newMin, float newMax,
            float oldMin = 0.0f, float oldMax = 1.0f)
        {
            if (Math.Abs(oldMin - oldMax) < 0.0001)
                throw new Exception($"Old min ({oldMin}) must not equal old max ({oldMax})");

            // Source: https://math.stackexchange.com/questions/914823/shift-numbers-into-a-different-range
            return newMin + (newMax - newMin) / (oldMax - oldMin) * (x - oldMin);
        }

        public static bool IsTranspose(double[,] matrix1, double[,] matrix2)
        {
            int nRows = matrix1.GetLength(0);
            int nColumns = matrix1.GetLength(1);
            if (matrix2.GetLength(0) != nRows ||
                matrix2.GetLength(1) != nColumns ||
                nRows != nColumns)
            {
                throw new Exception("Matrices must be square and have the same dimensions");
            }

            for (var r = 0; r < nRows; r++)
            {
                for (var c = 0; c < nRows; c++)
                {
                    if (Math.Abs(matrix1[r, c] - matrix2[c, r]) > 0.000001) return false;
                }
            }

            return true;
        }

        public static bool SumMirroredAcrossDiagonalIsSameAs(int[,] matrix, int n)
        {
            int nRows = matrix.GetLength(0);
            int nColumns = matrix.GetLength(1);
            if (nRows != nColumns)
            {
                throw new Exception("Matrix must be square");
            }

            if (nRows <= 1) return true;

            for (var r = 0; r < nRows; r++)
            {
                for (var c = 0; c < nRows; c++)
                {
                    if (r == c) continue;
                    if (matrix[r, c] + matrix[c, r] != n) return false;
                }
            }

            return true;
        }
        
        public static void Print2DArray<T>(T[,] matrix)
        {
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    var n = matrix[i, j];
                    if (n is double nDouble && double.IsInfinity(nDouble))
                    {
                        Console.Write("Inf" + "\t");
                    }
                    else
                    {
                        Console.Write($"{matrix[i, j]:0.##}" + "\t");
                    }
                    
                }
                Console.WriteLine("\n");
            }
        }

        public static Dictionary<T, int> GetFrequencyTable<T>(T[] values) where T : notnull
        {
            return values.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
        }

        public static bool AreFrequenciesTheSamePerColumn(double[,] matrix1, double[,] matrix2) 
        {
            var nColumns = matrix1.GetLength(1);
            var nRows = matrix1.GetLength(0);
            if (nColumns != matrix2.GetLength(1) || nRows != matrix2.GetLength(0))
            {
                throw new Exception("Matrices must have the same dimensions and be square");
            }
            
            for (int c = 0; c < nColumns; c++)
            {
                var valuesInColumnC1 = new double[nRows];
                var valuesInColumnC2 = new double[nRows];

                for (int r = 0; r < nRows; r++)
                {
                    valuesInColumnC1[r] = matrix1[r, c];
                    valuesInColumnC2[r] = matrix2[r, c];
                }

                var frequencies1 = GetFrequencyTable(valuesInColumnC1);
                var frequencies2 = GetFrequencyTable(valuesInColumnC1);

                if (frequencies1.Count != frequencies2.Count || frequencies1.Except(frequencies2).Any())
                    return false;
            }

            return true;
        }

        public static double SumMatrix(double[,] matrix) 
        {
            double sum = 0;
            for (int r = 0; r < matrix.GetLength(0); r++)
            {
                for (int c = 0; c < matrix.GetLength(1); c++)
                {
                    double cell = matrix[r, c];
                    sum += double.IsInfinity(cell) ? 0 : cell;
                }
            }

            return sum;
        }

        public static int NumInfinitiesInMatrix(double[,] matrix)
        {
            int sum = 0;
            for (int r = 0; r < matrix.GetLength(0); r++)
            {
                for (int c = 0; c < matrix.GetLength(1); c++)
                {
                    if (double.IsInfinity(matrix[r, c])) sum++;
                }
            }

            return sum;
        }
    
        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize) 
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        public static string AppendNumberToFileName(string path, int number)
        {
            string dir = Path.GetDirectoryName(path) ?? throw new Exception("File not found -- Directory");
            string fileName = Path.GetFileNameWithoutExtension(path) ?? throw new Exception("File not found -- Name");
            string fileExt = Path.GetExtension(path) ?? throw new Exception("File not found -- Extension");
            
            return Path.Combine(dir, fileName + number + fileExt);
        }
        
        
        public static IEnumerable<List<int>>
            GetPermutations(List<int> list, int length)
        {
            if (length == 1) return list.Select(t => new List<int> { t });

            return GetPermutations(list, length - 1)
                .SelectMany(t =>
                        list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new[] { t2 })
                        .ToList());
        }
    }
}
public class FitnessConverter : JsonCreationConverter<FitnessBase>
{
    protected override FitnessBase Create(Type objectType, JObject jObject)
    {
        return jObject.ToObject<FitnessLexicographic>() ?? throw new NullReferenceException();

        // return new FitnessLexicographic();
    }

    private bool FieldExists(string fieldName, JObject jObject)
    {
        return jObject[fieldName] != null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class AttributeConverter : JsonCreationConverter<GpBuildingBlock<BomberLandAgentAttribute>>
{
    protected override GpBuildingBlock<BomberLandAgentAttribute> Create(Type objectType, JObject jObject)
    {
        return jObject.ToObject<BomberLandAgentAttributeConstant>() ?? throw new NullReferenceException();

        // return new FitnessLexicographic();
    }

    private bool FieldExists(string fieldName, JObject jObject)
    {
        return jObject[fieldName] != null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}


public class DetailedSummaryConverter : JsonCreationConverter<GpResultsStatsBase.DetailedSummary>
{
    protected override GpResultsStatsBase.DetailedSummary Create(Type objectType, JObject jObject)
    {
        return jObject.ToObject<GpResultsStatsLexicographic.DetailedSummaryLexicographic>();
        // return new GpResultsStatsLexicographic.DetailedSummaryLexicographic();
    }

    private bool FieldExists(string fieldName, JObject jObject)
    {
        return jObject[fieldName] != null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}


public abstract class JsonCreationConverter<T> : JsonConverter
{
    /// <summary>
    /// Create an instance of objectType, based properties in the JSON object
    /// </summary>
    /// <param name="objectType">type of object expected</param>
    /// <param name="jObject">
    /// contents of JSON object that will be deserialized
    /// </param>
    /// <returns></returns>
    protected abstract T Create(Type objectType, JObject jObject);

    public override bool CanConvert(Type objectType)
    {
        return typeof(T).IsAssignableFrom(objectType);
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override object ReadJson(JsonReader reader, 
        Type objectType, 
        object existingValue, 
        JsonSerializer serializer)
    {
        // Load JObject from stream
        JObject jObject = JObject.Load(reader);

        // Create target object based on JObject
        T target = Create(objectType, jObject);

        // Populate the object properties
        serializer.Populate(jObject.CreateReader(), target);

        return target;
    }
}