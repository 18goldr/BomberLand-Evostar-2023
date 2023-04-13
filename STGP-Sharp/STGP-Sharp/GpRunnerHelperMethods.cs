#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using GP;
using STGP_Sharp.GpBuildingBlockTypes;
using STGP_Sharp.Utilities.GeneralCSharp;
using STGP_Sharp.Utilities.GP;
using Newtonsoft.Json;
using STGP_Sharp.Fitness_and_Gp_Results;
using static STGP_Sharp.Utilities.GeneralCSharp.GeneralCSharpUtilities;

namespace STGP_Sharp
{
    public partial class GpRunner
    {
        // TODO move this to a new class and rename file to GpRunnerTreeGenerationHelperMethods
        private void MaybeSaveProgressToCheckpointJson(List<Individual> population)
        {
            if (null == this.checkPointSaveFile) return;

            var directoryContainingCheckPointSaveFile = CreateAndGetDirectoryForFileIfNotExist(this.checkPointSaveFile);

            var uniqueCheckPointSaveFileName =
                Path.GetFileNameWithoutExtension(this.checkPointSaveFile) +
                $@"_{DateTime.Now.ToString("s", 
                    CultureInfo.GetCultureInfo("en-US"))
                                       .Replace(":", "-")}" +
                Path.GetExtension(this.checkPointSaveFile);

            var checkPointSaveFileNameFullPath =
                Path.Join(directoryContainingCheckPointSaveFile, uniqueCheckPointSaveFileName);

            CustomPrinter.PrintLine($"Saving progress to {checkPointSaveFileNameFullPath}\n");
            
            var generatedPopulations = new GeneratedPopulations(
                new[] { population }.ToNestedList(),
                GpResultsUtility.GetDetailedSummary(
                    this._gpResultsStatsType,
                    population),
                timeoutInfo.runStartTime,
                DateTime.Now,
                population.SortedByFitness().FirstOrDefault(),
                verbose);
            
            var json = JsonConvert.SerializeObject(generatedPopulations, Formatting.Indented);
            
