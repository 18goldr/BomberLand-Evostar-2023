#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using STGP_Sharp.Utilities.GeneralCSharp;
using STGP_Sharp.Utilities.GP;

namespace STGP_Sharp.GpBuildingBlockTypes
{
    public class GpBuildingBlock<TReturnType> : Node 
    {
        protected GpBuildingBlock(params Node[] inputs) : base(typeof(TReturnType), inputs.ToList())
        {
        }

        protected GpBuildingBlock() : base(typeof(TReturnType))
        {
        }

        [RandomTreeConstructor]
        // ReSharper disable once UnusedParameter.Local
        protected GpBuildingBlock(GpFieldsWrapper gpFieldsWrapper) : base(typeof(TReturnType))
        {
        }

        public virtual TReturnType Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            throw new Exception();
        }
    }

    public class TypedRootNode<TReturnType> : GpBuildingBlock<TReturnType>
    {
        public TypedRootNode(GpBuildingBlock<TReturnType> child) : base(child)
        {
            symbol = "RootNode" + GpUtility.GetBetterClassName(typeof(TReturnType));
        }

        public GpBuildingBlock<TReturnType> Child =>
            (GpBuildingBlock<TReturnType>)this.children[0]; // There is only ever one child in this tree

        public override TReturnType Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return Child.Evaluate(gpFieldsWrapper);
        }
        
    }
    
    
    public class EnumConstant<TEnum> : GpBuildingBlock<TEnum> where TEnum : Enum
    {
        protected readonly TEnum value;
        public EnumConstant(TEnum v)
        {
            this.value = v;
            this.symbol = v.ToString();
        }
        
        
        [RandomTreeConstructor]
        public EnumConstant(
            GpFieldsWrapper gpFieldsWrapper, 
            Func<TEnum, bool>? filter = null) : base(gpFieldsWrapper)
        {
            IEnumerable<TEnum> enums = Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
            if (null != filter)
            {
                enums = enums.Where(filter);
            }
            
            TEnum e = enums.GetRandomEntry(gpFieldsWrapper.rand) ?? 
                      throw new Exception($"Cannot find sample value for the enum {typeof(TEnum)}");
            this.value = e;
            this.symbol = this.value.ToString();
        }


        public override TEnum Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return this.value;
        }
    }
}