#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using STGP_Sharp.Utilities.GeneralCSharp;
using Utilities.GeneralCSharp;


namespace GP
{
    public static class GpReflectionCache
    {
        private static readonly ConcurrentDictionary<(Type superType, Type subType), bool> DCacheIsSubclass =
            new ConcurrentDictionary<(Type superType, Type subType), bool>();


        private static readonly ConcurrentDictionary<Type, List<Type>> DCacheGetAllSubTypes =
            new ConcurrentDictionary<Type, List<Type>>();

        private static readonly ConcurrentDictionary<Type, Type> DCacheGetReturnTypeFromGpBuildingBlockSubClass =
            new ConcurrentDictionary<Type, Type>();

        public static bool IsSubclass(Type superType, Type subType)
        {
            var key = (superType, subType);
            if (DCacheIsSubclass.TryGetValue(key, out var isSubclass)) return isSubclass;

            if ((subType.IsGenericType && subType.GetGenericTypeDefinition() == superType) ||
                subType.BaseType == superType)
            {
                DCacheIsSubclass[key] = true;
                return true;
            }

            isSubclass =
                subType.BaseType != null &&
                IsSubclass(superType, subType.BaseType); //&& GpRunner.IsSubclassOfGpBuildingBlock(subType.BaseType);
            DCacheIsSubclass[key] = isSubclass;
            return isSubclass;
        }

        public static IEnumerable<Type> GetAllSubTypes(Type parentType)
        {
            if (DCacheGetAllSubTypes.TryGetValue(parentType, out var subTypes)) return subTypes;

            subTypes = ReflectionUtilities.GetAllTypesFromAllAssemblies()
                .Where(t =>
                    t.BaseType != null &&
                    t != parentType &&
                    IsSubclass(parentType, t))
                .ToList();

            DCacheGetAllSubTypes[parentType] = subTypes;
            return subTypes;
        }

        public static Type GetReturnTypeFromGpBuildingBlockSubClass(Type type)
        {
            if (DCacheGetReturnTypeFromGpBuildingBlockSubClass.TryGetValue(type, out var returnType)) return returnType;

            returnType = Internal_GetReturnTypeFromGpBuildingBlockSubClass(type);
            DCacheGetReturnTypeFromGpBuildingBlockSubClass[type] = returnType;
            return returnType;
        }

        private static Type Internal_GetReturnTypeFromGpBuildingBlockSubClass(Type type)
        {
            var parentType = type;

            while (null != parentType)
            {
                var templateParameters = parentType.GetGenericArguments();
                if (templateParameters.Length > 0) return templateParameters[0];

                parentType = parentType.BaseType;
            }

            throw new Exception($"Type {type.Name} does not descend from a generic type");
        }
    }
}