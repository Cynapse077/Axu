using UnityEngine;
using System.Collections.Generic;

namespace Augments
{
    public class Cybernetic
    {
        public string Name { get; protected set; }
        public string ID { get; protected set; }
        public string Desc;
        public BodyPart bodyPart;

        static Cybernetic[] cyberArray = new Cybernetic[]
        {
            new SyntheticMuscle(), new SubdermalScales(), new RadiationScrubber(), 
            new FoldingBlade(), new ArmCannon(), new ImpactSole(), new NanoRegen(),
            new NanoAdrenal(), new TargetSensor()
        };

        public static List<Cybernetic> GetCyberneticsForLimb(BodyPart bp)
        {
            List<Cybernetic> cs = new List<Cybernetic>();

            for (int i = 0; i < cyberArray.Length; i++)
            {
                if (cyberArray[i].CanAttach(bp))
                {
                    cs.Add(cyberArray[i].Clone());
                }
            }

            return cs;
        }

        public static Cybernetic GetCybernetic(string id)
        {
            for (int i = 0; i < cyberArray.Length; i++)
            {
                if (cyberArray[i].ID == id)
                {
                    return cyberArray[i].Clone();
                }
            }

            Debug.LogError("Could not find cybernetic with ID \"" + id + "\".");
            return null;
        }

        public virtual bool CanAttach(BodyPart bp)
        {
            return true;
        }

        public Cybernetic()
        {
            Name = "Undefined";
            ID = "N/A";
            Desc = "[N/A]";
        }

        public Cybernetic Clone()
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
            bodyPart.cybernetic = null;
            bodyPart = null;
        }
    }

    public class SyntheticMuscle : Cybernetic
    {
        const int bonus = 2;
        const string attribute = "Strength";

        public SyntheticMuscle()
        {
            Name = "Synthetic Muscle";
            ID = "SyntheticMuscle";
            Desc = "The muscles in your arms are interwoven with synthetic materials, increasing their strength.";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            bodyPart.AddAttribute(attribute, bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute(attribute, bonus);
        }

        public override void Remove()
        {
            bodyPart.AddAttribute(attribute, -bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute(attribute, -bonus);
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Arm;
        }
    }

    public class SubdermalScales : Cybernetic
    {
        const int bonus = 2;

        public SubdermalScales()
        {
            Name = "Subdermal Scales";
            ID = "SubdermalScales";
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
                bodyPart.myBody.entity.stats.RemoveRadiation(1);
            }
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Chest || bp.slot == ItemProperty.Slot_Back;
        }
    }

    public class FoldingBlade : Cybernetic
    {
        const string weaponID = "foldingblade";
        string baseWeaponID = "fists";

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
                baseWeaponID = bodyPart.hand.baseItem;
                bodyPart.hand.baseItem = weaponID;
                bodyPart.myBody.entity.inventory.PickupItem(bodyPart.hand.EquippedItem);
                bodyPart.hand.RevertToBase(bodyPart.myBody.entity);
            }
        }

        public override void Remove()
        {
            bodyPart.hand.baseItem = baseWeaponID;
            bodyPart.hand.RevertToBase(bodyPart.myBody.entity);
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Arm && bp.hand != null;
        }
    }

    public class ArmCannon : Cybernetic
    {
        const string firearmID = "armcannon";

        public ArmCannon()
        {
            Name = "Arm Cannon";
            ID = "ArmCannon";
            Desc = "Your arm has a small firearm set above the wrist.";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            Item firearm = bodyPart.myBody.entity.inventory.firearm;

            if (firearm != null)
            {
                bodyPart.myBody.entity.inventory.PickupItem(firearm);
            }

            bodyPart.myBody.entity.inventory.firearm = ItemList.GetItemByID(firearmID);
        }

        public override void Remove()
        {
            bodyPart.myBody.entity.inventory.firearm = ItemList.GetNone();
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Arm;
        }
    }

    public class ImpactSole : Cybernetic
    {
        const int bonus = 3;
        const string attribute = "Stealth";

        public ImpactSole()
        {
            Name = "Impact Sole";
            ID = "ImpactSole";
            Desc = "Your sole has an impact dampener, reducing sounds made by this leg.";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            bodyPart.AddAttribute(attribute, bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute(attribute, bonus);
        }

        public override void Remove()
        {
            bodyPart.AddAttribute(attribute, -bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute(attribute, -bonus);
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
        const string attribute = "HP Regen";

        public NanoRegen()
        {
            Name = "Healing Nanomachines";
            ID = "NanoRegen";
            Desc = "Upon being injured, your body will release nanomachines to help close the wound.";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            bodyPart.AddAttribute(attribute, 3);
            bodyPart.myBody.entity.stats.ChangeAttribute(attribute, 3);
        }

        public override void Remove()
        {
            bodyPart.AddAttribute(attribute, -3);
            bodyPart.myBody.entity.stats.ChangeAttribute(attribute, -3);
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
        const string attribute = "ST Regen";

        public NanoAdrenal()
        {
            Name = "Adrenal Nanomachines";
            ID = "NanoAdrenal";
            Desc = "Your blood is filled with nanomachines that aid in removing fatigue from muscles.";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            bodyPart.AddAttribute(attribute, bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute(attribute, bonus);
        }

        public override void Remove()
        {
            bodyPart.AddAttribute(attribute, -bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute(attribute, -bonus);
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
        const string attribute = "Accuracy";

        public TargetSensor()
        {
            Name = "Target Sensor";
            ID = "TargetSensor";
            Desc = "Your eyes are able to accurately predict a target's movement.";
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            bodyPart.AddAttribute(attribute, bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute(attribute, bonus);
        }

        public override void Remove()
        {
            bodyPart.AddAttribute(attribute, -bonus);
            bodyPart.myBody.entity.stats.ChangeAttribute(attribute, -bonus);
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Head;
        }
    }
}
