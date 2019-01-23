using System.Collections.Generic;

namespace Augments
{
    public class Cybernetic
    {
        public string Name { get; protected set; }
        public string ID { get; protected set; }
        protected BodyPart bodyPart;

        public Cybernetic()
        {
            Name = "Undefined";
            ID = "N/A";
        }

        public Cybernetic Clone(Cybernetic other)
        {
            return (Cybernetic)MemberwiseClone();
        }

        public virtual void Attach(BodyPart bp)
        {
            bodyPart = bp;
            bodyPart.cybernetic = this;
        }

        public virtual void Remove()
        {
            bodyPart = null;
            bodyPart.cybernetic = null;
        }

        public virtual bool CanAttach(BodyPart bp)
        {
            return true;
        }
    }

    public class RadiationScrubber : Cybernetic
    {
        public RadiationScrubber()
        {
            Name = "Radiation Scrubber";
            ID = "RadScrubber";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            World.turnManager.incrementTurnCounter += OnTurn;
        }

        public override void Remove()
        {
            base.Remove();
            World.turnManager.incrementTurnCounter -= OnTurn;
        }

        void OnTurn()
        {
            if (bodyPart != null && World.turnManager.turn % 100 == 0)
            {
                bodyPart.myBody.entity.stats.radiation--;
            }
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Chest || bp.slot == ItemProperty.Slot_Back;
        }
    }

    public class FoldingBlade : Cybernetic
    {
        public FoldingBlade()
        {
            Name = "Folding Blade";
            ID = "FoldingBlade";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);

            if (bp.hand != null)
            {
                bp.hand.baseItem = "foldingblade";
            }
        }

        public override void Remove()
        {
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Arm;
        }
    }

    public class ImpactSoles : Cybernetic
    {
        public ImpactSoles()
        {
            Name = "Impact Soles";
            ID = "ImpactSoles";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            bodyPart.AddAttribute("Stealth", 3);
            bodyPart.myBody.entity.stats.ChangeAttribute("Stealth", 3);
        }

        public override void Remove()
        {
            bodyPart.AddAttribute("Stealth", -3);
            bodyPart.myBody.entity.stats.ChangeAttribute("Stealth", -3);
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Leg;
        }
    }
}
