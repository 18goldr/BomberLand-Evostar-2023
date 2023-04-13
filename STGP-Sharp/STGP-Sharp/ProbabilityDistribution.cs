#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GP;
using STGP_Sharp.Utilities.GeneralCSharp;
using Newtonsoft.Json;

namespace STGP_Sharp
{
    public class TypeProbability
    {
        public double probability; // TODO force min value

        public Type type;

        public TypeProbability(Type type, double prob)
        {
            this.type = type;
            probability = prob;
        }

        public TypeProbability()
        {
            type = ProbabilityDistribution.ValidTypes.First();
            probability = 1;
        }

        
        public string TypeToString
        {
            get => type.Name;
            set { type = ProbabilityDistribution.ValidTypes.First(t => t.Name == value); }
        }
    }

    public class ProbabilityDistribution // TODO inherit from dictionary?
    {
        public static readonly IEnumerable<Type> ValidTypes =
            STGP_Sharp.GpRunner.GetSubclassesOfGpBuildingBlock(true);

        public readonly List<TypeProbability> distribution;

        public ProbabilityDistribution(IEnumerable<TypeProbability> distribution)
        {
            this.distribution = distribution.ToList();

            AddDefaultUnspecifiedTypeProbabilities();
        }

        public ProbabilityDistribution(List<Type> types)
        {
            distribution = new List<TypeProbability>(types.Count);

            foreach (var type in types) distribution.Add(new TypeProbability(type, 1));

            AddDefaultUnspecifiedTypeProbabilities();
        }

        public IEnumerable<Type> GetTypesWithProbabilityGreaterThanZero()
        {
            return distribution.Where(tp => tp.probability > 0).Select(tp => tp.type);
        }

        public double? GetProbabilityOfType(Type t)
        {
            return distribution.FirstOrDefault(tp => tp.type == t)?.probability;
        }

        public void AddDefaultUnspecifiedTypeProbabilities()
        {
            var typesOnly = distribution.Select(tp => tp.type).ToList();
            foreach (var type in ValidTypes)
                if (!typesOnly.Contains(type))
                    distribution.Add(new TypeProbability(type, 0));
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static ProbabilityDistribution? Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<ProbabilityDistribution>(json);
        }

        public static ProbabilityDistribution GetProbabilityDistributionFromFile(string file)
        {
            file = GeneralCSharpUtilities.GetRelativePath(file);
            return Deserialize(File.ReadAllText(file)) ??
                   throw new Exception($"File {file} does not contain a probability distribution.");
        }

        public void WriteToFile(string file)
        {
            file = GeneralCSharpUtilities.GetRelativePath(file);
            File.WriteAllText(file, Serialize());
        }

        public Dictionary<Type, double> ToDictionary()
        {
            return distribution.ToDictionary(
                tp => tp.type,
                tp => tp.probability
            );
        }

        public static ProbabilityDistribution FromDictionary(Dictionary<Type, double> d)
        {
            var typeProbabilities = new List<TypeProbability>(d.Count);
            typeProbabilities.AddRange(
                d.Select(kvp =>
                    new TypeProbability(kvp.Key, kvp.Value)));

            return new ProbabilityDistribution(typeProbabilities);
        }
    }
}