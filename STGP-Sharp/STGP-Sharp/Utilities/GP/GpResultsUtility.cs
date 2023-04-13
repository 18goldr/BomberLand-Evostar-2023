#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using STGP_Sharp.Fitness;
using STGP_Sharp.Fitness.Fitness_Stats;
using STGP_Sharp.Fitness_and_Gp_Results;
using STGP_Sharp.Utilities.GeneralCSharp;

namespace STGP_Sharp.Utilities.GP
{
    public static class GpResultsUtility
    {
        public static Type GetGpResultsStatsType(Type fitnessType)
        {
            Debug.Assert(typeof(FitnessBase).IsAssignableFrom(fitnessType));
            
            var gpResultsStatsType = fitnessType.GetProperty("GpResultsStatsType")?.GetValue(null) as Type
                                           ?? throw new Exception(
                                               $"The property GpResultsStatsType is undefined for the type {fitnessType}");
            Debug.Assert(typeof(GpResultsStatsBase).IsAssignableFrom(gpResultsStatsType));

            return gpResultsStatsType;
        }
        
        private static void VerifyFitnessType(IFitnessFunction fitnessFunction)
        {
            Debug.Assert(typeof(FitnessBase).IsAssignableFrom(fitnessFunction.FitnessType));
        }

        public static GpResultsStatsBase.DetailedSummary GetDetailedSummary(Type gpResultsStatsType, IEnumerable<Individual> individuals)

        {
            Debug.Assert(typeof(GpResultsStatsBase).IsAssignableFrom(gpResultsStatsType));

            // Allow the user to not be required to define a constructor for individuals, and instead just fitness values.
            var hasConstructorWhichTakesIndividuals =
                gpResultsStatsType
                .GetConstructors()
                .Any(c =>
                     typeof(IEnumerable<Individual>) == c.GetParameters().FirstOrDefault()?.ParameterType);

            var gpResultsStatsObject = hasConstructorWhichTakesIndividuals ?
                                       Activator.CreateInstance(gpResultsStatsType, individuals) :
                                       Activator.CreateInstance(gpResultsStatsType, individuals.Select(i => i.fitness));
            var method = gpResultsStatsType.GetMethod("GetDetailedSummary");
            var detailedSumary = method?.Invoke(gpResultsStatsObject, Array.Empty<object>()) ??
                 throw new Exception($"Could not find the method GetDetailedSummary for type {gpResultsStatsType}");
            return (GpResultsStatsBase.DetailedSummary)detailedSumary;
        }

        private static void ValidateFitnessStatTypes()
        {
            var gpResultsStatsTypes = ReflectionUtilities.GetAllTypesFromAllAssemblies().Where(t => 
                typeof(GpResultsStatsBase).IsAssignableFrom(t) &&
                t != typeof(GpResultsStatsBase));

            var validTypes = 
                from type in gpResultsStatsTypes
                from c in type.GetConstructors()
                let paramType = c.GetParameters().First().ParameterType
                where paramType == typeof(IEnumerable<FitnessBase>) ||
                      paramType == typeof(IEnumerable<Individual>)
                select type;

            // TODO want to write to use an All linq call on the get constructors call in the previous statement
            var invalidTypes = gpResultsStatsTypes.Except(validTypes); 
               
            if (invalidTypes.Any())
            {
                throw new Exception($"The following GpResultsStats types are invalid: {string.Join(", ", invalidTypes)}");
            }
        }

        private static void ValidateFitnessTypes()
        {
            var fitnessTypes = ReflectionUtilities.GetAllTypesFromAllAssemblies().Where(t => 
                typeof(FitnessBase).IsAssignableFrom(t) &&
                t != typeof(FitnessBase));
            
            
            var invalidTypes = new List<Type>();

            foreach (var type in fitnessTypes)
            {
                try
                {
                    var gpResultsStatsType = (Type?)type.GetProperty("GpResultsStatsType")?.GetValue(null);
                    if (null == gpResultsStatsType || !typeof(GpResultsStatsBase).IsAssignableFrom(gpResultsStatsType))
                    {
                        invalidTypes.Add(type);
                    }
                }
                catch (Exception e)
                {
                    CustomPrinter.PrintLine(e);
                    invalidTypes.Add(type);
                }
            }
            
            if (invalidTypes.Any())
            {
                throw new Exception($"The following Fitness types are invalid: {string.Join(", ", invalidTypes)}");
            }
        }

        public static void ValidateFitnessFunctionAndRelatedClasses(IFitnessFunction fitnessFunction)
        {
            VerifyFitnessType(fitnessFunction);
            ValidateFitnessTypes();
            ValidateFitnessStatTypes();
        }
    }
}