using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Inventory : MonoBehaviour
{
    public static int BodyDropChance = 5;

    public int gold = 0;
    public List<Item> items = new List<Item>();
    public Entity entity;

    Item _firearm;
    int _maxItems = 15;

    Stats stats { get { return entity.stats; } }
    Body body { get { return entity.body; } }

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
        set
        {
            //Used only for map object inventories.
            if (entity == null)
            {
                _maxItems = value;
            }
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
                _firearm.OnUnequip(entity, false);
            }

            _firearm = value;

            if (entity != null && entity.isPlayer)
            {
                GameObject.FindObjectOfType<AmmoPanel>().Display(_firearm.IsNullOrDefault());
            }

            if (stats != null && _firearm != null)
            {
                _firearm.OnEquip(stats, false);
            }
        }
    }

    public void Init()
    {
        if (entity.isPlayer)
        {
            GetFromBuilder(Manager.playerBuilder);
        }
        else
        {
            GetFromNPC();
        }
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

            for (int j = 0; j < ItemUtility.MaxRarity; j++)
            {
                if (RNG.Next(1000) < j * World.DangerLevel())
                {
                    rarity++;
                }
            }

            Item item = ItemList.GetItemByRarity(rarity);

            if (rarity >= ItemUtility.MaxRarity && RNG.OneIn(15) || RNG.OneIn(500))
            {
                item = ItemList.GetRandart(item);
            }

            inv.Add(item);
        }

        return inv;
    }

    public bool CanFly()
    {
        if (stats == null || stats.statusEffects == null || stats.HasEffect("Topple") || stats.HasEffect("Unconscious"))
        {
            return false;
        }

        if (!entity.isPlayer && entity.AI.npcBase.HasFlag(NPC_Flags.Flying) || stats.HasEffect("Float") 
            || EquippedItems().Find(x => x.HasProp(ItemProperty.Flying)) != null)
        {
            return true;
        }

        return body.GetBodyPartsBySlot(ItemProperty.Slot_Wing).FindAll(x => x.Attached).Count > 0;
    }

    public bool HasItem(string id)
    {
        return items.Any(x => x.ID == id);
    }

    //Called from lua
    public Item GetItem(string id)
    {
        return items.Find(x => x.ID == id);
    }

    public void ThrowItem(Coord destination, Item i, Explosive exp)
    {
        if (i.HasProp(ItemProperty.Explosive))
        {
            exp.DetonateExplosion(entity);
            RemoveInstance(i);
            return;
        }

        exp.DetonateOneTile(entity);

        if (i.TryGetCComponent(out CLiquidContainer cl) && World.tileMap.GetCellAt(destination) != null 
            && World.tileMap.GetCellAt(destination).entity != null)
        {
            if (cl.sLiquid != null)
            {
                for (int j = 0; j < cl.FilledUnits(); j++)
                {
                    if (cl.IsEmpty())
                    {
                        break;
                    }

                    cl.Pour(World.tileMap.GetCellAt(destination).entity);                    
                }                
            }

            if (!i.HasProp(ItemProperty.Quest_Item) && !i.HasProp(ItemProperty.Artifact))
            {
                RemoveInstance(i);
                Destroy(exp.gameObject);
                CombatLog.NameMessage("Item_Impact_Shatter", i.Name);
                return;
            }
        }
        else if (RNG.Next(100) < 12 + stats.proficiencies.Throwing.level)
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
                MapObject m = World.objectManager.NewObjectAtSpecificScreen("Loot", destination, World.tileMap.WorldPosition, World.tileMap.currentElevation);
                m.inv.Add(newItem);
            }
            else
            {
                otherInventory.PickupItem(newItem);
            }
        }

        RemoveInstance(i);
    }

    public void Ready(Item i)
    {
        EquipFirearm(i);
    }

    public void Equip(Item i)
    {
        if (i.TryGetCComponent(out CRequirement cr) && !cr.CanUse(stats))
        {
            Alert.NewAlert("CannotEquip", UIWindow.Inventory);
            return;
        }

        if (i.HasProp(ItemProperty.Ranged))
        {
            EquipFirearm(i);
            return;
        }

        List<BodyPart> parts = body.bodyParts.FindAll(x => x.slot == i.GetSlot());

        if (parts.Count == 1)
        {
            EquipDirectlyToBodyPart(i, body.bodyParts.Find(x => x.slot == i.GetSlot()));
        }
    }

    public void EquipDirectlyToBodyPart(Item i, BodyPart b)
    {
        if (!b.CanWearGear || !b.Attached || b.equippedItem.HasProp(ItemProperty.Cannot_Remove))
        {
            return;
        }

        if (i.TryGetCComponent(out CRequirement cr) && !cr.CanUse(stats))
        {
            Alert.NewAlert("CannotEquip", UIWindow.Inventory);
            return;
        }

        if (b.equippedItem != null && !IsNoneItem(b.equippedItem) && b.equippedItem.lootable)
        {
            UnEquipArmor(b, true);
        }

        b.equippedItem = i;
        i.OnEquip(stats, false);
        RemoveInstance(i);
        World.soundManager.UseItem();
    }

    public void EquipFirearm(Item i)
    {
        if (firearm.HasProp(ItemProperty.Cannot_Remove))
        {
            return;
        }

        if (i.TryGetCComponent(out CRequirement cr) && !cr.CanUse(stats))
        {
            Alert.NewAlert("CannotEquip", UIWindow.Inventory);
            return;
        }

        Item itemToPickup = new Item(firearm);
        firearm = new Item(i);
        i.OnEquip(stats, false);
        RemoveInstance_All(i);
        PickupItem(itemToPickup, false);

        if (entity.isPlayer && i.statMods.Any(x => x.Stat == "Light"))
        {
            World.tileMap.LightCheck();
        }

        if (World.soundManager != null)
        {
            World.soundManager.UseItem();
        }
    }

    public void UnEquipFirearm(bool cancelMessage = false)
    {
        if (firearm == null)
        {
            return;
        }

        if (!IsNoneItem(firearm))
        {
            if (firearm.HasProp(ItemProperty.Cannot_Remove))
            {
                if (!cancelMessage)
                {
                    Alert.NewAlert("Cannot_Remove", UIWindow.Inventory);
                }

                return;
            }

            if (firearm.amount > 0)
            {
                PickupItem(firearm, true);
            }

            firearm = ItemList.NoneItem;
        }
    }

    public void Attach(Item i)
    {
        CombatLog.NameMessage("Attach_Limb", i.DisplayName());

        i.RunCommands("OnAttach", entity);
        i.OnEquip(stats, false);
        RemoveInstance(i);
    }

    public bool TwoHandPenalty(BodyPart.Hand hand)
    {
        if (hand == null || body == null || hand.arm == null)
        {
            return false;
        }

        List<BodyPart.Hand> hands = body.Hands;
        int twoHanded = 0;

        for (int i = 0; i < hands.Count; i++)
        {
            if (hands[i].EquippedItem.HasProp(ItemProperty.Two_Handed) && hands[i].arm.GetStatMod("Strength").Amount < 6)
            {
                twoHanded++;
            }
        }

        return twoHanded > body.FreeHands().Count;
    }

    public int BurdenPenalty()
    {
        if (!OverCapacity() || !entity.isPlayer)
        {
            return 0;
        }

        return Mathf.Min((items.Count - maxItems) * 2, entity.stats.Speed - 1);
    }

    public void Wield(Item item, int armIndex)
    {
        List<BodyPart.Hand> hands = body.Hands;

        if (hands.Count == 0)
        {
            Alert.NewAlert("No_Hands", UIWindow.Inventory);
            return;
        }

        if (item.TryGetCComponent<CRequirement>(out var cr) && !cr.CanUse(stats))
        {
            Alert.NewAlert("CannotEquip", UIWindow.Inventory);
            return;
        }

        BodyPart.Hand hand = hands[armIndex];

        if (hand.EquippedItem.HasProp(ItemProperty.Cannot_Remove) && hand.EquippedItem.ID != hand.baseItem)
        {
            return;
        }

        if (hand.EquippedItem != null && hand.EquippedItem.lootable)
        {
            PickupItem(hand.EquippedItem, true);
        }

        hand.SetEquippedItem(new Item(item), entity);

        //Check for two handed weapons with low strength arms
        for (int h = 0; h < hands.Count; h++)
        {
            if (TwoHandPenalty(hands[h]))
            {
                CombatLog.NameMessage("Message_2_Handed_No_Req", hands[h].EquippedItem.DisplayName());
                break;
            }
        }

        item.amount = 1;
        RemoveInstance(item);

        if (entity.isPlayer && item.statMods.Any(x => x.Stat == "Light"))
        {
            World.tileMap.LightCheck();
        }

        if (World.soundManager != null)
        {
            World.soundManager.UseItem();
        }
    }

    public bool UnEquipWeapon(BodyPart.Hand hand)
    {
        if (!hand.EquippedItem.HasProp(ItemProperty.Cannot_Remove))
        {
            PickupItem(hand.EquippedItem);
            hand.RevertToBase(entity);
            return true;
        }

        return false;
    }

    public void UnEquipArmor(BodyPart b, bool overrideMax = false)
    {
        if (b.equippedItem.lootable && !b.equippedItem.HasProp(ItemProperty.Cannot_Remove))
        {
            b.equippedItem.OnUnequip(entity, false);
            PickupItem(b.equippedItem, overrideMax);
            b.equippedItem = ItemList.NoneItem;
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
        if (i.TryGetCComponent(out CCharges cc) && !i.UseCharge())
        {
            if (i.HasProp(ItemProperty.DestroyOnZeroCharges) && cc.current <= 0)
            {
                RemoveInstance(i);
            }
            else
            {
                Alert.NewAlert("No_Charges", UIWindow.Inventory);
            }

            return;
        }

        if (i.TryGetCComponent<CModKit>(out var cm))
        {
            World.userInterface.ItemOnItem_Mod(i, this, cm);
            return;
        }

        i.RunCommands("OnUse", entity);

        if (i.HasProp(ItemProperty.Selected_Tele))
        {
            i.ApplyEffects(stats);
            return;
        }

        if (i.HasProp(ItemProperty.ReplaceLimb))
        {
            i.ApplyEffects(stats);
        }

        if (i.HasProp(ItemProperty.Stop_Bleeding))
        {
            stats.RemoveStatusEffect("Bleed");
            RemoveInstance(i);
            return;
        }

        if (i.TryGetCComponent<CLocationMap>(out var cl))
        {
            cl.OnUse();
            RemoveInstance(i);
            return;
        }

        if (i.HasProp(ItemProperty.Blink) && GameData.TryGet<Ability>("blink", out var ab))
        {
            Ability s = new Ability(ab)
            {
                staminaCost = 0,
                timeCost = 10
            };
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
        if (i.TryGetCComponent<CRequirement>(out var cr) && !cr.CanUse(stats))
        {
            Alert.NewAlert("CannotRead");
            stats.AddStatusEffect("Confuse", Random.Range(6, 14));
            return;
        }

        i.RunCommands("OnRead", entity);

        //For skill books
        if (i.HasProp(ItemProperty.Tome))
        {
            if (i.TryGetCComponent<CAbility>(out var cab) && GameData.TryGet<Ability>(cab.abID, out var ability))
            {
                Ability skill = new Ability(ability);

                if (!entity.skills.abilities.Any(x => x.ID == skill.ID))
                {
                    entity.skills.AddSkill(skill, Ability.AbilityOrigin.Natrual);
                    CombatLog.NameMessage("Learn_Skill", skill.Name);
                }
                else
                {
                    skill = entity.skills.abilities.FirstOrDefault(x => x.ID == skill.ID);

                    if (skill.level < Ability.MaxLevel && skill.CanLevelUp)
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

            items.Remove(i);
        }

        entity.EndTurn(0.1f, 20);
    }

    public List<Item> Items_ThrowingFirst()
    {
        List<Item> tItems = new List<Item>();
        List<Item> ntItems = new List<Item>();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].HasProp(ItemProperty.Throwing_Wep) || 
                items[i].TryGetCComponent<CLiquidContainer>(out var cl) && cl.sLiquid != null)
            {
                tItems.Add(items[i]);
            }
            else
            {
                ntItems.Add(items[i]);
            }
        }

        tItems.AddRange(ntItems);

        return tItems;
    }

    public bool CanPickupItem(Item i)
    {
        return (i.lootable && !i.HasProp(ItemProperty.Pool));
    }

    public void PickupItem(Item i, bool fromFirearm = false)
    {
        if (i == null || !i.lootable)
        {
            return;
        }

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

        if (OverCapacity() && entity.isPlayer)
        {
            CombatLog.SimpleMessage("Message_Overburdened");
        }
    }

    public void RemoveInstance(Item i)
    {
        if (i == firearm)
        {
            if (i.stackable && i.amount > 0)
            {
                i.amount--;
            }
            else
            {
                firearm.OnUnequip(entity, false);
                firearm = ItemList.NoneItem;
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
        {
            items.Remove(i);
        }
    }

    public bool DisguisedAs(Faction faction)
    {
        var equipped = EquippedItems();

        for (int i = 0; i < equipped.Count; i++)
        {
            if (equipped[i].HasCComponent<CDisguise>())
            {
                CDisguise dis = equipped[i].GetCComponent<CDisguise>();

                if (dis.factionID == faction.ID)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public int DisguiseStrength(Faction faction)
    {
        var equipped = EquippedItems();
        int total = 0;

        for (int i = 0; i < equipped.Count; i++)
        {
            if (equipped[i].HasCComponent<CDisguise>())
            {
                CDisguise dis = equipped[i].GetCComponent<CDisguise>();

                if (dis.factionID == faction.ID)
                {
                    total += dis.strength;
                }
            }
        }

        return total;
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
        {
            return false;
        }

        //TODO: Select ammo type to reload with if there is more than one.
        Item ammo = items.Find(
            x => x.HasCComponent<CAmmo>()
            && x.GetCComponent<CAmmo>().AmmoType == i.GetCComponent<CFirearm>().ammoID);

        if (ammo == null)
        {
            return false;
        }

        int ammoAmount = ammo.amount;
        ammo.amount -= i.Reload(ammoAmount, ammo);

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

        Item ammo = ItemList.GetItemByID(it.GetCComponent<CFirearm>().ammoID);
        ammo.amount = it.Charges();
        it.Unload();

        PickupItem(ammo);
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

            BodyPart sample = new BodyPart(corpse.owner + "'s " + corpse.parts[i].Name, corpse.parts[i].Slot);
            Item newItem = ItemList.GetSeveredBodyPart(sample);

            if (newItem == null)
            {
                continue;
            }

            newItem.displayName = sample.name;

            string handBaseItemID = "";

            if (corpse.parts[i].Hnd != null)
            {
                handBaseItemID = corpse.parts[i].Hnd.bItem;
            }

            CEquipped ce = new CEquipped(corpse.parts[i].item.ID, handBaseItemID);
            newItem.AddComponent(ce);

            if (item.HasCComponent<CRot>() && newItem.HasCComponent<CRot>())
            {
                newItem.GetCComponent<CRot>().current = item.GetCComponent<CRot>().current;
            }

            if (!corpse.parts[i].Flgs.Contains(BodyPart.BPFlags.Synthetic))
            {
                if (randomNumer > (butcheryLevel + 1) * 10)
                {
                    newItem = (randomNumer > 40) ? ItemList.GetItemByID("fleshraw") : null;
                }
                else
                {
                    foreach (Stat_Modifier sm in newItem.statMods)
                    {
                        if (SeedManager.combatRandom.CoinFlip())
                            sm.Amount += SeedManager.combatRandom.Next(0, 2);
                        else if (SeedManager.combatRandom.Next(15) < butcheryLevel)
                            sm.Amount += SeedManager.combatRandom.Next(1, 5);
                    }
                }

                if (newItem != null)
                {
                    if (corpse.parts[i].Flgs.Contains(BodyPart.BPFlags.Leprosy) || corpse.lep)
                    {
                        newItem.AddProperty(ItemProperty.OnAttach_Leprosy);
                    }
                    else if (corpse.parts[i].Flgs.Contains(BodyPart.BPFlags.Crystal))
                    {
                        newItem.AddProperty(ItemProperty.OnAttach_Crystallization);
                    }
                    else if (corpse.parts[i].Flgs.Contains(BodyPart.BPFlags.Vampire) || corpse.vamp)
                    {
                        newItem.AddProperty(ItemProperty.OnAttach_Vampirism);
                    }

                    if (corpse.cann)
                    {
                        newItem.AddProperty(ItemProperty.Cannibalism);
                    }
                }
            }
            else
            {
                newItem = ItemList.GetItemByID("scrap");
            }

            if (newItem != null)
            {
                PickupItem(newItem);
            }
        }

        CombatLog.SimpleMessage("Butcher_Corpse");
        item.RunCommands("OnButcher", entity);
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
        if (i.HasCComponent<CLiquidContainer>())
        {
            CombatLog.NameMessage("Message_Action_Drink", i.DisplayName());
            i.OnConsume(stats);
        }
        else
        {
            CombatLog.NameMessage("Message_Action_Consume", i.DisplayName());
            i.OnConsume(stats);
            RemoveInstance(i);
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

    public bool HasSpearEquipped()
    {
        var hands = body.Hands;

        for (int i = 0; i < hands.Count; i++)
        {
            if (hands[i].EquippedItem != null && hands[i].EquippedItem.attackType == Item.AttackType.Spear)
            {
                return true;
            }
        }

        return false;
    }

    Inventory CheckForInventories(Coord pos)
    {
        Cell c = World.tileMap.GetCellAt(pos);

        if (c != null)
        {
            for (int i = 0; i < c.mapObjects.Count; i++)
            {
                if (c.mapObjects[i].inv != null)
                {
                    return c.mapObjects[i].inv;
                }
            }
        }

        return null;
    }

    public void Drop(Item i)
    {
        if (entity == null || World.userInterface.CurrentState == UIWindow.Inventory && !entity.isPlayer && i.HasProp(ItemProperty.Unique))
        {
            return;
        }

        Inventory otherInventory = CheckForInventories(entity.myPos);

        if (i.lootable)
        {
            Item newItem = new Item(i) { amount = 1 };

            if (otherInventory != null)
            {
                otherInventory.PickupItem(newItem);
            }
            else
            {
                World.objectManager.NewInventory("Loot", new Coord(entity.myPos), World.tileMap.WorldPosition, World.tileMap.currentElevation, newItem);
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
        {
            return;
        }

        if (i.lootable)
        {
            Inventory otherInventory = CheckForInventories(entity.myPos);
            Item newItem = new Item(i);

            if (otherInventory != null)
            {
                otherInventory.PickupItem(newItem);
            }
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
            BaseAI bai = entity.AI != null ? entity.AI : entity.GetComponent<BaseAI>();
            Item corpseItem = (bai.npcBase.corpseItem == null) ? ItemList.GetItemByID("corpse_norm") : ItemList.GetItemByID(bai.npcBase.corpseItem);

            if (!bai.npcBase.HasFlag(NPC_Flags.Deteriortate_HP) && !bai.npcBase.HasFlag(NPC_Flags.No_Body))
            {
                CCorpse co = new CCorpse(new List<BodyPart>(body.bodyParts), gameObject.name, bai.npcBase.HasFlag(NPC_Flags.Human),
                    bai.npcBase.HasFlag(NPC_Flags.Radiation), bai.npcBase.HasFlag(NPC_Flags.Skills_Leprosy), bai.npcBase.HasFlag(NPC_Flags.Vampire));

                corpseItem.AddComponent(co);
                corpseItem.AddProperty(ItemProperty.Corpse);
                corpseItem.displayName = string.Format("{0}'s Corpse", entity.MyName);

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

        if (!entity.isPlayer)
        {
            if (entity.AI.isFollower())
            {
                for (int i = 0; i < body.bodyParts.Count; i++)
                {
                    if (body.bodyParts[i].equippedItem != null)
                    {
                        PickupItem(body.bodyParts[i].equippedItem);
                    }
                }
            }
            else if (entity.AI.npcBase.HasFlag(NPC_Flags.Human) && items.Count < 2 && RNG.Next(100) < 5)
            {
                items.AddRange(GetDrops(RNG.Next(0, 4)));
            }
        }

        if (items.Count > 0)
        {
            Inventory otherInventory = CheckForInventories(entity.myPos);

            if (otherInventory == null)
            {
                MapObject m = World.objectManager.NewObjectAtSpecificScreen("Loot", entity.myPos, World.tileMap.WorldPosition, World.tileMap.currentElevation);

                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null && items[i].lootable)
                    {     
                        m.inv.Add(items[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < items.Count; i++)
                {
                    otherInventory.PickupItem(items[i], true);
                }
            }
        }

        World.objectManager.CheckMapObjectInventories();
    }

    public int ArmorProfLevelFromBP(BodyPart bp)
    {
        return stats.CheckProficiencies(bp.equippedItem).level;
    }

    /// <summary>
    /// All equipped items, including weapon, off-hand and firearm.
    /// </summary>
    public List<Item> EquippedItems()
    {
        List<Item> eItems = new List<Item>();

        if (entity == null)
        {
            return eItems;
        }

        CheckIntegrity();

        foreach (BodyPart.Hand h in body.Hands)
        {
            eItems.Add(h.EquippedItem);
        }

        eItems.Add(firearm);

        if (body.bodyParts == null)
        {
            return eItems;
        }

        foreach (BodyPart b in body.bodyParts)
        {
            eItems.Add(b.equippedItem);
        }

        return eItems;
    }

    void CheckIntegrity()
    {
        foreach (BodyPart.Hand h in body.Hands)
        {
            if (h.EquippedItem.IsNullOrDefault())
            {
                h.RevertToBase(entity);
            }
        }

        if (firearm == null)
        {
            firearm = ItemList.NoneItem;
        }

        if (body.bodyParts != null)
        {
            foreach (BodyPart b in body.bodyParts)
            {
                if (b.equippedItem == null)
                {
                    b.equippedItem = ItemList.NoneItem;
                }
            }
        }
    }

    public Coord RandomOpenDropLocation()
    {
        List<Coord> possibleDropCoords = new List<Coord>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                if (World.tileMap.WalkableTile(entity.posX + x, entity.posY + y))
                {
                    possibleDropCoords.Add(new Coord(entity.posX + x, entity.posY + y));
                }
            }
        }

        return possibleDropCoords.Any() ? possibleDropCoords.GetRandom() : entity.myPos;
    }

    public bool CanAfford(int cost)
    {
        return gold >= cost;
    }

    public bool OverCapacity()
    {
        return items.Count > maxItems;
    }

    public bool IsNoneItem(Item i)
    {
        return i.ID == ItemList.NoneItem.ID;
    }

    public bool DiggingEquipped()
    {
        return EquippedItems().Any(x => x.HasProp(ItemProperty.Dig));
    }

    void GetFromNPC()
    {
        BaseAI aibase = GetComponent<BaseAI>();

        for (int i = 0; i < aibase.npcBase.inventory.Count; i++)
        {
            PickupItem(aibase.npcBase.inventory[i]);
        }

        firearm = aibase.npcBase.firearm;
        body.InitializeBody();
    }

    public void GetFromBuilder(PlayerBuilder builder)
    {
        items.Clear();
        firearm = builder.firearm;

        if (builder.items != null)
        {
            for (int i = 0; i < Manager.playerBuilder.items.Count; i++)
            {
                items.Add(new Item(Manager.playerBuilder.items[i]));
            }
        }

        body.GetFromBuilder(builder);
        gold = builder.money;
        GameObject.FindObjectOfType<AmmoPanel>().Display(!firearm.IsNullOrDefault());
    }
}