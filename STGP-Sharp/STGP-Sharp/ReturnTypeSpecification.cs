#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using STGP_Sharp;
using STGP_Sharp.Utilities.GeneralCSharp;
using Utilities.GeneralCSharp;

namespace GP
{
    public class ReturnTypeSpecification : IEquatable<ReturnTypeSpecification>
    {
        public readonly IEnumerable<FilterAttribute> filters;
        public readonly Type returnType;

        public ReturnTypeSpecification(Type returnType, IEnumerable<FilterAttribute>? filters)
        {
            this.returnType = returnType;
            this.filters = filters ?? new FilterAttribute[] { };
        }

        public bool Equals(ReturnTypeSpecification? spec)
        {
            if (null == spec) return false;
            var sameReturnType = returnType == spec.returnType;
            var sameFilters = filters.SequenceEqual(spec.filters);
            return sameReturnType && sameFilters;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ReturnTypeSpecification ??
                          throw new InvalidOperationException());
        }

        public override int GetHashCode()
        {
            var allHashes = new List<int> { returnType.GetHashCode() };
            allHashes.AddRange(filters.Select(f => f.GetHashCode()));
            return GeneralCSharpUtilities.CombineHashCodes(allHashes);
        }
    }
}