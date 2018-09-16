using UnityEngine;
using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Inventory : MonoBehaviour {
	[HideInInspector]
    public int gold = 0;
	public List<Item> items = new List<Item>();
	public string baseWeapon = "fists";
	public Entity entity;

	Item _mainHand, _offHand, _firearm;
	int _maxItems = 15;

	Stats stats {
		get { return entity.stats; } 
	}
	Body body {
		get { return entity.body; }
	}

	public int maxItems {
		get {
			if (entity != null && !entity.isPlayer)
				return 25;

			return (entity != null) ? _maxItems + stats.Strength - 1 : _maxItems; 
		}
	}

	public Item firearm {
		get {
			return _firearm; 
		}
		set {
			if (_firearm != null && entity != null)
				_firearm.OnUnequip(entity);
			
			_firearm = value;
			if (entity != null && entity.isPlayer)
				GameObject.FindObjectOfType<AmmoPanel>().Display(_firearm.ID != "none");

			if (stats != null && _firearm != null)
				_firearm.OnEquip(stats);
		}
	}

	public void Init() {
		if (entity.isPlayer)
			GetFromBuilder(Manager.playerBuilder);
		else
			GetFromNPC();
	}

	public void SetStorage(int amount) {
		_maxItems = amount;
	}

	public void AddRemoveStorage(int amount) {
		_maxItems += amount;
		ManageCapacity();
	}

	public static List<Item> GetDrops(int numItems) {
		List<Item> inv = new List<Item>();

		for (int i = 0; i < numItems; i++) {
			int rarity = 1;

			for (int j = 0; j < ItemList.MaxRarity; j++) {
				if (SeedManager.combatRandom.Next(1000) < j * World.DangerLevel())
					rarity ++;
			}

			Item item = ItemList.GetItemByRarity(rarity);

			if (rarity >= ItemList.MaxRarity && SeedManager.combatRandom.Next(150) < 10 || SeedManager.combatRandom.Next(500) == 1)
				item = ItemList.GetRandart(item);
			
			inv.Add(item);
		}

		return inv;
	}

	//This is called when capacity changes. If you have more items than you can carry, some will drop on the ground.
	public void ManageCapacity() {
		if (items.Count <= maxItems)
			return;
		
		int numToDrop = items.Count - maxItems;

		for (int i = 0; i < numToDrop; i++) {
			Drop(items[i]);
		}

		Alert.NewAlert("Inv_Full", UIWindow.Inventory);
	}

    public bool CanFly() {
        if (stats == null || stats.statusEffects == null || stats.HasEffect("Topple") || stats.HasEffect("Unconscious"))
            return false;
        if (!entity.isPlayer && entity.AI.npcBase.HasFlag(NPC_Flags.Flying))
            return true;
        if (stats.HasEffect("Float"))
            return true;
        if (EquippedItems() != null && EquippedItems().Find(x => x.HasProp(ItemProperty.Flying)) != null)
			return true;
		
		return body.GetBodyPartsBySlot(ItemProperty.Slot_Wing).FindAll(x => x.isAttached).Count > 0;
    }

	public void ThrowItem(Coord destination, Item i, Explosive exp) {
        if (i.HasProp(ItemProperty.Explosive)) {
			exp.DetonateExplosion(i.damageTypes, entity);
            RemoveInstance(i);
            return;
        }

		exp.DetonateOneTile(entity);

		if (i.HasComponent<CLiquidContainer>() && World.tileMap.GetCellAt(destination) != null && World.tileMap.GetCellAt(destination).entity != null) {
			i.GetItemComponent<CLiquidContainer>().Pour(World.tileMap.GetCellAt(destination).entity);

			if (!i.HasProp(ItemProperty.Quest_Item) && !i.HasProp(ItemProperty.Artifact)) {
				RemoveInstance(i);
				Destroy(exp.gameObject);
				CombatLog.NameMessage("Item_Impact_Shatter", i.Name);
				return;
			}
		}

		if (SeedManager.combatRandom.Next(100) < 12 + stats.proficiencies.Throwing.level) {
			if (!i.HasProp(ItemProperty.Quest_Item) && !i.HasProp(ItemProperty.Artifact)) {
				RemoveInstance(i);
				Destroy(exp.gameObject);
				CombatLog.NameMessage("Item_Impact_Shatter", i.Name);
				return;
			}
		}

		//If it didn't break or explode
		if (i.lootable) {
			Item newItem = new Item(i);
			newItem.amount = 1;
			Inventory otherInventory = CheckForInventories(destination);

			if (otherInventory == null) {
				MapObject m = World.objectManager.NewObjectAtOtherScreen("Loot", destination, World.tileMap.WorldPosition, World.tileMap.currentElevation);
				m.inv.Add(newItem);
			} else
				otherInventory.PickupItem(newItem);
		}

		RemoveInstance(i);
    }

	public void Ready(Item i) {
		EquipFirearm(i);
	}

	public void Equip(Item i) {
		if (i.HasProp(ItemProperty.Ranged)) {
			EquipFirearm(i);
			return;
		}
			
		List<BodyPart> parts = body.bodyParts.FindAll(x => x.slot == i.GetSlot());

		if (parts.Count == 1)
			EquipDirectlyToBodyPart(i, body.bodyParts.Find(x => x.slot == i.GetSlot()));
	}

    public void EquipDirectlyToBodyPart(Item i, BodyPart b) {
		if (!b.canWearGear || !b.isAttached || b.equippedItem.HasProp(ItemProperty.Cannot_Remove))
            return;
		
        if (b.equippedItem != null && !isNoneItem(b.equippedItem) && b.equippedItem.lootable)
            UnEquipArmor(b, true);

        b.equippedItem = i;
		i.OnEquip(stats);
        RemoveInstance(i);
		World.soundManager.UseItem();
    }

	public void EquipFirearm(Item i, bool cancelMessage = false) {
		if (firearm.HasProp(ItemProperty.Cannot_Remove))
			return;

        Item itemToPickup = new Item(firearm);
		firearm = new Item(i);
        i.OnEquip(stats);
        RemoveInstance_All(i);
		PickupItem(itemToPickup, false);

		foreach (Stat_Modifier sm in i.statMods) {
			if (sm.Stat == "Light" && entity.isPlayer)
				World.tileMap.LightCheck(entity);
		}

		if (World.soundManager != null)
			World.soundManager.UseItem();
	}

	public void UnEquipFirearm(bool cancelMessage = false) {
		if (firearm == null)
			return;

		if (!isNoneItem(firearm)) {
			if (firearm.HasProp(ItemProperty.Cannot_Remove)) {
				if (!cancelMessage)
					Alert.NewAlert("Cannot_Remove", UIWindow.Inventory);
				return;
			}

			PickupItem(firearm, false, true);
			firearm = ItemList.GetNone();
		}
	}

	public void Attach(Item i) {
		CombatLog.NameMessage("Attach_Limb", i.DisplayName());

        i.RunCommands("OnAttach");
        i.OnEquip(stats, false);
		RemoveInstance(i);
	}

	public bool TwoHandPenalty(BodyPart.Hand hand) {
		if (hand == null  || body == null || hand.arm == null)
			return false;
		
		return (body.FreeHands().Count == 0 && hand.arm.GetStatMod("Strength").Amount < 5);
	}

	public void Wield(Item i, int armSlot) {
		List<BodyPart.Hand> hands = body.Hands;
		BodyPart.Hand hand = hands[armSlot];

		if (hands.Count == 0 || hand == null) {
			Alert.NewAlert("No_Hands", UIWindow.Inventory);
			return;
		}
		if (hand.equippedItem.HasProp(ItemProperty.Cannot_Remove) && hand.equippedItem.ID != baseWeapon)
			return;

		if (hand.equippedItem != null && hand.equippedItem.lootable)
			PickupItem(hand.equippedItem, true);

		hand.SetEquippedItem(new Item(i), entity);

		//Check for two handed weapons with low strength arms
		for (int h = 0; h < hands.Count; h++) {
			if (hands[h].equippedItem.HasProp(ItemProperty.Two_Handed) && TwoHandPenalty(hands[h])) {
				CombatLog.NameMessage("Message_2_Handed_No_Req", hands[h].equippedItem.DisplayName());
				break;
			}
		}

        i.RunCommands("OnEquip");
        i.amount = 1;
		RemoveInstance(i);

		foreach (Stat_Modifier sm in i.statMods) {
			if (sm.Stat == "Light" && entity.isPlayer)
				World.tileMap.LightCheck(entity);
		}

		if (World.soundManager != null)
			World.soundManager.UseItem();
    }

	public void UnEquipWeapon(Item i, int armSlot) {
		List<BodyPart.Hand> hands = body.Hands;
		BodyPart.Hand hand = hands[armSlot];

		if (!hand.equippedItem.HasProp(ItemProperty.Cannot_Remove)) {
			PickupItem(hand.equippedItem);
			hand.SetEquippedItem(ItemList.GetItemByID(baseWeapon), entity);
		}
	}

	public void UnEquipArmor(BodyPart b, bool overrideMax = false) {
        if (b.equippedItem.lootable && !b.equippedItem.HasProp(ItemProperty.Cannot_Remove)) {
			b.equippedItem.OnUnequip(entity, false);
			PickupItem(b.equippedItem, overrideMax);
            b.equippedItem = ItemList.GetNone();
            entity.Wait();
        }
    }

	//This is only used by Return Pads to set the destination. 
	public void Set(Item i) {
		i.ApplyEffects(stats);
	}

    public void Use(Item i) {
		if (i.GetItemComponent<CCharges>() != null && !i.UseCharge()) {
			if (i.HasProp(ItemProperty.DestroyOnZeroCharges) && i.GetItemComponent<CCharges>().current <= 0)
				RemoveInstance(i);
			else
				Alert.NewAlert("No_Charges", UIWindow.Inventory);
			return;
		}

		if (i.GetItemComponent<CModKit>() != null) {
			World.userInterface.ItemOnItem_Mod(i, this, i.GetItemComponent<CModKit>());
			return;
		}

        i.RunCommands("OnUse");

        if (i.HasProp(ItemProperty.Selected_Tele)) {
			i.ApplyEffects(stats);
			return;
		}

		if (i.HasProp(ItemProperty.ReplaceLimb))
			i.ApplyEffects(stats);
        if (i.HasProp(ItemProperty.Stop_Bleeding)) {
			stats.RemoveStatusEffect("Bleed");
            RemoveInstance(i);
        }

		if (i.HasProp(ItemProperty.Blink))
			GetComponent<PlayerInput>().UseSelectTileSkill(SkillList.GetSkillByID("blink"), false);
		if (i.HasProp(ItemProperty.Teleport))
			GetComponent<EntitySkills>().Teleport();
		if (i.HasProp(ItemProperty.Surface_Tele) && entity.TeleportToSurface())
            RemoveInstance(i);
		
        entity.Wait();
    }

    public void Read(Item i) {
		int timeCost = 100 - (stats.Intelligence * 2);
		timeCost = Mathf.Clamp(timeCost, 1, 100);

		if (i.HasProp(ItemProperty.Tome)) {
			if (i.GetItemComponent<CAbility>() != null) {
				string abName = i.GetItemComponent<CAbility>().abID;

				EntitySkills eSkills = GetComponent<EntitySkills>();
				Skill skill = new Skill(SkillList.GetSkillByID(abName));

				if (eSkills.abilities.Find(x => x.ID == skill.ID) == null) {
					eSkills.AddSkill(new Skill(SkillList.GetSkillByID(abName)));
					CombatLog.NameMessage("Learn_Skill", skill.Name);
				} else {
					skill = eSkills.abilities.Find(x => x.ID == abName);

					if (skill.level < Skill.maxLvl && skill.CanLevelUp) {
						skill.level ++;
						skill.XP = 0;
						CombatLog.NameMessage("Increase_Skill", skill.Name);
					} else {
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

	public List<Item> Items_ThrowingFirst() {
		List<Item> tItems = new List<Item>();
		List<Item> ntItems = new List<Item>();

		for (int i = 0; i < items.Count; i++) {
			if (items[i].HasProp(ItemProperty.Throwing_Wep) || items[i].GetItemComponent<CLiquidContainer>() != null)
				tItems.Add(items[i]);
			else 
				ntItems.Add(items[i]);
		}

		tItems.AddRange(ntItems);

		return tItems;
	}

    public bool CanPickupItem(Item i) {
		if (!i.lootable || i.HasProp(ItemProperty.Pool))
            return false;
		
		if (i.stackable && items.Find(x => x.ItemName() == i.ItemName() && x.components == i.components) != null)
            return true;

		return !atMaxCapacity();
    }

	public void PickupItem(Item i, bool canExceedMaxCapacity = false, bool fromFirearm = false) {
		if (i == null || !i.lootable)
            return;

        if (i.stackable) {
			if (firearm != null && i.ID == firearm.ID && !fromFirearm) {
				firearm.amount += i.amount;
				World.objectManager.UpdateDialogueOptions();
				return;
			}
            for (int x = 0; x < items.Count; x++) {
				if (items[x].DisplayName() == i.DisplayName() && items[x].ID == i.ID) {
					if (i.GetItemComponent<CCharges>() != null) {
						if (i.GetItemComponent<CCharges>().current == items[x].GetItemComponent<CCharges>().current) {
							items[x].amount += i.amount;
							World.objectManager.UpdateDialogueOptions();
							return;
						}
					} else if (i.GetItemComponent<CRot>() != null) {
						if (i.GetItemComponent<CRot>().current == items[x].GetItemComponent<CRot>().current) {
							items[x].amount += i.amount;
							World.objectManager.UpdateDialogueOptions();
							return;
						}
					} else {
						items[x].amount += i.amount;
						World.objectManager.UpdateDialogueOptions();
						return;
					}
				
                }
            }
        }

        if (atMaxCapacity() && !canExceedMaxCapacity) {
            if (entity != null && entity.isPlayer) {
				Alert.NewAlert("Inv_Full", UIWindow.Inventory);
                Drop(i);
            }
            return;
        }

		World.objectManager.UpdateDialogueOptions();
        items.Add(i);
    }

    public void RemoveInstance(Item i) {
		if (i == firearm) {
			if (i.stackable && i.amount > 1)
				i.amount--;
			else {
				firearm.OnUnequip(entity, false);
				firearm = ItemList.GetNone();
			}
		} else if (items.Contains(i)) {
			if (i.stackable && i.amount > 1) {
				i.amount--;
				return;
			}

			items.Remove(i);

			if (entity == null) {
				MapObjectSprite mos = GetComponent<MapObjectSprite>();
				if (mos != null)
					mos.UpdateVisuals();
			}
		}
    }

	public void RemoveInstance_All(Item i) {
		if (items.Contains(i))
			items.Remove(i);
	}

    public void Disarm() {
		if (body.MainHand.equippedItem.lootable) {
			Item i = body.MainHand.equippedItem;
			PickupItem(i, false);
			body.MainHand.SetEquippedItem(ItemList.GetItemByID(baseWeapon), entity);
            Drop(i);
        }
    }

	public bool Reload(Item i) {
		if (!i.HasComponent<CFirearm>())
			return false;
		
		Item ammo = items.Find(x => x.HasProp(ItemProperty.Ammunition) && x.ID == i.GetItemComponent<CFirearm>().ammoID);

		if (ammo == null)
			return false;
		
		int ammoAmount = ammo.amount;
		ammo.amount -= i.Reload(ammoAmount);

		if (ammo.amount <= 0)
			RemoveInstance(ammo);

		if (!i.ContainsProperty(ItemProperty.Bow))
			World.soundManager.Reload();
		
		return true;
	}

	public bool Unload(Item it) {
		if (it.Charges() == 0)
			return false;

		Item bullet = ItemList.GetItemByID(it.GetItemComponent<CFirearm>().ammoID);
		bullet.amount = it.Charges();
		it.Unload();

		PickupItem(bullet);
		return true;
	}

	public void Butcher(Item item) {
		if (!item.HasComponent<CCorpse>())
			return;

		CCorpse corpse = item.GetItemComponent<CCorpse>();

		for (int i = 0; i < corpse.parts.Count; i++) {
			if (!corpse.parts[i].Att)
				continue;

			int randomNumer = SeedManager.combatRandom.Next(100);
			int butcheryLevel = stats.proficiencies.Butchery.level;

			BodyPart sample = new BodyPart(corpse.owner + "'s " + corpse.parts[i].Name, true, corpse.parts[i].Slot);
			Item it = new Item(ItemList.GetSeveredBodyPart(sample));

			if (it.ID != "fleshraw")
				it.displayName = sample.name;
			
			CEquipped ce = new CEquipped(corpse.parts[i].item.ID);
			it.AddComponent(ce);

			if (item.HasComponent<CRot>() && it.HasComponent<CRot>())
				it.GetItemComponent<CRot>().current = item.GetItemComponent<CRot>().current;

			if (corpse.parts[i].Org) {
				if (randomNumer > (butcheryLevel + 1) * 10) {
                    it = (randomNumer > 60) ? ItemList.GetItemByID("fleshraw") : null;
				} else {
					foreach (Stat_Modifier sm in it.statMods) {
						if (sm.Stat != "Hunger") {
							if (SeedManager.combatRandom.CoinFlip())
								sm.Amount += SeedManager.combatRandom.Next(0, 2);
							else if (SeedManager.combatRandom.Next(12) < butcheryLevel)
								sm.Amount += SeedManager.combatRandom.Next(1, 5);
						}
					}
				}

				if (it != null) {
					if (corpse.parts[i].Dis == TraitEffects.Leprosy || corpse.lep)
						it.AddProperty(ItemProperty.OnAttach_Leprosy);
					else if (corpse.parts[i].Dis == TraitEffects.Crystallization)
						it.AddProperty(ItemProperty.OnAttach_Crystallization);

					if (corpse.cann)
						it.AddProperty(ItemProperty.Cannibalism);
				}
			} else
				it = ItemList.GetItemByID("scrap");

			if (it != null)
				DropBodyPart(it);
		}

        item.RunCommands("OnButcher");
        CombatLog.SimpleMessage("Butcher_Corpse");
		stats.AddProficiencyXP(stats.proficiencies.Butchery, SeedManager.localRandom.Next(3, 6));
		entity.CreateBloodstain(true, 100);
		RemoveInstance(item);
	}

	public void Eat(Item i) {
        if (stats.hasTraitEffect(TraitEffects.Vampirism)) {
            Alert.NewAlert("Cannot_Eat", UIWindow.Inventory);
            return;
        }

		if (stats.Hunger < Globals.Satiated) {
			Consume(i);
			World.soundManager.Eat();
		} else
			Alert.NewAlert("Too_Full", UIWindow.Inventory);
	}

	public void Drink(Item i) {
		Consume(i);
		World.soundManager.Drink();
	}

	void Consume(Item i) {
        i.OnConsume(stats);

        if (i.HasComponent<CLiquidContainer>())
			CombatLog.NameMessage("Message_Action_Drink", i.DisplayName());
		else {
            RemoveInstance(i);
            CombatLog.Action("Message_Action_Drink", LocalizationManager.GetContent("Action_Eat"), i.DisplayName());
        }
		
        entity.Wait();
    }

	public void Mix(Item i) {
		World.userInterface.ItemOnItem_Fill(i, this);
	}

	public void Pour(Item i) {
		if (i.HasComponent<CLiquidContainer>())
			World.userInterface.PourActions(i);
	}

    Inventory CheckForInventories(Coord pos) {
		Cell c = World.tileMap.GetCellAt(pos);

		if (c != null) {
			for (int i = 0; i < c.mapObjects.Count; i++) {
				if (c.mapObjects[i].inv != null)
					return c.mapObjects[i].inv;
			}
		}

        return null;
    }

    public void Drop(Item i) {
		if (entity == null || World.userInterface.CurrentState() == UIWindow.Inventory && !entity.isPlayer && i.HasProp(ItemProperty.Unique))
			return;
		
        Inventory otherInventory = CheckForInventories(entity.myPos);

		if (i.lootable) {
			Item newItem = new Item(i);
			newItem.amount = 1;

			if (otherInventory != null)
				otherInventory.PickupItem(newItem);
			else {
				MapObject m = World.objectManager.NewObjectAtOtherScreen("Loot", entity.myPos, World.tileMap.WorldPosition, World.tileMap.currentElevation);
				m.inv.Add(newItem);
			}
		}

		RemoveInstance(i);
    }

    public void DropBodyPart(Item i) {
        if (!i.lootable) {
            items.Remove(i);
            return;
        }

        Coord dropPos = RandomOpenDropLocation();
        Inventory otherInventory = CheckForInventories(dropPos);

        if (otherInventory == null) {
			MapObject m = new MapObject("Loot", dropPos, World.tileMap.WorldPosition, World.tileMap.currentElevation, "");
			m.inv.Add(i);
			World.objectManager.SpawnObject(m);
        } else
            otherInventory.PickupItem(i);

		if (items.Contains(i))
        	items.Remove(i);
    }

	public void DropAllOfType(Item i) {
		if (entity == null)
			return;

		if (i.lootable) {
			Inventory otherInventory = CheckForInventories(entity.myPos);
			Item newItem = new Item(i);

			if (otherInventory != null)
				otherInventory.PickupItem(newItem);
			else {
				MapObject m = World.objectManager.NewObjectAtOtherScreen("Loot", entity.myPos, World.tileMap.WorldPosition, World.tileMap.currentElevation);
				m.inv.Add(newItem);
			}
		}

		RemoveInstance_All(i);
	}

    public void DropAll() {
		if (entity.isPlayer || entity.AI.npcBase.HasFlag(NPC_Flags.Deteriortate_HP))
			return;
		
		//drop corpse
		if (!entity.isPlayer && SeedManager.combatRandom.Next(100) < 20) {
			BaseAI bai = (entity.AI == null) ? GetComponent<BaseAI>() : entity.AI;
			Item corpseItem;

			if (bai.npcBase.corpseItem != null)
				corpseItem = ItemList.GetItemByID(bai.npcBase.corpseItem);
			else
				corpseItem = ItemList.GetItemByID("corpse_norm");

			if (!bai.npcBase.HasFlag(NPC_Flags.Deteriortate_HP) && !bai.npcBase.HasFlag(NPC_Flags.No_Body)) {
				CCorpse co = new CCorpse(new List<BodyPart>(body.bodyParts), gameObject.name, bai.npcBase.HasFlag(NPC_Flags.Human), 
					bai.npcBase.HasFlag(NPC_Flags.Radiation), bai.npcBase.HasFlag(NPC_Flags.Skills_Leprosy));

				corpseItem.AddComponent(co);
				corpseItem.AddProperty(ItemProperty.Corpse);
				corpseItem.displayName = gameObject.name + "'s Corpse";
				
				if (bai.npcBase.HasFlag(NPC_Flags.Human) && !corpseItem.HasProp(ItemProperty.Cannibalism))
					corpseItem.AddProperty(ItemProperty.Cannibalism);

				PickupItem(corpseItem, false);
			}
        }

		//Drop equipped weapons
		List<BodyPart.Hand> hands = body.Hands;

		for (int i = 0; i < hands.Count; i++) {
			PickupItem(hands[i].equippedItem, true);
		}

		PickupItem(firearm, true);

		if (items.Count < 2 && SeedManager.combatRandom.Next(100) < 5)
			items = GetDrops(SeedManager.combatRandom.Next(0, 4));

        if (items.Count > 0) {
            Inventory otherInventory = CheckForInventories(entity.myPos);

            if (otherInventory == null) {
				MapObject m = World.objectManager.NewObjectAtOtherScreen("Loot", entity.myPos, World.tileMap.WorldPosition, World.tileMap.currentElevation);

                for (int i = 0; i < items.Count; i++) {
                    if (items[i] != null && items[i].lootable)
                        m.inv.Add(items[i]);
                }
            } else {
                for (int i = 0; i < items.Count; i++) {
                    if (items[i] != null && items[i].lootable)
                    	otherInventory.PickupItem(items[i], true);
                }
            }
        }

        World.objectManager.CheckMapObjectInventories();
    }

	public int ArmorProfLevelFromBP(BodyPart bp) {
		return (stats.CheckProficiencies(bp.equippedItem).level);
	}

	/// <summary>
	/// All equipped items, including weapon, off-hand and firearm.
	/// </summary>
    public List<Item> EquippedItems() {
		List<Item> eItems = new List<Item>();

		if (entity == null)
			return eItems;
		
		List<BodyPart.Hand> hands = body.Hands;

		for (int i = 0; i < hands.Count; i++) {
			if (hands[i].equippedItem == null)
				hands[i].SetEquippedItem(ItemList.GetItemByID(baseWeapon), entity);

			eItems.Add(hands[i].equippedItem);
		}
			
		if (firearm == null)
			firearm = ItemList.GetNone();
		
		eItems.Add(firearm);

		if (body.bodyParts == null)
			return eItems;

        for (int i = 0; i < body.bodyParts.Count; i++) {
            eItems.Add(body.bodyParts[i].equippedItem);
        }

        return eItems;
    }

    public Coord RandomOpenDropLocation() {
        List<Coord> possibleDropCoords = new List<Coord>();

        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == 0 && y == 0)
                    continue;
				if (World.tileMap.WalkableTile(entity.posX + x, entity.posY + y))
                    possibleDropCoords.Add(new Coord(entity.posX + x, entity.posY + y));
            }
        }

		return (possibleDropCoords.Count > 0) ? possibleDropCoords.GetRandom() : entity.myPos;
    }

    public bool canAfford(int cost) {
		return (gold >= cost);
    }

	public bool overCapacity() {
		return (items.Count > maxItems);
	}

    public bool atMaxCapacity() {
        return (items.Count >= maxItems);
    }

    public bool isNoneItem(Item i) {
		return (i.ID == ItemList.noneItem.ID);
    }

	public bool DiggingEquipped() {
		return (EquippedItems().Find(x => x.HasProp(ItemProperty.Dig)) != null);
	}

	void GetFromNPC() {
		BaseAI aibase = GetComponent<BaseAI>();

		for (int i = 0; i < aibase.npcBase.inventory.Count; i++) {
			PickupItem(aibase.npcBase.inventory[i]);
		}

		body.bodyParts = new List<BodyPart>();
		for (int i = 0; i < aibase.npcBase.bodyParts.Count; i++) {
			body.bodyParts.Add(new BodyPart(aibase.npcBase.bodyParts[i]));
		}

		Item handItem = (aibase.npcBase.handItems.Count > 0 && aibase.npcBase.handItems[0] != null) ? aibase.npcBase.handItems[0] : ItemList.GetItemByName("fists");
		
		body.defaultHand = new BodyPart.Hand(body.GetBodyPartBySlot(ItemProperty.Slot_Head), handItem);

		for (int i = 0; i < aibase.npcBase.handItems.Count; i++) {
			if (body.Hands.Count > i && body.Hands[i] != null && body.Hands[i].isAttached) {
				body.Hands[i].SetEquippedItem(aibase.npcBase.handItems[i], entity);
			}
		}

		firearm = aibase.npcBase.firearm;

		if (body.bodyParts == null || body.bodyParts.Count <= 0)
			body.bodyParts = EntityList.DefaultBodyStructure();

		body.InitializeBody();
	}

	public void GetFromBuilder(PlayerBuilder builder) {
		if (builder == null)
			Debug.LogError("No Builder.");
		
        items.Clear();
		baseWeapon = builder.baseWeapon;
		firearm = builder.firearm;

		if (builder.items != null && builder.items.Count > 0) {
			for (int i = 0; i < Manager.playerBuilder.items.Count; i++) {
				items.Add(new Item(Manager.playerBuilder.items[i]));
            }
        }

		if (builder.bodyParts != null && builder.bodyParts.Count > 0) {
            body.bodyParts = new List<BodyPart>();

			for (int i = 0; i < builder.bodyParts.Count; i++) {
				body.bodyParts.Add(new BodyPart(builder.bodyParts[i]));
            }
        }

		body.GetFromBuilder(builder);
		body.defaultHand = new BodyPart.Hand(body.GetBodyPartBySlot(ItemProperty.Slot_Head), ItemList.GetItemByID("stump"));

		gold = builder.money;

		GameObject.FindObjectOfType<AmmoPanel>().Display(firearm != null && firearm.ID != "none");
    }
}
