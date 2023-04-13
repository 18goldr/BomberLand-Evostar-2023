#nullable enable

using System.Collections.Generic;
using System.Linq;
using STGP_Sharp.Utilities.GeneralCSharp;
using Utilities.GeneralCSharp;

namespace GP
{
    public class PositionalArguments
    {
        private readonly MultiTypeCollection _positionalArguments;
        private int _positionalArgumentsUsed;

        public PositionalArguments(MultiTypeCollection args)
        {
            _positionalArguments = args;
        }

        public PositionalArguments()
        {
            _positionalArguments = new MultiTypeCollection();
        }

        public PositionalArguments(params object[] args)
        {
            this._positionalArguments = new MultiTypeCollection();
            foreach (var arg in args)
            {
                this._positionalArguments.Add(arg.GetType(), arg);
            }
        }

        public int PopNextIndex()
        {
            return _positionalArgumentsUsed++;
        }

        public bool MapToTypedArgument<T>(int untypedIndex, out T? arg)
        {
            var typedIndex = GetTypedIndex<T>(untypedIndex, out var numOfType);
            if (numOfType < 1)
            {
                arg = default;
                return false;
            }

            arg = _positionalArguments.Get<T>().Skip(typedIndex).FirstOrDefault();
            return true;
        }
        
        public static implicit operator PositionalArguments(object[] args)
        {
            return new PositionalArguments(args);
        }
        
        public static implicit operator PositionalArguments(List<object> args)
        {
            return new PositionalArguments(args.ToArray());
        }

        public int GetTypedIndex<T>(int untypedIndex, out int numOfType)
        {
            numOfType = _positionalArguments.Get<T>().Count();
            var typedIndex = untypedIndex % numOfType;
            return typedIndex;
        }
    }
}