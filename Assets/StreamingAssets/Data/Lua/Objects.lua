--OnInteract functions for Objects.

function Interact_Door(obj)
	if (obj.cell.entity == nil) then
		if (obj.objectType == "Door_Open") then
			obj.SetTypeAndSwapSprite("Door_Closed")

		elseif (obj.objectType == "Ensis_Door_Open") then
			obj.SetTypeAndSwapSprite("Ensis_Door_Closed")

		elseif (obj.objectType == "Prison_Door_Open") then
			obj.SetTypeAndSwapSprite("Prison_Door_Closed")

		elseif (obj.objectType == "Magna_Door_Open") then
			obj.SetTypeAndSwapSprite("Magna_Door_Closed")

		elseif (obj.objectType == "Kin_Door_Open") then
			obj.SetTypeAndSwapSprite("Kin_Door_Closed")
		else
			return
		end

		TileMap.SoftRebuild()
		SoundManager.CloseDoor()
	end
end

function Interact_Cryopod(obj)
	obj.SetTypeAndSwapSprite("Cryopod_Open")
	local ranNum = Random(0, 100)

	if (ranNum < 25) then
		PlayerEntity.stats.Radiate(Random(10, 50))
		Log("Radiation spills out from the pod!")
	elseif (ranNum < 60) then
		local pos = TileMap.EmptyAdjacent(obj.localPos)

		if (pos == nil) then 
			Log("<color=grey>You open the cryopod. Nothing happens.</color>")
			return 
		end

		SpawnController.SpawnNPCByID("subject", TileMap.WorldPosition, TileMap.currentElevation, pos)
		Log("<color=red>A creature was inside the pod! It looks hostile!</color>")
		TileMap.SoftRebuild()
	else
		Log("<color=grey>You open the cryopod. Nothing happens.</color>")
	end
end

function Interact_Terminal(obj)
	Log("<color=grey>You fiddle with the terminal.</color>")
end

function Interact_Stairlock(obj)
	Log("You cannot figure out a way to break the chains surrounding this block. Perhaps there is another way past this barrier...")
end

function Interact_Grave(obj)
	if (PlayerEntity.body.MainHand.equippedItem.HasProp(ItemProperty.Dig)) then
		obj.SetTypeAndSwapSprite("Grave_Dug")
		Log("You excavate the grave.")
	end
end

function Interact_Light(obj)
	if (obj.objectType == "Light_Off") then
		obj.SetTypeAndSwapSprite("Light_On")
	elseif (obj.objectType == "Light_On") then
		obj.SetTypeAndSwapSprite("Light_Off")
	end
end

function Interact_Switch(obj)
	if (obj.objectType == "Switch_Off") then
		obj.SetTypeAndSwapSprite("Switch_On")
		obj.StartPulse(true)
		Log("You set the switch to the \"on\" position.")
	elseif (obj.objectType == "Switch_On") then
		obj.SetTypeAndSwapSprite("Switch_Off")
		obj.StartPulse(false)
		Log("You set the switch to the \"off\" position.")
	end
end

function OnEnter_Web(entity, obj)
	if (entity.inventory.CanFly()) then
		return
	end

	if (entity.isPlayer) then 
		if (entity.stats.hasTraitEffect(TraitEffects.Resist_Webs)) then 
			return
		end
	elseif (entity.AI.npcBase.HasFlag(NPC_Flags.Resist_Webs)) then 
		return 
	end

	if (obj.InSight()) then
		Log(entity.Name .. "is caught in the web!")
	end
	
	entity.stats.AddStatusEffect("Stuck", Random(1, 7))
end

function OnExit_Web(entity, obj)
	--obj.DestroyMe()
end

function OnEnter_SpikeTrap(entity, obj)
	if (entity.inventory.CanFly()) then
		return
	end

	if (obj.InSight()) then
		Log("Spikes protrude from the ground beneath " .. entity.Name .. "!")
	end
	
	entity.stats.SimpleDamage(Random(3, 7))
end

function OnEnter_PoisonTrap(entity, obj)
	if (entity.inventory.CanFly()) then
		return
	end

	if (obj.InSight()) then
		Log("The floor beneath " .. entity.Name .. " spews poisonous gas!")
	end

	entity.stats.AddStatusEffect("Poison", Random(2, 6))
end

function OnEnter_PressurePlate(entity, obj)
	obj.StartPulse(true)
end

function OnExit_PressurePlate(entity, obj)
	obj.StartPulse(false)
end