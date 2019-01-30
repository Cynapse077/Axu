using System;

namespace Augments
{
    public abstract class Cybernetic
    {
        public string Name { get; protected set; }
        public string ID { get; protected set; }
        public string Desc;
        protected BodyPart bodyPart;

        public abstract bool CanAttach(BodyPart bp);

        public Cybernetic()
        {
            Name = "Undefined";
            ID = "N/A";
            Desc = "[N/A]";
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

        public static Type GetCyberneticType(Cybernetic c)
        {
            switch (c.ID)
            {
                case "RadScrubber":
                    return typeof(RadiationScrubber);
                case "FoldingBlade":
                    return typeof(FoldingBlade);
                case "ImpactSole":
                    return typeof(ImpactSole);
                case "NanoRegen":
                    return typeof(NanoRegen);
                case "NanoAdrenal":
                    return typeof(NanoAdrenal);
                case "TargetSensor":
                    return typeof(TargetSensor);
                case "DermalPlating":
                    return typeof(DermalPlating);

                default: return null;
            }
        }
    }

    public class SyntheticMuscle : Cybernetic
    {
        const int bonus = 2;

        public SyntheticMuscle()
        {
            Name = "Synthetic Muscle";
            ID = "SyntheticMuscle";
            Desc = "The muscles in your arms are interwoven with synthetic materials, increasing their strength.";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            bodyPart.AddAttribute("Strength", bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute("Strength", bonus);
        }

        public override void Remove()
        {
            bodyPart.AddAttribute("Strength", -bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute("Strength", -bonus);
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Arm;
        }
    }

    public class DermalPlating : Cybernetic
    {
        const int bonus = 2;

        public DermalPlating()
        {
            Name = "Dermal Plating";
            ID = "DermalPlating";
            Desc = "You have small plates under the skin of this body part. They offer increased protection.";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            bodyPart.armor += bonus;
        }

        public override void Remove()
        {
            bodyPart.armor -= bonus;
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return true;
        }
    }

    public class RadiationScrubber : Cybernetic
    {
        const int turnsToActivate = 100;

        public RadiationScrubber()
        {
            Name = "Radiation Scrubber";
            ID = "RadScrubber";
            Desc = "Your body scrubs away radiation as your heart beats.";
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
            if (bodyPart != null && World.turnManager.turn % turnsToActivate == 0)
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
            Desc = "Your arm sports a curved blade that can be retracted into your flesh.";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);

            if (bodyPart.hand != null)
            {
                bodyPart.hand.baseItem = "foldingblade";
                bodyPart.myBody.entity.inventory.PickupItem(bodyPart.hand.EquippedItem);
                bodyPart.hand.RevertToBase(bodyPart.myBody.entity);
            }
        }

        public override void Remove()
        {
            bodyPart.hand.baseItem = "fists";
            bodyPart.hand.RevertToBase(bodyPart.myBody.entity);
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Arm && bp.hand != null;
        }
    }

    public class ImpactSole : Cybernetic
    {
        const int bonus = 3;

        public ImpactSole()
        {
            Name = "Impact Sole";
            ID = "ImpactSole";
            Desc = "Your sole has an impact dampener, reducing sounds made by this leg.";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            bodyPart.AddAttribute("Stealth", bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute("Stealth", bonus);
        }

        public override void Remove()
        {
            bodyPart.AddAttribute("Stealth", -bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute("Stealth", -bonus);
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Leg;
        }
    }

    public class NanoRegen : Cybernetic
    {
        const int bonus = 3;

        public NanoRegen()
        {
            Name = "Healing Nanomachines";
            ID = "NanoRegen";
            Desc = "Upon being injured, your body will release nanomachines to help close the wound.";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            bodyPart.AddAttribute("HP Regen", 3);
            bodyPart.myBody.entity.stats.ChangeAttribute("HP Regen", 3);
        }

        public override void Remove()
        {
            bodyPart.AddAttribute("HP Regen", -3);
            bodyPart.myBody.entity.stats.ChangeAttribute("HP Regen", -3);
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Chest || bp.slot == ItemProperty.Slot_Back;
        }
    }

    public class NanoAdrenal : Cybernetic
    {
        const int bonus = 2;

        public NanoAdrenal()
        {
            Name = "Adrenal Nanomachines";
            ID = "NanoAdrenal";
            Desc = "Your blood is filled with nanomachines that aid in removing fatigue from muscles.";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            bodyPart.AddAttribute("ST Regen", bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute("ST Regen", bonus);
        }

        public override void Remove()
        {
            bodyPart.AddAttribute("ST Regen", -bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute("ST Regen", -bonus);
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Chest || bp.slot == ItemProperty.Slot_Back;
        }
    }

    public class TargetSensor : Cybernetic
    {
        const int bonus = 2;

        public TargetSensor()
        {
            Name = "Target Sensor";
            ID = "TargetSensor";
            Desc = "Your eyes are able to accurately predict a target's movement.";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            bodyPart.AddAttribute("Accuracy", bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute("Accuracy", bonus);
        }

        public override void Remove()
        {
            bodyPart.AddAttribute("Accuracy", -bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute("Accuracy", -bonus);
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Head;
        }
    }
}
