using System;
using STGP_Sharp.Utilities.GeneralCSharp;
using Utilities.GeneralCSharp;

namespace STGP_Sharp
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class RandomTreeConstructorAttribute : Attribute
    {
    }

    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Parameter,
        Inherited = false)]
    public class FilterAttribute : Attribute
    {
        public bool IsSatisfiedBy(Type candidate)
        {
            return candidate.HasAttribute(GetType());
        }
    }

}