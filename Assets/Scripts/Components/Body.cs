using System.Collections.Generic;
using UnityEngine;

[MoonSharp.Interpreter.MoonSharpUserData]
public class Body : MonoBehaviour
{
    public List<BodyPart> bodyParts;
    public Entity entity;
    public BodyPart.Hand defaultHand;

    Stats MyStats
    {
        get { return entity.stats; }
    }
    Inventory MyInventory
    {
        get { return entity.inventory; }
    }

    public BodyPart.Hand MainHand
    {
        get
        {
            if (bodyParts.Find(x => x.slot == ItemProperty.Slot_Arm && x.isAttached) == null)
            {
                return defaultHand;
            }

            return bodyParts.Find(x => x.slot == ItemProperty.Slot_Arm && x.isAttached).hand;
        }
    }

    public int MainHandIndex
    {
        get
        {
            BodyPart.Hand mh = MainHand;

            for (int i = 0; i < Hands.Count; i++)
            {
                if (Hands[i] == mh)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    void OnDisable()
    {
        if (bodyParts == null)
        {
            return;
        }
        
        for (int i = 0; i < bodyParts.Count; i++)
        {
            if (bodyParts[i].grip != null && bodyParts[i].grip.heldPart != null)
            {
                bodyParts[i].grip.Release();
            }
        }
    }

    public void InitializeBody()
    {
        bodyParts = new List<BodyPart>();

        for (int i = 0; i < entity.AI.npcBase.bodyParts.Count; i++)
        {
            bodyParts.Add(new BodyPart(entity.AI.npcBase.bodyParts[i]));
        }

        Item handItem = (entity.AI.npcBase.handItems.Count > 0 && entity.AI.npcBase.handItems[0] != null) ? entity.AI.npcBase.handItems[0] : ItemList.GetItemByName("fists");

        defaultHand = new BodyPart.Hand(GetBodyPartBySlot(ItemProperty.Slot_Head), handItem, handItem.ID);

        for (int i = 0; i < entity.AI.npcBase.handItems.Count; i++)
        {
            if (Hands.Count > i && Hands[i] != null && Hands[i].IsAttached)
            {
                Hands[i].SetEquippedItem(entity.AI.npcBase.handItems[i], entity);

                if (entity.AI.npcBase.handItems[i].HasProp(ItemProperty.Cannot_Remove))
                {
                    Hands[i].baseItem = entity.AI.npcBase.handItems[i].ID;
                }
            }
        }

        if (bodyParts == null || bodyParts.Count <= 0)
        {
            bodyParts = EntityList.DefaultBodyStructure();
        }

        for (int i = 0; i < bodyParts.Count; i++)
        {
            if (bodyParts[i].equippedItem == null || bodyParts[i].equippedItem.Name == "")
            {
                bodyParts[i].equippedItem = ItemList.GetNone();
            }

            bodyParts[i].myBody = this;
        }

        bodyParts = CharacterCreation.SortBodyParts(bodyParts);
    }

    //Called from Lua
    public void AddBodyPart(BodyPart bp)
    {
        bodyParts.Add(bp);
        bodyParts = CharacterCreation.SortBodyParts(bodyParts);
    }

    public List<int> SeverableBodyPartsIDs()
    {
        List<int> bp = new List<int>();

        for (int i = 0; i < bodyParts.Count; i++)
        {
            if (bodyParts[i].severable)
            {
                bp.Add(i);
            }
        }

        return bp;
    }

    public List<BodyPart> SeverableBodyParts()
    {
        return bodyParts.FindAll(x => x.isAttached && x.severable);
    }

    public List<BodyPart> TargetableBodyParts()
    {
        return bodyParts.FindAll(x => x.isAttached);
    }

    public int TotalBodyWeight()
    {
        int weight = 0;

        for (int i = 0; i < bodyParts.Count; i++)
        {
            weight += bodyParts[i].Weight;
        }

        return weight;
    }

    public void TrainLimb(BodyPart part)
    {
        part.AddXP(entity, SeedManager.combatRandom.NextDouble());
    }

    public void TrainLimbOfType(ItemProperty[] types)
    {
        if (!entity.isPlayer || types.Length == 0)
        {
            return;
        }

        List<BodyPart> parts = new List<BodyPart>();

        for (int i = 0; i < types.Length; i++)
        {
            parts.AddRange(GetBodyPartsBySlot(types[i]));
        }

        if (parts.Count > 0)
        {
            parts.GetRandom().AddXP(entity, SeedManager.combatRandom.NextDouble());
        }
    }
    public void TrainLimbOfType(ItemProperty t)
    {
        List<BodyPart> parts = GetBodyPartsBySlot(t);

        if (parts.Count > 0)
        {
            parts.GetRandom().AddXP(entity, SeedManager.combatRandom.NextDouble());
        }
    }

    public List<BodyPart.Grip> AllGrips()
    {
        List<BodyPart.Grip> allGrips = new List<BodyPart.Grip>();

        for (int i = 0; i < bodyParts.Count; i++)
        {
            if (bodyParts[i].grip != null && bodyParts[i].grip.heldPart != null)
            {
                allGrips.Add(bodyParts[i].grip);
            }
        }

        return allGrips;
    }

    public List<BodyPart.Grip> AllGripsAgainst()
    {
        List<BodyPart.Grip> allGrips = new List<BodyPart.Grip>();

        if (bodyParts != null)
        {
            for (int i = 0; i < bodyParts.Count; i++)
            {
                if (bodyParts[i].holdsOnMe != null && bodyParts[i].holdsOnMe.Count > 0)
                {
                    allGrips.AddRange(bodyParts[i].holdsOnMe);
                }
            }
        }

        return allGrips;
    }

    public bool FreeToMove()
    {
        bool canMove = true;

        foreach (BodyPart bp in bodyParts)
        {
            if (!bp.FreeToMove())
            {
                canMove = false;
                bp.TryBreakGrips();
            }
        }

        return canMove;
    }

    public void ReleaseAllGrips(bool forced)
    {
        foreach (BodyPart bp in bodyParts)
        {
            bp.ReleaseGrip(forced);
        }
    }

    public void CheckGripIntegrities()
    {
        foreach (BodyPart bp in bodyParts)
        {
            if (bp.grip == null)
            {
                continue;
            }

            bool goodIntegrity = (bp.grip != null && bp.grip.heldPart != null && bp.grip.HeldBody != null && bp.grip.HeldBody.entity.myPos.DistanceTo(entity.myPos) < 2);

            if (goodIntegrity)
            {
                if (bp.grip.GripBroken())
                {
                    string message = LocalizationManager.GetContent("Gr_BreakGrip");
                    message = message.Replace("[ATTACKER]", entity.MyName);
                    message = message.Replace("[DEFENDER]", bp.grip.HeldBody.entity.MyName);
                    message = (!entity.isPlayer ? "<color=cyan>" : "<color=orange>") + message;
                    CombatLog.NewMessage(message + "</color>");

                    bp.ReleaseGrip(true);
                }
                else if (bp.grip != null && bp.grip.heldPart != null && bp.grip.HeldBody != null)
                {
                    bp.grip.HeldBody.entity.cell.UnSetEntity(bp.grip.HeldBody.entity);
                    bp.grip.HeldBody.entity.myPos = entity.myPos;
                    bp.grip.HeldBody.entity.SetCell();
                }
                else
                {
                    bp.ReleaseGrip(true);
                }
            }
            else
            {
                bp.ReleaseGrip(true);
            }
        }
    }

    /// <summary>
    /// Severs the limb, but does not remove it from the body part list (unless external limb)
    /// </summary>
    public void RemoveLimb(BodyPart b)
    {
        if (bodyParts.Contains(b) && b.isAttached)
        {
            RemoveLimb(bodyParts.FindIndex(x => x == b));
        }
    }

    public void RemoveLimb(int id)
    {
        if (!bodyParts[id].isAttached || !bodyParts[id].severable)
        {
            return;
        }

        bodyParts[id].Sever(entity);

        if (bodyParts[id].slot == ItemProperty.Slot_Arm)
        {
            if (bodyParts[id].hand != null)
            {
                Item item = bodyParts[id].hand.EquippedItem;

                if (item.lootable)
                {
                    MyInventory.PickupItem(item, true);
                    CombatLog.NameItemMessage("Lose_Hold", entity.Name, item.DisplayName());
                }

                bodyParts[id].hand.SetEquippedItem(ItemList.GetItemByID("stump"), entity);
            }
        }

        if (bodyParts[id].equippedItem.lootable)
        {
            if (!MyInventory.isNoneItem(bodyParts[id].equippedItem))
            {
                Item i = bodyParts[id].equippedItem;

                MyInventory.UnEquipArmor(bodyParts[id], true);
                MyInventory.Drop(i);
                CombatLog.NameMessage("Item_Removed", i.DisplayName());
            }
        }

        if (bodyParts[id].external)
        {
            //Aizith external. Remove from list
            bodyParts.Remove(bodyParts[id]);
            Categorize(new List<BodyPart>(bodyParts));
            return;
        }

        Item partToDrop = new Item(ItemList.GetSeveredBodyPart(bodyParts[id]));

        if (partToDrop != null)
        {
            if (bodyParts[id].organic)
            {
                if (bodyParts[id].effect == TraitEffects.Leprosy || MyStats.hasTraitEffect(TraitEffects.Leprosy))
                    partToDrop.AddProperty(ItemProperty.OnAttach_Leprosy);
                else if (bodyParts[id].effect == TraitEffects.Crystallization || MyStats.hasTraitEffect(TraitEffects.Crystallization))
                    partToDrop.AddProperty(ItemProperty.OnAttach_Crystallization);

                if (entity.isPlayer || !entity.isPlayer && entity.AI.npcBase.HasFlag(NPC_Flags.Human))
                    partToDrop.AddProperty(ItemProperty.Cannibalism);

                partToDrop.displayName = gameObject.name + " " + partToDrop.Name;

                string handBaseItemID = "";

                if (bodyParts[id].hand != null)
                {
                    handBaseItemID = bodyParts[id].hand.baseItem;
                }

                CEquipped ce = new CEquipped(bodyParts[id].equippedItem.ID, handBaseItemID);
                partToDrop.AddComponent(ce);
                partToDrop.armor = bodyParts[id].armor;

                if (entity.isPlayer)
                {
                    partToDrop.statMods.Clear();

                    foreach (Stat_Modifier sm in bodyParts[id].Attributes)
                    {
                        partToDrop.statMods.Add(new Stat_Modifier(sm.Stat, sm.Amount));
                    }
                }
                else
                {
                    foreach (Stat_Modifier sm in partToDrop.statMods)
                    {
                        if (SeedManager.combatRandom.Next(100) < 30)
                        {
                            int mod = Mathf.Clamp(World.DangerLevel() / 5, 0, 5);
                            sm.Amount += SeedManager.combatRandom.Next(0, mod + 1);
                        }
                    }
                }
            }
            else
            {
                partToDrop = ItemList.GetItemByID("scrap");
            }

            MyInventory.PickupItem(partToDrop, true);
            MyInventory.DropBodyPart(partToDrop);

            bodyParts[id].equippedItem = ItemList.GetItemByID("stump");
            entity.CreateBloodstain(true);
        }

        if (bodyParts[id].slot == ItemProperty.Slot_Head)
        {
            List<BodyPart> heads = GetBodyPartsBySlot(ItemProperty.Slot_Head);

            if (heads.Count <= 1)
            {
                MyStats.Die();
                return;
            }
        }

        bodyParts[id].organic = true;
    }

    public List<BodyPart.Hand> Hands
    {
        get
        {
            List<BodyPart.Hand> hands = new List<BodyPart.Hand>();

            for (int i = 0; i < bodyParts.Count; i++)
            {
                if (bodyParts[i].hand != null && bodyParts[i].isAttached)
                {
                    hands.Add(bodyParts[i].hand);
                }
            }

            return hands;
        }
    }

    public BodyPart GetBodyPartBySlot(ItemProperty sl)
    {
        return (bodyParts.Find(x => x.slot == sl));
    }

    public List<BodyPart> GetBodyPartsBySlot(ItemProperty sl)
    {
        return (bodyParts.FindAll(x => x.slot == sl));
    }

    public List<BodyPart.Hand> FreeHands()
    {
        return Hands.FindAll(x => x.IsAttached && (x.EquippedItem.ID == x.baseItem));
    }

    public List<BodyPart> AttachedArms()
    {
        return bodyParts.FindAll(x => x.slot == ItemProperty.Slot_Arm && x.isAttached);
    }

    public List<BodyPart> GrippableLimbs()
    {
        List<BodyPart> parts = new List<BodyPart>();

        for (int i = 0; i < bodyParts.Count; i++)
        {
            if (bodyParts[i].isAttached && bodyParts[i].hand != null)
            {
                parts.Add(bodyParts[i]);
            }
        }

        return parts;
    }

    public void AttachLimb(int id)
    {
        if (bodyParts[id].isAttached)
        {
            return;
        }

        bodyParts[id].Attach(MyStats);
        bodyParts[id].myBody = this;

        if (bodyParts[id].slot == ItemProperty.Slot_Arm)
        {
            bodyParts[id].hand.SetEquippedItem(ItemList.GetItemByID("fists"), entity);
        }

        bodyParts[id].equippedItem = ItemList.GetNone();
        bodyParts[id].SetXP(0.0, 1000.0);

        CombatLog.NameMessage("Grow_Back_Limb", bodyParts[id].displayName);
    }

    public void RegrowLimbs()
    {
        for (int i = 0; i < bodyParts.Count; i++)
        {
            AttachLimb(i);
        }
    }

    public void Categorize(List<BodyPart> parts)
    {
        bodyParts = CharacterCreation.SortBodyParts(parts);
    }

    public void GetFromBuilder(PlayerBuilder builder)
    {
        if (builder.bodyParts != null && builder.bodyParts.Count > 0)
        {
            bodyParts = new List<BodyPart>();

            for (int i = 0; i < builder.bodyParts.Count; i++)
            {
                bodyParts.Add(new BodyPart(builder.bodyParts[i]));
            }
        }

        StatInitializer.GetPlayerStats(MyStats, Manager.playerBuilder);

        if (MyStats != null)
        {
            for (int i = 0; i < bodyParts.Count; i++)
            {
                if (bodyParts[i] == null)
                {
                    continue;
                }

                if (bodyParts[i].equippedItem == null)
                {
                    bodyParts[i].equippedItem = ItemList.GetNone();
                }

                bodyParts[i].myBody = this;
                bodyParts[i].equippedItem.UpdateUserSprite(MyStats, false);

                if (bodyParts[i].equippedItem.statMods.Find(x => x.Stat == "Heat Resist") != null)
                    MyStats.Attributes["Heat Resist"] += bodyParts[i].equippedItem.statMods.Find(x => x.Stat == "Heat Resist").Amount;
                if (bodyParts[i].equippedItem.statMods.Find(x => x.Stat == "Cold Resist") != null)
                    MyStats.Attributes["Cold Resist"] += bodyParts[i].equippedItem.statMods.Find(x => x.Stat == "Cold Resist").Amount;
                if (bodyParts[i].equippedItem.statMods.Find(x => x.Stat == "Energy Resist") != null)
                    MyStats.Attributes["Energy Resist"] += bodyParts[i].equippedItem.statMods.Find(x => x.Stat == "Energy Resist").Amount;
                if (bodyParts[i].equippedItem.statMods.Find(x => x.Stat == "Storage Capacity") != null)
                    MyInventory.AddRemoveStorage(bodyParts[i].equippedItem.statMods.Find(x => x.Stat == "Storage Capacity").Amount);
            }

            if (builder.handItems != null && builder.handItems.Count > 0)
            {
                for (int i = 0; i < Hands.Count; i++)
                {
                    if (builder.handItems.Count > i && builder.handItems[i] != null)
                    {
                        Hands[i].SetEquippedItem(new Item(builder.handItems[i]), entity);
                    }
                }
            }

            if (Manager.newGame && Manager.profName == "Experiment")
            {
                for (int i = 0; i < 3; i++)
                {
                    MyStats.ForceMutation();
                }
            }

            Categorize(bodyParts);
        }

        defaultHand = new BodyPart.Hand(GetBodyPartBySlot(ItemProperty.Slot_Head), ItemList.GetItemByID("stump"), "stump");
    }

    public enum LimbSlot
    {
        None, Head, Chest, Back, Arm, Leg, Tail, Wing
    }
}