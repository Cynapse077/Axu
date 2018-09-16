function Interact_Cryopod(obj)
	obj.SetTypeAndSwapSprite(ObjectTypes.Cryopod_Open)
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

		SpawnController.SpawnEnemyByID("subject", TileMap.WorldPosition, TileMap.currentElevation, pos)
		Log("<color=red>A creature was inside the pod! It looks hostile!</color>")
		TileMap.SoftRebuild()
	else
		Log("<color=grey>You open the cryopod. Nothing happens.</color>")
	end
end

function Interact_Terminal(obj)
	if (Journal.OnActivateTerminal_CheckQuestProgress()) then
		obj.SetTypeAndSwapSprite("Terminal_On")
		Log("<color=green>The terminal switches on, and begins its bootup sequence.</color>")
	else
		Log("<color=grey>You fiddle with the terminal, but nothing happens.</color>")
	end
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