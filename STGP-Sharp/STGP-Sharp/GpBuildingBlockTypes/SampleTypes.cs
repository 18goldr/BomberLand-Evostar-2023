#nullable enable
using System;
using System.Diagnostics;
using STGP_Sharp.Utilities.GeneralCSharp;

namespace STGP_Sharp.GpBuildingBlockTypes
{

    public abstract class BinaryOperator<TReturnType, TOperandType> : GpBuildingBlock<TReturnType>
    {
        protected BinaryOperator(GpBuildingBlock<TOperandType> left, GpBuildingBlock<TOperandType> right) :
            base(left, right)
        {
        }

        public GpBuildingBlock<TOperandType> Left => (GpBuildingBlock<TOperandType>)children[0];
        public GpBuildingBlock<TOperandType> Right => (GpBuildingBlock<TOperandType>)children[1];
    }

    public abstract class BooleanOperator : BinaryOperator<bool, bool>
    {
        protected BooleanOperator(GpBuildingBlock<bool> left, GpBuildingBlock<bool> right) :
            base(left, right)
        {
        }

        protected BooleanOperator() : base(new BooleanConstant(true), new BooleanConstant(true))
        {
        }
    }

    public class And : BooleanOperator
    {
        public And(GpBuildingBlock<bool> left, GpBuildingBlock<bool> right) :
            base(left, right)
        {
        }

        public And()
        {
        }
        

        public override bool Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return Left.Evaluate(gpFieldsWrapper) && Right.Evaluate(gpFieldsWrapper);
        }
    }

    public class Not : GpBuildingBlock<bool>
    {
        public Not(GpBuildingBlock<bool> operand) : base(operand)
        {
        }

        public Not()
        {
        }

        public GpBuildingBlock<bool> Operand => (GpBuildingBlock<bool>)children[0];

        public override bool Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return !Operand.Evaluate(gpFieldsWrapper);
        }
    }

    public class BooleanConstant : GpBuildingBlock<bool>
    {
        private readonly bool _value;

        public BooleanConstant(bool v)
        {
            _value = v;
            symbol = v.ToString();
        }

        [RandomTreeConstructor]
        public BooleanConstant(GpFieldsWrapper gpFieldsWrapper) : base(gpFieldsWrapper)
        {
            _value = gpFieldsWrapper.rand.NextBool();
            symbol = _value.ToString();
        }

        public override bool Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return _value;
        }
    }

    public class PositionalArgument<TArgumentType> : GpBuildingBlock<TArgumentType>
    {
        public int argIndex;

        [RandomTreeConstructor]
        public PositionalArgument(GpFieldsWrapper gpFieldsWrapper) : base(gpFieldsWrapper)
        {
            if (null == gpFieldsWrapper.positionalArguments) throw new Exception("Positional arguments is null");
            argIndex = gpFieldsWrapper.positionalArguments.PopNextIndex();
        }

        public PositionalArgument(int argIndex)
        {
            this.argIndex = argIndex;
        }

        public override TArgumentType Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            if (null == gpFieldsWrapper.positionalArguments) throw new Exception("Positional arguments is null");

            if (gpFieldsWrapper.positionalArguments.MapToTypedArgument(argIndex, out TArgumentType? arg))
            {
                Debug.Assert(null != arg);

                return arg;
            }
            
            throw new Exception($"Positional argument at index {argIndex} is not of type {typeof(TArgumentType)}");
        }
    }

    // TODO possible to have symbol include typed index?
    public class BooleanPositionalArgument : PositionalArgument<bool>
    {
        [RandomTreeConstructor]
        public BooleanPositionalArgument(GpFieldsWrapper gpFieldsWrapper) : base(gpFieldsWrapper)
        {
            this.symbol = $"BoolArg{this.argIndex}";
        }

        public BooleanPositionalArgument(int argIndex) : base(argIndex)
        {
            this.symbol = $"BoolArg{this.argIndex}";
        }
    }
}