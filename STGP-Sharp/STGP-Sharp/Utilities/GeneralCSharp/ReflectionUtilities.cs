using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace STGP_Sharp.Utilities.GeneralCSharp
{
    public static class ReflectionUtilities
    {
        // TODO should this be in GpReflectionCache?
        private static readonly ConcurrentDictionary<(Type type, Type attribute), bool> _dCacheHasAttribute =
            new ConcurrentDictionary<(Type type, Type attribute), bool>();

        public static Type GetUnderlyingType(this MemberInfo member)
        {
            return member.MemberType switch
            {
                MemberTypes.Event => ((EventInfo)member).EventHandlerType,
                MemberTypes.Field => ((FieldInfo)member).FieldType,
                MemberTypes.Method => ((MethodInfo)member).ReturnType,
                MemberTypes.Property => ((PropertyInfo)member).PropertyType,
                _ => throw new ArgumentException(
                    "Input MemberInfo must be of type EventInfo, FieldInfo, MethodInfo, or PropertyInfo")
            };
        }

        public static bool IsPrivateFieldOrProperty(this MemberInfo memberInfo)
        {
            return memberInfo.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)memberInfo).IsPrivate,
                MemberTypes.Property => ((PropertyInfo)memberInfo).GetAccessors()
                    .Any(methodInfo => methodInfo.IsPrivate),
                _ => false
            };
        }

        public static object GetValue(this MemberInfo memberInfo, object forObject)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).GetValue(forObject);
                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).GetValue(forObject);
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool HasAttribute(this Type t, Type requiredAttribute)
        {
            var key = (t, requiredAttribute);
            if (_dCacheHasAttribute.TryGetValue(key, out bool hasAttribute)) return hasAttribute;

            hasAttribute = null != t.GetCustomAttribute(requiredAttribute);
            _dCacheHasAttribute[key] = hasAttribute;

            return hasAttribute;
        }

        public static IEnumerable<Type> GetAllTypesFromAllAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
        }
    }
}