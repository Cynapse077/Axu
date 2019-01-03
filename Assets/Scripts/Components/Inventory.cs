using UnityEngine;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Inventory : MonoBehaviour
{
    public static int BodyDropChance = 5;

    [HideInInspector]
    public int gold = 0;
    public List<Item> items = new List<Item>();
    public Entity entity;

    Item _mainHand, _offHand, _firearm;
    int _maxItems = 15;

    Stats stats
    {
        get { return entity.stats; }
    }
    Body body
    {
        get { return entity.body; }
    }

    public int maxItems
    {
        get
        {
            if (entity != null && !entity.isPlayer)
            {
                return 25;
            }

            return (entity != null) ? _maxItems + stats.Strength - 1 : _maxItems;
        }
    }

    public Item firearm
    {
        get
        {
            return _firearm;
        }
        set
        {
            if (_firearm != null && entity != null)
            {
                _firearm.OnUnequip(entity);
            }

            _firearm = value;

            if (entity != null && entity.isPlayer)
            {
                GameObject.FindObjectOfType<AmmoPanel>().Display(_firearm.ID != "none");
            }

            if (stats != null && _firearm != null)
            {
                _firearm.OnEquip(stats);
            }
        }
    }

    public void Init()
    {
        if (entity.isPlayer)
            GetFromBuilder(Manager.playerBuilder);
        else
            GetFromNPC();
    }

    public void SetStorage(int amount)
    {
        _maxItems = amount;
    }

    public void AddRemoveStorage(int amount)
    {
        _maxItems += amount;
    }

    public static List<Item> GetDrops(int numItems)
    {
        List<Item> inv = new List<Item>();

        for (int i = 0; i < numItems; i++)
        {
            int rarity = 1;

            for (int j = 0; j < ItemList.MaxRarity; j++)
            {
                if (SeedManager.combatRandom.Next(1000) < j * World.DangerLevel())
                    rarity++;
            }

            Item item = ItemList.GetItemByRarity(rarity);

            if (rarity >= ItemList.MaxRarity && SeedManager.combatRandom.Next(150) < 10 || SeedManager.combatRandom.Next(500) == 1)
                item = ItemList.GetRandart(item);

            inv.Add(item);
        }

        return inv;
    }

    public bool CanFly()
    {
        if (stats == null || stats.statusEffects == null || stats.HasEffect("Topple") || stats.HasEffect("Unconscious"))
            return false;
        if (!entity.isPlayer && entity.AI.npcBase.HasFlag(NPC_Flags.Flying) || stats.HasEffect("Float"))
            return true;
        if (EquippedItems() != null && EquippedItems().Find(x => x.HasProp(ItemProperty.Flying)) != null)
            return true;

        return body.GetBodyPartsBySlot(ItemProperty.Slot_Wing).FindAll(x => x.isAttached).Count > 0;
    }

    public void ThrowItem(Coord destination, Item i, Explosive exp)
    {
        if (i.HasProp(ItemProperty.Explosive))
        {
            exp.DetonateExplosion(i.damageTypes, entity);
            RemoveInstance(i);
            return;
        }

        exp.DetonateOneTile(entity);

        if (i.HasCComponent<CLiquidContainer>() && World.tileMap.GetCellAt(destination) != null && World.tileMap.GetCellAt(destination).entity != null)
        {
            i.GetCComponent<CLiquidContainer>().Pour(World.tileMap.GetCellAt(destination).entity);

            if (!i.HasProp(ItemProperty.Quest_Item) && !i.HasProp(ItemProperty.Artifact))
            {
                RemoveInstance(i);
                Destroy(exp.gameObject);
                CombatLog.NameMessage("Item_Impact_Shatter", i.Name);
                return;
            }
        }

        if (SeedManager.combatRandom.Next(100) < 12 + stats.proficiencies.Throwing.level)
        {
            if (!i.HasProp(ItemProperty.Quest_Item) && !i.HasProp(ItemProperty.Artifact))
            {
                RemoveInstance(i);
                Destroy(exp.gameObject);
                CombatLog.NameMessage("Item_Impact_Shatter", i.Name);
                return;
            }
        }

        //If it didn't break or explode
        if (i.lootable)
        {
            Item newItem = new Item(i) { amount = 1 };
            Inventory otherInventory = CheckForInventories(destination);

            if (otherInventory == null)
            {
                MapObject m = World.objectManager.NewObjectAtOtherScreen("Loot", destination, World.tileMap.WorldPosition, World.tileMap.currentElevation);
                m.inv.Add(newItem);
            }
            else
                otherInventory.PickupItem(newItem);
        }

        RemoveInstance(i);
    }

    public void Ready(Item i)
    {
        EquipFirearm(i);
    }

    public void Equip(Item i)
    {
        if (i.HasProp(ItemProperty.Ranged))
        {
            EquipFirearm(i);
            return;
        }

        List<BodyPart> parts = body.bodyParts.FindAll(x => x.slot == i.GetSlot());

        if (parts.Count == 1)
            EquipDirectlyToBodyPart(i, body.bodyParts.Find(x => x.slot == i.GetSlot()));
    }

    public void EquipDirectlyToBodyPart(Item i, BodyPart b)
    {
        if (!b.canWearGear || !b.isAttached || b.equippedItem.HasProp(ItemProperty.Cannot_Remove))
            return;

        if (b.equippedItem != null && !isNoneItem(b.equippedItem) && b.equippedItem.lootable)
            UnEquipArmor(b, true);

        b.equippedItem = i;
        i.OnEquip(stats);
        RemoveInstance(i);
        World.soundManager.UseItem();
    }

    public void EquipFirearm(Item i, bool cancelMessage = false)
    {
        if (firearm.HasProp(ItemProperty.Cannot_Remove))
            return;

        Item itemToPickup = new Item(firearm);
        firearm = new Item(i);
        i.OnEquip(stats);
        RemoveInstance_All(i);
        PickupItem(itemToPickup, false);

        if (entity.isPlayer && i.statMods.Find(x => x.Stat == "Light") != null)
            World.tileMap.LightCheck();

        if (World.soundManager != null)
            World.soundManager.UseItem();
    }

    public void UnEquipFirearm(bool cancelMessage = false)
    {
        if (firearm == null)
            return;

        if (!isNoneItem(firearm))
        {
            if (firearm.HasProp(ItemProperty.Cannot_Remove))
            {
                if (!cancelMessage)
                    Alert.NewAlert("Cannot_Remove", UIWindow.Inventory);

                return;
            }

            if (firearm.amount > 0)
                PickupItem(firearm, true);

            firearm = ItemList.GetNone();
        }
    }

    public void Attach(Item i)
    {
        CombatLog.NameMessage("Attach_Limb", i.DisplayName());

        i.RunCommands("OnAttach");
        i.OnEquip(stats, false);
        RemoveInstance(i);
    }

    public bool TwoHandPenalty(BodyPart.Hand hand)
    {
        if (hand == null || body == null || hand.arm == null)
            return false;

        return (body.FreeHands().Count == 0 && hand.arm.GetStatMod("Strength").Amount < 5);
    }

    public int BurdenPenalty()
    {
        if (!overCapacity() || !entity.isPlayer)
            return 0;

        return Mathf.Min((items.Count - maxItems) * 2, 10);
    }

    public void Wield(Item i, int armSlot)
    {
        List<BodyPart.Hand> hands = body.Hands;
        BodyPart.Hand hand = hands[armSlot];

        if (hands.Count == 0 || hand == null)
        {
            Alert.NewAlert("No_Hands", UIWindow.Inventory);
            return;
        }

        if (hand.EquippedItem.HasProp(ItemProperty.Cannot_Remove) && hand.EquippedItem.ID != hand.baseItem)
        {
            return;
        }

        if (hand.EquippedItem != null && hand.EquippedItem.lootable)
        {
            PickupItem(hand.EquippedItem, true);
        }

        hand.SetEquippedItem(new Item(i), entity);

        //Check for two handed weapons with low strength arms
        for (int h = 0; h < hands.Count; h++)
        {
            if (hands[h].EquippedItem.HasProp(ItemProperty.Two_Handed) && TwoHandPenalty(hands[h]))
            {
                CombatLog.NameMessage("Message_2_Handed_No_Req", hands[h].EquippedItem.DisplayName());
                break;
            }
        }

        i.RunCommands("OnEquip");
        i.amount = 1;
        RemoveInstance(i);

        if (entity.isPlayer && i.statMods.Find(x => x.Stat == "Light") != null)
        {
            World.tileMap.LightCheck();
        }

        if (World.soundManager != null)
        {
            World.soundManager.UseItem();
        }
    }

    public void UnEquipWeapon(Item i, int armSlot)
    {
        List<BodyPart.Hand> hands = body.Hands;
        BodyPart.Hand hand = hands[armSlot];

        if (!hand.EquippedItem.HasProp(ItemProperty.Cannot_Remove))
        {
            PickupItem(hand.EquippedItem);
            hand.SetEquippedItem(ItemList.GetItemByID(hand.baseItem), entity);
        }
    }

    public void UnEquipArmor(BodyPart b, bool overrideMax = false)
    {
        if (b.equippedItem.lootable && !b.equippedItem.HasProp(ItemProperty.Cannot_Remove))
        {
            b.equippedItem.OnUnequip(entity, false);
            PickupItem(b.equippedItem, overrideMax);
            b.equippedItem = ItemList.GetNone();
            entity.Wait();
        }
    }

    //This is only used by Return Pads to set the destination. 
    public void Set(Item i)
    {
        i.ApplyEffects(stats);
    }

    public void Use(Item i)
    {
        if (i.HasCComponent<CCharges>() && !i.UseCharge())
        {
            if (i.HasProp(ItemProperty.DestroyOnZeroCharges) && i.GetCComponent<CCharges>().current <= 0)
                RemoveInstance(i);
            else
                Alert.NewAlert("No_Charges", UIWindow.Inventory);
            return;
        }

        if (i.HasCComponent<CModKit>())
        {
            World.userInterface.ItemOnItem_Mod(i, this, i.GetCComponent<CModKit>());
            return;
        }

        i.RunCommands("OnUse");

        if (i.HasProp(ItemProperty.Selected_Tele))
        {
            i.ApplyEffects(stats);
            return;
        }

        if (i.HasProp(ItemProperty.ReplaceLimb))
            i.ApplyEffects(stats);
        if (i.HasProp(ItemProperty.Stop_Bleeding))
        {
            stats.RemoveStatusEffect("Bleed");
            RemoveInstance(i);
        }

        if (i.HasProp(ItemProperty.Blink))
        {
            Skill s = SkillList.GetSkillByID("blink");
            s.staminaCost = 0;
            s.timeCost = 10;
            GetComponent<PlayerInput>().UseSelectTileSkill(s);
        }
        if (i.HasProp(ItemProperty.Surface_Tele) && entity.TeleportToSurface())
        {
            RemoveInstance(i);
        }

        entity.Wait();
    }

    public void Read(Item i)
    {
        int timeCost = 100 - (stats.Intelligence * 2);
        timeCost = Mathf.Clamp(timeCost, 1, 100);

        if (i.HasProp(ItemProperty.Tome))
        {
            if (i.GetCComponent<CAbility>() != null)
            {
                string abName = i.GetCComponent<CAbility>().abID;

                EntitySkills eSkills = GetComponent<EntitySkills>();
                Skill skill = new Skill(SkillList.GetSkillByID(abName));

                if (eSkills.abilities.Find(x => x.ID == skill.ID) == null)
                {
                    Skill s = SkillList.GetSkillByID(abName);
                    s.SetFlag(Skill.AbilityOrigin.Book);

                    eSkills.AddSkill(s, Skill.AbilityOrigin.Book);
                    CombatLog.NameMessage("Learn_Skill", skill.Name);
                }
                else
                {
                    skill = eSkills.abilities.Find(x => x.ID == abName);

                    if (skill.level < Skill.maxLvl && skill.CanLevelUp)
                    {
                        skill.level++;
                        skill.XP = 0;
                        CombatLog.NameMessage("Increase_Skill", skill.Name);
                    }
                    else
                    {
                        Alert.NewAlert("Mastery");
                        return;
                    }
                }
            }
        }

        i.RunCommands("OnRead");
        items.Remove(i);
        entity.EndTurn(0.1f, timeCost);
    }

    public List<Item> Items_ThrowingFirst()
    {
        List<Item> tItems = new List<Item>();
        List<Item> ntItems = new List<Item>();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].HasProp(ItemProperty.Throwing_Wep) || items[i].GetCComponent<CLiquidContainer>() != null)
                tItems.Add(items[i]);
            else
                ntItems.Add(items[i]);
        }

        tItems.AddRange(ntItems);

        return tItems;
    }

    public bool CanPickupItem(Item i)
    {
        if (!i.lootable || i.HasProp(ItemProperty.Pool))
            return false;

        return true;
    }

    public void PickupItem(Item i, bool fromFirearm = false)
    {
        if (i == null || !i.lootable)
            return;

        if (i.stackable)
        {
            if (firearm != null && i.ID == firearm.ID && !fromFirearm)
            {
                firearm.amount += i.amount;
                World.objectManager.UpdateDialogueOptions();
                return;
            }
            for (int x = 0; x < items.Count; x++)
            {
                if (items[x].DisplayName() == i.DisplayName() && items[x].ID == i.ID)
                {
                    if (i.HasCComponent<CCharges>())
                    {
                        if (i.GetCComponent<CCharges>().current == items[x].GetCComponent<CCharges>().current)
                        {
                            items[x].amount += i.amount;
                            World.objectManager.UpdateDialogueOptions();
                            return;
                        }
                    }
                    else if (i.HasCComponent<CRot>())
                    {
                        if (i.GetCComponent<CRot>().current == items[x].GetCComponent<CRot>().current)
                        {
                            items[x].amount += i.amount;
                            World.objectManager.UpdateDialogueOptions();
                            return;
                        }
                    }
                    else
                    {
                        items[x].amount += i.amount;
                        World.objectManager.UpdateDialogueOptions();
                        return;
                    }

                }
            }
        }

        World.objectManager.UpdateDialogueOptions();
        items.Add(i);

        if (overCapacity() && entity.isPlayer)
        {
            CombatLog.SimpleMessage("Message_Overburdened");
        }
    }

    public void RemoveInstance(Item i)
    {
        if (i == firearm)
        {
            if (i.stackable && i.amount > 0)
                i.amount--;
            else
            {
                firearm.OnUnequip(entity, false);
                firearm = ItemList.GetNone();
            }
        }
        else if (items.Contains(i))
        {
            if (i.stackable && i.amount > 1)
            {
                i.amount--;
                return;
            }

            items.Remove(i);

            if (entity == null && gameObject)
            {
                MapObjectSprite mos = GetComponent<MapObjectSprite>();

                if (mos != null)
                {
                    mos.UpdateVisuals();
                }
            }
        }
    }

    public void RemoveInstance_All(Item i)
    {
        if (items.Contains(i))
            items.Remove(i);
    }

    public void Disarm()
    {
        if (body.MainHand.EquippedItem.lootable)
        {
            Item i = body.MainHand.EquippedItem;
            PickupItem(i, false);
            body.MainHand.SetEquippedItem(ItemList.GetItemByID(body.MainHand.baseItem), entity);
            Drop(i);
        }
    }

    public bool Reload(Item i)
    {
        if (!i.HasCComponent<CFirearm>())
            return false;

        Item ammo = items.Find(x => x.HasProp(ItemProperty.Ammunition) && x.ID == i.GetCComponent<CFirearm>().ammoID);

        if (ammo == null)
            return false;

        int ammoAmount = ammo.amount;
        ammo.amount -= i.Reload(ammoAmount);

        if (ammo.amount <= 0)
        {
            RemoveInstance(ammo);
        }

        if (!i.ContainsProperty(ItemProperty.Bow))
        {
            World.soundManager.Reload();
        }

        return true;
    }

    public bool Unload(Item it)
    {
        if (it.Charges() == 0)
        {
            return false;
        }

        Item bullet = ItemList.GetItemByID(it.GetCComponent<CFirearm>().ammoID);
        bullet.amount = it.Charges();
        it.Unload();

        PickupItem(bullet);
        return true;
    }

    public void Butcher(Item item)
    {
        if (!item.HasCComponent<CCorpse>())
        {
            return;
        }

        CCorpse corpse = item.GetCComponent<CCorpse>();

        for (int i = 0; i < corpse.parts.Count; i++)
        {
            if (!corpse.parts[i].Att)
            {
                continue;
            }

            int randomNumer = SeedManager.combatRandom.Next(100);
            int butcheryLevel = stats.proficiencies.Butchery.level;

            BodyPart sample = new BodyPart(corpse.owner + "'s " + corpse.parts[i].Name, true, corpse.parts[i].Slot);
            Item it = ItemList.GetSeveredBodyPart(sample);

            if (it == null)
            {
                continue;
            }

            it.displayName = sample.name;

            string handBaseItemID = "";

            if (corpse.parts[i].Hnd != null)
            {
                handBaseItemID = corpse.parts[i].Hnd.bItem;
            }

            CEquipped ce = new CEquipped(corpse.parts[i].item.ID, handBaseItemID);
            it.AddComponent(ce);

            if (item.HasCComponent<CRot>() && it.HasCComponent<CRot>())
            {
                it.GetCComponent<CRot>().current = item.GetCComponent<CRot>().current;
            }

            if (corpse.parts[i].Org)
            {
                if (randomNumer > (butcheryLevel + 1) * 10)
                {
                    it = (randomNumer > 60) ? ItemList.GetItemByID("fleshraw") : null;
                }
                else
                {
                    foreach (Stat_Modifier sm in it.statMods)
                    {
                        if (sm.Stat != "Hunger")
                        {
                            if (SeedManager.combatRandom.CoinFlip())
                                sm.Amount += SeedManager.combatRandom.Next(0, 2);
                            else if (SeedManager.combatRandom.Next(12) < butcheryLevel)
                                sm.Amount += SeedManager.combatRandom.Next(1, 5);
                        }
                    }
                }

                if (it != null)
                {
                    if (corpse.parts[i].Dis == TraitEffects.Leprosy || corpse.lep)
                        it.AddProperty(ItemProperty.OnAttach_Leprosy);
                    else if (corpse.parts[i].Dis == TraitEffects.Crystallization)
                        it.AddProperty(ItemProperty.OnAttach_Crystallization);

                    if (corpse.cann)
                    {
                        it.AddProperty(ItemProperty.Cannibalism);
                    }
                }
            }
            else
            {
                it = ItemList.GetItemByID("scrap");
            }

            if (it != null)
            {
                DropBodyPart(it);
            }
        }

        item.RunCommands("OnButcher");
        CombatLog.SimpleMessage("Butcher_Corpse");
        stats.AddProficiencyXP(stats.proficiencies.Butchery, SeedManager.localRandom.Next(3, 6));
        entity.CreateBloodstain(true, 100);
        RemoveInstance(item);
    }

    public void Eat(Item i)
    {
        Consume(i);
        World.soundManager.Eat();
    }

    public void Drink(Item i)
    {
        Consume(i);
        World.soundManager.Drink();
    }

    void Consume(Item i)
    {
        i.OnConsume(stats);

        if (i.HasCComponent<CLiquidContainer>())
            CombatLog.NameMessage("Message_Action_Drink", i.DisplayName());
        else
        {
            RemoveInstance(i);
            CombatLog.NameMessage("Message_Action_Consume", i.DisplayName());
        }

        entity.Wait();
    }

    public void Mix(Item i)
    {
        World.userInterface.ItemOnItem_Fill(i, this);
    }

    public void Pour(Item i)
    {
        if (i.HasCComponent<CLiquidContainer>())
        {
            World.userInterface.PourActions(i);
        }
    }

    Inventory CheckForInventories(Coord pos)
    {
        Cell c = World.tileMap.GetCellAt(pos);

        if (c != null)
        {
            for (int i = 0; i < c.mapObjects.Count; i++)
            {
                if (c.mapObjects[i].inv != null)
                    return c.mapObjects[i].inv;
            }
        }

        return null;
    }

    public void Drop(Item i)
    {
        if (entity == null || World.userInterface.CurrentState() == UIWindow.Inventory && !entity.isPlayer && i.HasProp(ItemProperty.Unique))
            return;

        Inventory otherInventory = CheckForInventories(entity.myPos);

        if (i.lootable)
        {
            Item newItem = new Item(i) { amount = 1 };

            if (otherInventory != null)
                otherInventory.PickupItem(newItem);
            else
            {
                World.objectManager.NewInventory("Loot", new Coord(entity.myPos), World.tileMap.WorldPosition, World.tileMap.currentElevation, new List<Item>() { newItem });
            }
        }

        RemoveInstance(i);
    }

    public void DropBodyPart(Item i)
    {
        if (!i.lootable)
        {
            items.Remove(i);
            return;
        }

        Coord dropPos = RandomOpenDropLocation();
        Inventory otherInventory = CheckForInventories(dropPos);

        if (otherInventory == null)
        {
            World.objectManager.NewInventory("Loot", dropPos, World.tileMap.WorldPosition, World.tileMap.currentElevation, new List<Item>() { i });
        }
        else
        {
            otherInventory.PickupItem(i);
        }

        if (items.Contains(i))
        {
            items.Remove(i);
        }
    }

    public void DropAllOfType(Item i)
    {
        if (entity == null)
            return;

        if (i.lootable)
        {
            Inventory otherInventory = CheckForInventories(entity.myPos);
            Item newItem = new Item(i);

            if (otherInventory != null)
                otherInventory.PickupItem(newItem);
            else
            {
                World.objectManager.NewInventory("Loot", entity.myPos, World.tileMap.WorldPosition, World.tileMap.currentElevation, new List<Item>() { newItem });
            }
        }

        RemoveInstance_All(i);
    }

    public void DropAll()
    {
        if (entity.isPlayer || entity.AI.npcBase.HasFlag(NPC_Flags.Deteriortate_HP))
        {
            return;
        }

        //drop corpse
        if (!entity.isPlayer && SeedManager.combatRandom.Next(100) < BodyDropChance + ObjectManager.playerEntity.stats.proficiencies.Butchery.level)
        {
            BaseAI bai = entity.AI ?? GetComponent<BaseAI>();
            Item corpseItem;

            if (bai.npcBase.corpseItem != null)
                corpseItem = ItemList.GetItemByID(bai.npcBase.corpseItem);
            else
                corpseItem = ItemList.GetItemByID("corpse_norm");

            if (!bai.npcBase.HasFlag(NPC_Flags.Deteriortate_HP) && !bai.npcBase.HasFlag(NPC_Flags.No_Body))
            {
                CCorpse co = new CCorpse(new List<BodyPart>(body.bodyParts), gameObject.name, bai.npcBase.HasFlag(NPC_Flags.Human),
                    bai.npcBase.HasFlag(NPC_Flags.Radiation), bai.npcBase.HasFlag(NPC_Flags.Skills_Leprosy));

                corpseItem.AddComponent(co);
                corpseItem.AddProperty(ItemProperty.Corpse);
                corpseItem.displayName = gameObject.name + "'s Corpse";

                if (bai.npcBase.HasFlag(NPC_Flags.Human) && !corpseItem.HasProp(ItemProperty.Cannibalism))
                {
                    corpseItem.AddProperty(ItemProperty.Cannibalism);
                }

                PickupItem(corpseItem, false);
            }
        }

        //Drop equipped weapons
        List<BodyPart.Hand> hands = body.Hands;

        for (int i = 0; i < hands.Count; i++)
        {
            PickupItem(hands[i].EquippedItem, true);
        }

        PickupItem(firearm, true);

        if (!entity.isPlayer && entity.AI.npcBase.HasFlag(NPC_Flags.Human) && items.Count < 2 && SeedManager.combatRandom.Next(100) < 5)
        {
            items = GetDrops(SeedManager.combatRandom.Next(0, 4));
        }

        if (items.Count > 0)
        {
            Inventory otherInventory = CheckForInventories(entity.myPos);

            if (otherInventory == null)
            {
                MapObject m = World.objectManager.NewObjectAtOtherScreen("Loot", entity.myPos, World.tileMap.WorldPosition, World.tileMap.currentElevation);

                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null && items[i].lootable)
                        m.inv.Add(items[i]);
                }
            }
            else
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null && items[i].lootable)
                        otherInventory.PickupItem(items[i], true);
                }
            }
        }

        World.objectManager.CheckMapObjectInventories();
    }

    public int ArmorProfLevelFromBP(BodyPart bp)
    {
        return (stats.CheckProficiencies(bp.equippedItem).level);
    }

    /// <summary>
    /// All equipped items, including weapon, off-hand and firearm.
    /// </summary>
    public List<Item> EquippedItems()
    {
        List<Item> eItems = new List<Item>();

        if (entity == null)
            return eItems;

        List<BodyPart.Hand> hands = body.Hands;

        for (int i = 0; i < hands.Count; i++)
        {
            if (hands[i].EquippedItem == null)
            {
                hands[i].SetEquippedItem(ItemList.GetItemByID(hands[i].baseItem), entity);
            }

            eItems.Add(hands[i].EquippedItem);
        }

        if (firearm == null)
            firearm = ItemList.GetNone();

        eItems.Add(firearm);

        if (body.bodyParts == null)
            return eItems;

        for (int i = 0; i < body.bodyParts.Count; i++)
        {
            eItems.Add(body.bodyParts[i].equippedItem);
        }

        return eItems;
    }

    public Coord RandomOpenDropLocation()
    {
        List<Coord> possibleDropCoords = new List<Coord>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                if (World.tileMap.WalkableTile(entity.posX + x, entity.posY + y))
                    possibleDropCoords.Add(new Coord(entity.posX + x, entity.posY + y));
            }
        }

        return (possibleDropCoords.Count > 0) ? possibleDropCoords.GetRandom() : entity.myPos;
    }

    public bool canAfford(int cost)
    {
        return (gold >= cost);
    }

    public bool overCapacity()
    {
        return (items.Count > maxItems);
    }

    public bool isNoneItem(Item i)
    {
        return (i.ID == ItemList.noneItem.ID);
    }

    public bool DiggingEquipped()
    {
        return (EquippedItems().Find(x => x.HasProp(ItemProperty.Dig)) != null);
    }

    void GetFromNPC()
    {
        BaseAI aibase = GetComponent<BaseAI>();
        body.bodyParts = new List<BodyPart>();

        for (int i = 0; i < aibase.npcBase.inventory.Count; i++)
        {
            PickupItem(aibase.npcBase.inventory[i]);
        }

        for (int i = 0; i < aibase.npcBase.bodyParts.Count; i++)
        {
            body.bodyParts.Add(new BodyPart(aibase.npcBase.bodyParts[i]));
        }

        Item handItem = (aibase.npcBase.handItems.Count > 0 && aibase.npcBase.handItems[0] != null) ? aibase.npcBase.handItems[0] : ItemList.GetItemByName("fists");

        body.defaultHand = new BodyPart.Hand(body.GetBodyPartBySlot(ItemProperty.Slot_Head), handItem, handItem.ID);

        for (int i = 0; i < aibase.npcBase.handItems.Count; i++)
        {
            if (body.Hands.Count > i && body.Hands[i] != null && body.Hands[i].IsAttached)
            {
                body.Hands[i].SetEquippedItem(aibase.npcBase.handItems[i], entity);

                if (aibase.npcBase.handItems[i].HasProp(ItemProperty.Cannot_Remove))
                {
                    body.Hands[i].baseItem = aibase.npcBase.handItems[i].ID;
                }
            }
        }

        firearm = aibase.npcBase.firearm;

        if (body.bodyParts == null || body.bodyParts.Count <= 0)
        {
            body.bodyParts = EntityList.DefaultBodyStructure();
        }

        body.InitializeBody();
    }

    public void GetFromBuilder(PlayerBuilder builder)
    {
        items.Clear();
        firearm = builder.firearm;

        if (builder.items != null && builder.items.Count > 0)
        {
            for (int i = 0; i < Manager.playerBuilder.items.Count; i++)
            {
                items.Add(new Item(Manager.playerBuilder.items[i]));
            }
        }

        if (builder.bodyParts != null && builder.bodyParts.Count > 0)
        {
            body.bodyParts = new List<BodyPart>();

            for (int i = 0; i < builder.bodyParts.Count; i++)
            {
                body.bodyParts.Add(new BodyPart(builder.bodyParts[i]));
            }
        }

        body.GetFromBuilder(builder);
        body.defaultHand = new BodyPart.Hand(body.GetBodyPartBySlot(ItemProperty.Slot_Head), ItemList.GetItemByID("stump"), "stump");
        gold = builder.money;
        GameObject.FindObjectOfType<AmmoPanel>().Display(firearm != null && firearm.ID != "none");
    }
}
