using System;
using System.Linq;
using STGP_Sharp;
using STGP_Sharp.GpBuildingBlockTypes;
using STGP_Sharp.Utilities.GeneralCSharp;

namespace BomberLandGp
{
    
    public class BomberLandAgentAction
    {
        public enum BomberLandAgentActionEnum
        {
            Up,
            Down,
            Left,
            Right,
            Bomb,
            Detonate,
            noop
        }

        public BomberLandAgentActionEnum bomberLandAgentActionEnum;

        public override string ToString()
        {
            return this.bomberLandAgentActionEnum.ToString();
        }
    }

    public class BomberLandAgentAttribute
    {
        public enum BomberLandAgentAttributeEnum
        {
            HP,
            bombsInInventory,
            isBombLeft,
            isBombRight,
            isBombUp,
            isBombDown,
            isAmmoLeft,
            isAmmoRight,
            isAmmoUp,
            isAmmoDown,
            isPowerUpLeft,
            isPowerUpRight,
            isPowerUpUp,
            isPowerUpDown,
            isEnemyInBlastRadius,
            enemiesAround,
            canMoveLeft,
            canMoveRight,
            canMoveUp,
            canMoveDown
        }

        public BomberLandAgentAttributeEnum bomberLandAgentAttributeEnum;
        
        
        public override string ToString()
        {
            return this.bomberLandAgentAttributeEnum.ToString();
        }
    }

    public class BomberLandAgentBehavior : GpBuildingBlock<BomberLandAgentAction>
    {
        public GpBuildingBlock<BomberLandAgentAttribute> AgentAttribute => (GpBuildingBlock<BomberLandAgentAttribute>)this.children[0];
        
        public GpBuildingBlock<float> Threshold => (GpBuildingBlock<float>)this.children[1];
        
        public GpBuildingBlock<BomberLandAgentAction> TrueBranch => (GpBuildingBlock<BomberLandAgentAction>)this.children[2];

        public GpBuildingBlock<BomberLandAgentAction> FalseBranch => (GpBuildingBlock<BomberLandAgentAction>)this.children[3];

        public BomberLandAgentBehavior(
            GpBuildingBlock<BomberLandAgentAttribute> agentAttribute,
            GpBuildingBlock<float> threshold,
            GpBuildingBlock<BomberLandAgentAction> trueBranch,
            GpBuildingBlock<BomberLandAgentAction> falseBranch) : base(agentAttribute, threshold, trueBranch, falseBranch)
        {
        }

        public override BomberLandAgentAction Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return this.TrueBranch.Evaluate(gpFieldsWrapper); // TODO bad hack!!

        }

        public override string ToString() // TODO bad hack because evaluate doesnt work like we want it!
        {
            return this.ToStringInListForm()
                .Replace("BomberLandAgentBehavior", "")
                .Replace(" ", "");
        }
    }

    // TODO add automatic class for enums
    public class BomberLandAgentAttributeConstant : GpBuildingBlock<BomberLandAgentAttribute>
    {
        protected BomberLandAgentAttribute value; // TODO would like to be readonly/private

        public BomberLandAgentAttributeConstant(BomberLandAgentAttribute v)
        {
            this.value = v;
            this.symbol = v.ToString();
        }

        [RandomTreeConstructor]
        public BomberLandAgentAttributeConstant(GpFieldsWrapper gpFieldsWrapper) : base(gpFieldsWrapper)
        {
            var attribute = Enum.GetValues(typeof(BomberLandAgentAttribute.BomberLandAgentAttributeEnum))
                .Cast<BomberLandAgentAttribute.BomberLandAgentAttributeEnum>()
                .GetRandomEntry(gpFieldsWrapper.rand);
            this.value = new BomberLandAgentAttribute { bomberLandAgentAttributeEnum = attribute };
            this.symbol = this.value.ToString();
        }


        public override BomberLandAgentAttribute Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return this.value;
        }
    }

    public class BomberLandActionConstant : GpBuildingBlock<BomberLandAgentAction>
    {
        protected BomberLandAgentAction value; // TODO would like to be readonly/private

        public BomberLandActionConstant(BomberLandAgentAction v)
        {
            this.value = v;
            this.symbol = v.ToString();
        }

        [RandomTreeConstructor]
        public BomberLandActionConstant(GpFieldsWrapper gpFieldsWrapper) : base(gpFieldsWrapper)
        {
            var action = Enum.GetValues(typeof(BomberLandAgentAction.BomberLandAgentActionEnum))
                .Cast<BomberLandAgentAction.BomberLandAgentActionEnum>()
                .GetRandomEntry(gpFieldsWrapper.rand);
            this.value = new BomberLandAgentAction { bomberLandAgentActionEnum = action };
           this.symbol = this.value.bomberLandAgentActionEnum.ToString();
        }


        public override BomberLandAgentAction Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return this.value;
        }
    }
    
    
    public class FloatConstant : GpBuildingBlock<float>
    {
        protected float value; // TODO would like to be readonly/private

        public FloatConstant(float v)
        {
            this.value = v;
            this.symbol = v.ToString();
        }

        [RandomTreeConstructor]
        public FloatConstant(GpFieldsWrapper gpFieldsWrapper) : base(gpFieldsWrapper)
        {
            this.value = Utilities.ShiftNumberIntoRange(
                gpFieldsWrapper.rand.NextFloat(),
                gpFieldsWrapper.populationParameters.floatMin,
                gpFieldsWrapper.populationParameters.floatMax);
            this.symbol = this.value.ToString();
        }

        public override float Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return this.value;
        }
    }
}