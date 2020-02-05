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
            new NanoAdrenal(), new TargetSensor(), new Shielding(), new SpringTendon()
        };

        public static void AddCyberneticToData(Cybernetic c)
        {
            List<Cybernetic> cybs = new List<Cybernetic>();
            
            for (int i = 0; i < cyberArray.Length; i++)
            {
                //Avoid duplicates
                if (cyberArray[i].ID == c.ID)
                {
                    return;
                }

                cybs.Add(cyberArray[i]);
            }

            cybs.Add(c);
            cyberArray = cybs.ToArray();
        }

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
            Desc = LocalizationManager.GetContent("CybDesc_" + ID);
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
            Desc = LocalizationManager.GetContent("CybDesc_" + ID);
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
            Desc = LocalizationManager.GetContent("CybDesc_" + ID);
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

    public class Shielding : Cybernetic
    {
        const int amount = 10;

        public Shielding()
        {
            Name = "Shielding";
            ID = "Shielding";
            Desc = LocalizationManager.GetContent("CybDesc_" + ID);
        }

        public override void Attach(BodyPart bp)
        {
            base.Attach(bp);
            bodyPart.myBody.entity.stats.ChangeAttribute("Health", amount);
        }

        public override void Remove()
        {
            bodyPart.myBody.entity.stats.ChangeAttribute("Health", -amount);
            base.Remove();
        }

        public override bool CanAttach(BodyPart bp)
        {
            return bp.slot == ItemProperty.Slot_Chest;
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
            Desc = LocalizationManager.GetContent("CybDesc_" + ID);
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
            Desc = LocalizationManager.GetContent("CybDesc_" + ID);
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
            bodyPart.myBody.entity.inventory.firearm = ItemList.NoneItem;
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
            Desc = LocalizationManager.GetContent("CybDesc_" + ID);
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
            Desc = LocalizationManager.GetContent("CybDesc_" + ID);
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
            Desc = LocalizationManager.GetContent("CybDesc_" + ID);
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
            Desc = LocalizationManager.GetContent("CybDesc_" + ID);
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

    public class SpringTendon : Cybernetic
    {
        const int bonus = 3;
        const string attribute = "Accuracy";

        public SpringTendon()
        {
            Name = "Spring Tendon";
            ID = "SpringTendon";
            Desc = LocalizationManager.GetContent("CybDesc_" + ID);
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
    }
}