            File.WriteAllTextAsync(checkPointSaveFileNameFullPath, json);
            
        }

        public Node GenerateRandomTreeFromTypeOrReturnType<T>(int maxDepth, bool fullyGrow)
        {
            var t = typeof(T);
            var isSubclassOfGpBuildingBlock = IsSubclassOfGpBuildingBlock(t);
            var randomTree = isSubclassOfGpBuildingBlock
                ? GenerateRandomTreeOfType(t, maxDepth, fullyGrow) // TODO GenerateRootNodeOfType
                : GenerateRootNodeOfReturnType<T>(maxDepth);
            return randomTree;
        }

        public TypedRootNode<T> GenerateRootNodeOfReturnType<T>(int maxDepth)
        {
            var mustFullyGrow = rand.NextBool();
            var filters = GetFilterAttributes(typeof(T));
            var returnTypeSpecification = new ReturnTypeSpecification(typeof(T), filters);
            var child = (GpBuildingBlock<T>)GenerateRandomTreeOfReturnType(returnTypeSpecification, maxDepth - 1,
                mustFullyGrow);

            if (child.GetHeight() + 1 > maxDepth)
                throw new Exception("Somehow the max depth has been violated");

            return new TypedRootNode<T>(child);
        }

        public static bool IsListOfSubTypeOfExecutableNode(Type t)
        {
            return typeof(List<>).IsAssignableFrom(t) &&
                   IsSubclassOfGpBuildingBlock(GetReturnTypeSpecification(t).returnType);
        }

        private Node GetChildFromParam(ParameterInfo param, int maxDepth, bool fullyGrow)
        {
            var returnType = GpReflectionCache.GetReturnTypeFromGpBuildingBlockSubClass(param.ParameterType);
            var filters = GetFilterAttributes(param);
            var returnTypeSpecification = new ReturnTypeSpecification(returnType, filters);
            var child = GenerateRandomTreeOfReturnType(returnTypeSpecification, maxDepth - 1, fullyGrow);

            if (child.GetHeight() > maxDepth - 1) throw new Exception("Somehow the max depth has been violated");

            return child;
        }

        private static bool IsTypedRootNodeLegal(Node typedRootNode, ProbabilityDistribution probabilityDistribution)
        {
            var maybeTypedRootNodeType = typedRootNode.GetType();
            if (maybeTypedRootNodeType.GetGenericTypeDefinition() != typeof(TypedRootNode<>))
                throw new Exception("Root node must be a TypedRootNode");

            var returnTypeSpecification = GetReturnTypeSpecification(maybeTypedRootNodeType);
            var allTypes = GetTerminalsOfReturnType(returnTypeSpecification).ToList();
            allTypes.AddRange(GetNonTerminalsOfReturnType(returnTypeSpecification));
            return allTypes.Any(t => probabilityDistribution.GetProbabilityOfType(t) > 0);
        }

        public static bool IsValidTree(Node root, ProbabilityDistribution probabilityDistribution, int maxDepth)
        {
            var satisfiesTypeConstraints = true;
            if (root.GetType().GetGenericTypeDefinition() == typeof(TypedRootNode<>))
                satisfiesTypeConstraints = IsTypedRootNodeLegal(root, probabilityDistribution);

            satisfiesTypeConstraints =
                satisfiesTypeConstraints && NodeSatisfiesTypeConstraints(root, probabilityDistribution);

            return root.GetHeight() <= maxDepth && satisfiesTypeConstraints;
        }

        private static bool HaveCompatibleReturnTypes(Type executableNodeType1, Type executableNodeType2)
        {
            Debug.Assert(IsSubclassOfGpBuildingBlock(executableNodeType1) &&
                         IsSubclassOfGpBuildingBlock(executableNodeType2));
            return executableNodeType1.IsAssignableFrom(executableNodeType2) ||
                   executableNodeType2.IsAssignableFrom(executableNodeType1);
        }

        private static bool NodeSatisfiesTypeConstraints(Node node, ProbabilityDistribution probabilityDistribution)
        {
            var nodeType = node.GetType();

            if (IsTerminal(nodeType) && probabilityDistribution.GetProbabilityOfType(nodeType) > 0) return true;

            if (probabilityDistribution.GetProbabilityOfType(nodeType) <= 0) return false;

            var constructors = nodeType.GetConstructors();
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                if (node.children.Count != parameters.Length) continue;

                var parameterTypes = parameters.Select(pInfo => pInfo.ParameterType).ToList();
                var childrenTypes = node.children.Select(child => child.GetType()).ToList();
                // This is assuming constructor arguments and the children in a Node are in the same order respectively 
                // which is currently the case because we do so elsewhere in the code (ie. GenerateRandomTreeOfType)
                var zippedTypes = parameterTypes.Zip(childrenTypes, (p, c) => (p, c));
                if (zippedTypes.All(t => HaveCompatibleReturnTypes(t.Item1, t.Item2)))
                    return node.children.All(child => NodeSatisfiesTypeConstraints(child, probabilityDistribution));
            }

            return false;
        }

        public static int GetNumberOfChildrenOfGpBuildingBlockType(Type t)
        {
            return GetParametersOfGpBuildingBlockType(t, out _).Length;
        }

        public static ParameterInfo[] GetParametersOfGpBuildingBlockType(Type t, out ConstructorInfo constructor)
        {
            Debug.Assert(IsSubclassOfGpBuildingBlock(t));

            constructor =
                GetRandomTreeConstructor(t) ??
                throw new Exception(
                    $"The type {t.Name} does not have a constructor with the RandomTreeConstructor attribute");

            var parameters = constructor.GetParameters();
            return parameters;
        }

        public Node GenerateRandomTreeOfType(Type t, int currentMaxDepth, bool fullyGrow)
        {
            var parameters = GetParametersOfGpBuildingBlockType(t, out var constructor);

            var constructorArguments = new List<object?>();

            // TODO either make constants for strings or do something smarter (attributes?)

            foreach (var param in parameters)
                if (param.ParameterType == typeof(GpFieldsWrapper))
                    constructorArguments.Add(new GpFieldsWrapper(this));
                else if (param.ParameterType == typeof(int) && param.Name == "maxDepth")
                    constructorArguments.Add(currentMaxDepth - 1);
                else if (param.ParameterType == typeof(bool) && param.Name == "fullyGrow")
                    constructorArguments.Add(fullyGrow);
                else
                    constructorArguments.Add(GetChildFromParam(param, currentMaxDepth, fullyGrow));

            var node = (Node)constructor.Invoke(constructorArguments.ToArray());

            if (node.GetHeight() > currentMaxDepth) throw new Exception("Somehow the max depth has been violated.");

            return node;
        }

        public Node GenerateRandomTreeOfReturnType(ReturnTypeSpecification returnTypeSpecification, int currentMaxDepth,
            bool fullyGrow) // TODO change currentMaxDepth to currentDepth and take MaxDepth - currentDepth
        {
            // TODO filters??
            Debug.Assert(GetAllReturnTypes().Contains(returnTypeSpecification.returnType));

            Type randomSubType;
            var filterAttributes = returnTypeSpecification.filters.ToList();

            var nonTerminals = GetNonTerminalsOfReturnType(returnTypeSpecification);
            var nonTerminalsProbabilitySum = nonTerminals.Sum(t =>
                populationParameters.probabilityDistribution.GetProbabilityOfType(t));
            var hasLegalNonTerminals = nonTerminals.Count > 0 && nonTerminalsProbabilitySum > 0;

            var terminals =
                GetTerminalsOfReturnType(new ReturnTypeSpecification(returnTypeSpecification.returnType,
                    filterAttributes));
            var terminalsList = terminals.ToList();
            var terminalsProbabilitySum = terminalsList.Sum(t =>
                populationParameters.probabilityDistribution.GetProbabilityOfType(t));
            var hasLegalTerminals = terminalsList.Count > 0 && terminalsProbabilitySum > 0;

            var randomChanceToStopGrowing = hasLegalTerminals && !fullyGrow && rand.NextBool();

            if (!nodeReturnTypeToMinTreeDictionary.TryGetValue(returnTypeSpecification, out var minTree))
                throw new GpRunner.MinTreeNotSatisfiable(returnTypeSpecification.returnType);

            if (minTree.heightOfMinTree > currentMaxDepth)
                throw new GpRunner.MinTreeNotSatisfiable(returnTypeSpecification.returnType);

            if (minTree.heightOfMinTree == currentMaxDepth)
                // Generate Random Tree will generate the min tree if given the height of the min
                // tree as the current max depth which right now is equal to the current max depth anyways.
                randomSubType = minTree.permissibleNodeTypes.GetRandomEntry(rand);
            else if (currentMaxDepth < 1 || !hasLegalNonTerminals || randomChanceToStopGrowing)
                randomSubType = GetRandomTerminalOfReturnType(returnTypeSpecification);
            else
                randomSubType = GetRandomNonTerminalOfReturnType(returnTypeSpecification, currentMaxDepth);

            var tree = GenerateRandomTreeOfType(randomSubType, currentMaxDepth, fullyGrow);
            if (tree.GetHeight() > currentMaxDepth) throw new Exception("Somehow the max depth has been violated.");

            return tree;
        }

        public static IEnumerable<FilterAttribute> GetFilterAttributes(Type t)
        {
            return t.GetCustomAttributes<FilterAttribute>();
        }

        private static IEnumerable<FilterAttribute> GetFilterAttributes(ParameterInfo param)
        {
            return param.GetCustomAttributes<FilterAttribute>();
        }

        private static bool SatisfiesAllFilterAttributes(Type t, IEnumerable<FilterAttribute> filters)
        {
            return filters.All(f => f.IsSatisfiedBy(t));
        }

        private static IEnumerable<Type> GetAllSubTypesWithReturnType(Type openGenericType,
            ReturnTypeSpecification returnTypeSpecification)
        {
            var closedGenericType = openGenericType.MakeGenericType(returnTypeSpecification.returnType);
            return ReflectionUtilities.GetAllTypesFromAllAssemblies()
                .Where(t =>
                    closedGenericType.IsAssignableFrom(t) &&
                    t != closedGenericType &&
                    SatisfiesAllFilterAttributes(t, returnTypeSpecification.filters)
                );
        }


        public static bool IsSubclassOfGpBuildingBlock(Type type)
        {
            return GpReflectionCache.IsSubclass(typeof(GpBuildingBlock<>), type);
        }

        public static bool IsTerminal(Type t)
        {
            var constructor = GetRandomTreeConstructor(t);
            if (constructor == null) return false;

            var parameters = constructor.GetParameters();
            var zeroParams = parameters.Length == 0;
            var gpRunnerParam =
                parameters.Length == 1 &&
                parameters[0].ParameterType == typeof(GpFieldsWrapper);

            return zeroParams || gpRunnerParam;
        }

        private static IEnumerable<Type> GetTerminalsOfReturnType(ReturnTypeSpecification returnTypeSpecification)
        {
            var allTypes = GetAllSubTypesWithReturnType(typeof(GpBuildingBlock<>), returnTypeSpecification);
            return allTypes.Where(IsTerminal);
        }

        private Type GetRandomTerminalOfReturnType(ReturnTypeSpecification returnTypeSpecification)
        {
            var terminals = GetTerminalsOfReturnType(returnTypeSpecification).ToList();

            return GetRandomTypeFromDistribution(terminals); // TODO need to check for 0 probability
        }

        private static IEnumerable<Type> GetTerminals()
        {
            var allTypes = GetSubclassesOfGpBuildingBlock();
            return allTypes.Where(IsTerminal);
        }

        private static List<Type> GetNonTerminalsOfReturnType(ReturnTypeSpecification returnTypeSpecification)
        {
            var allTypes = GetAllSubTypesWithReturnType(typeof(GpBuildingBlock<>), returnTypeSpecification);
            return allTypes.Except(GetTerminalsOfReturnType(returnTypeSpecification)).ToList();
        }

        private Type GetRandomNonTerminalOfReturnType(ReturnTypeSpecification returnTypeSpecification,
            int currentMaxDepth)
        {
            var nonTerminals = GetNonTerminalsOfReturnType(returnTypeSpecification)
                .Where(t =>
                    nodeTypeToMinTreeDictionary.TryGetValue(t, out var minTree) &&
                    minTree.heightOfMinTree <= currentMaxDepth);
            var allowedTypes = nonTerminals.ToList();
            if (!allowedTypes.Any()) throw new GpRunner.MinTreeNotSatisfiable(returnTypeSpecification.returnType);
            return GetRandomTypeFromDistribution(allowedTypes);
        }

        private static ConstructorInfo? GetRandomTreeConstructor(Type t)
        {
            Debug.Assert(IsSubclassOfGpBuildingBlock(t));

            var constructors = t.GetConstructors();
            if (constructors.Length == 1) return constructors[0];

            var constructorsWithParameters = 0;
            ConstructorInfo? constructorToReturn = null;
            foreach (var candidateConstructor in constructors)
            {
                if (null != candidateConstructor.GetCustomAttribute<RandomTreeConstructorAttribute>())
                {
                    Debug.Assert(candidateConstructor != null);
                    return candidateConstructor;
                }

                if (candidateConstructor.GetParameters().Length > 0)
                {
                    constructorToReturn = candidateConstructor;
                    constructorsWithParameters++;
                }
            }

            if (constructorsWithParameters > 1)
                throw new Exception(
                    "There is no constructor decorated with the attribute [RandomTreeConstructor] " +
                    $"and there are multiple constructors with more than 0 parameters defined for the type {t.Name}. " +
                    "This is a limitation of the code we have written. " +
                    $"You must either change the code or change how the type {t.Name} is defined in order to continue");

            return constructorToReturn;
        }

        private Type GetRandomTypeFromDistribution(ProbabilityDistribution typeProbabilities)
        {
            return GetRandomElementFromDistribution(typeProbabilities.ToDictionary(), rand);
        }

        public Type GetRandomTypeFromDistribution()
        {
            return GetRandomTypeFromDistribution(populationParameters.probabilityDistribution);
        }

        private Type GetRandomTypeFromDistribution(IEnumerable<Type> allowedTypes)
        {
            var filteredTypes =
                populationParameters.probabilityDistribution.distribution.Where(tp =>
                    tp != null && allowedTypes.Contains(tp.type));
            return GetRandomTypeFromDistribution(new ProbabilityDistribution(filteredTypes.ToList()));
        }

        public static IEnumerable<Type> GetSubclassesOfGpBuildingBlock(bool sortAlphabetically = false)
        {
            var types = GpReflectionCache.GetAllSubTypes(typeof(GpBuildingBlock<>))
                .Except(new List<Type>
                {
                    typeof(TypedRootNode<>)
                })
                .Where(t => !t.IsAbstract);

            return sortAlphabetically
                ? types.OrderBy(GpUtility.GetBetterClassName)
                : types; // TODO always get better class name
        }

        public static Node LoadTreeFromFile(string file)
        {
            file = GetRelativePath(file);
            var tree = JsonConvert.DeserializeObject<Node>(File.ReadAllText(file)) ??
                       throw new Exception($"File {file} does not contain a GP Tree.");

            return tree;
        }

        public static IEnumerable<Type> GetFitnessFunctionTypes()
        {
            return ReflectionUtilities.GetAllTypesFromAllAssemblies().Where(t =>
                typeof(IFitnessFunction).IsAssignableFrom(t) &&
                !t.IsAbstract &&
                null == Attribute.GetCustomAttribute(t,
                    typeof(CompilerGeneratedAttribute))); // Exclude compiler generated classes
        }

        public static IEnumerable<Type> GetAllReturnTypes()
        {
            var allTypes = GetSubclassesOfGpBuildingBlock();
            var allTypesArray = allTypes as Type[] ?? allTypes.ToArray();
            var returnTypes = allTypesArray
                .Where(t =>
                    t.BaseType is { IsGenericType: true } &&
                    t.BaseType.GetGenericTypeDefinition().IsAssignableFrom(typeof(GpBuildingBlock<>)))
                // ReSharper disable once NullableWarningSuppressionIsUsed
                // The previous line checks that it's a subclass of ExecutableNode, so it obviously has a non-null base type.
                .Select(t => GpReflectionCache.GetReturnTypeFromGpBuildingBlockSubClass(t.BaseType!)).ToList();
            returnTypes.AddRange(allTypesArray);
            return returnTypes;
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private static PropertyInfo[] GetAllGpBuildingBlockProperties(Type t)
        {
            if (!IsSubclassOfGpBuildingBlock(t))
                throw new Exception($"Type {t.Name} is not a subclass of executable tree");

            var props = new List<PropertyInfo>();

            // TODO currentType will never be null because it will reach Node first.
            // This is the case because t is always a subclass of GpBuildingBlock which
            // always extends from Node.
            var currentType = t;
            do
            {
                var newProps = currentType?
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .ToList();
                if (newProps != null) props.AddRange(newProps);
            } while ((currentType = currentType!.BaseType) != typeof(Node));

            return props.ToArray();
        }

        public static string? GetChildPropertyNameAtChildrenIndex(int i, Node? child)
        {
            if (child == null) throw new Exception("Child cannot be null");

            var childProperties = GetAllGpBuildingBlockProperties(child.GetType());

            return childProperties
                .Where(prop =>
                    ReferenceEquals(prop.GetValue(child, null), child.children[i]))
                .Select(prop => prop.Name)
                .FirstOrDefault();
        }

        private static ReturnTypeSpecification GetReturnTypeSpecification(Type t)
        {
            var returnType = GpReflectionCache.GetReturnTypeFromGpBuildingBlockSubClass(t);
            var filters = GetFilterAttributes(t);
            return new ReturnTypeSpecification(returnType, filters);
        }

        public static Dictionary<int, List<int>> GetLegalCrossoverPointsInChildren(Node a, Node b)
        {
            var xPoints = new Dictionary<int, List<int>>();
            var typesFound = new Dictionary<ReturnTypeSpecification, List<int>>();
            var i = 1;
            foreach (var nodeWrapper in a.IterateNodeWrappers().Skip(1))
            {
                var node = nodeWrapper.child;
                // Hack so we don't crossover frequency for canMove nodes in bomberland gp
                if (nodeWrapper.parent != null && 
                    node.returnType == typeof(float) && 
                    nodeWrapper.parent.symbol.Contains("canMove"))
                {
                    continue;
                }
                var filters = GetFilterAttributes(node.GetType()).ToList();
                var nodeSpec = new ReturnTypeSpecification(node.returnType, filters);
                var locations = typesFound.ContainsKey(nodeSpec)
                    ? typesFound[nodeSpec]
                    : b.GetSymTypeAndFilterLocationsInDescendants(node.returnType, filters).ToList();

                if (locations.Count > 0)
                {
                    typesFound[nodeSpec] = locations;
                    xPoints[i] = typesFound[nodeSpec];
                }

                i++;
            }

            return xPoints;
        }
    }
}