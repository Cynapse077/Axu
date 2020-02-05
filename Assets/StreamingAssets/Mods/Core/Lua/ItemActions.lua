
--Adds a mechanical body part to the player's body.
function AddMechBodyPart(entity, partID)
	local bp = EntityList.GetBodyPart(partID)

	if (bp ~= nil) then
		entity.body.AddBodyPart(bp)
		bp.Attach(entity.stats, true)
		Log("ASDF")
	end
end	
--Removes it.
function RemoveMechBodyPart(entity, partID)
	local bpName = EntityList.GetBodyPart(partID).name

	for i = 1, #entity.body.bodyParts do
		if (entity.body.bodyParts[i].name == bpName) then
			entity.inventory.PickupItem(entity.body.bodyParts[i].equippedItem);
			entity.body.RemoveLimb(entity.body.bodyParts[i]);
			return
		end
	end
end	

--Shocks nearby entities to the target.
function ShockAdjacent(defender, attacker)
	if (Random(0, 100) < 60) then
		return
	end

	local range = 1

	for x = defender.posX - range, defender.posX + range do
		for y = defender.posY - range, defender.posY + range do
			if (Random(0, 100) < 50 and TileMap.WalkableTile(x, y)) then
				local c = TileMap.GetCellAt(x, y)

				if (c ~= nil and c.entity ~= nil and c.entity ~= attacker and c.entity ~= defender) then
					--Determine rotation of object
					local diffX = x - defender.posX
					local diffY = y - defender.posY
					local rot = 0

					if (diffY ~= 0) then
						if (diffX == 0) then 
							rot = 90
						elseif (diffX + diffY == 0) then 
							rot = 135 
						elseif (diffX == diffY) then 
							rot = 45 
						end
					end

					--Spawn the damage effect object
					ObjectManager.SpawnEffect(1, "shock", attacker, x, y, Random(2, 6), nil, rot)
				end
			end
		end 
	end
end

--From ammunition types.
function Explosion(source, pos)
	for x = pos.x - 1, pos.x + 1 do
		for y = pos.y - 1, pos.y + 1 do
			ObjectManager.SpawnEffect(6, "explosion", source, x, y, Random(6, 12), nil, 0)
		end
	end

	SoundManager.Explosion()
end
function OnHit_Poison(source, pos)
	if (Random(0, 100) < 50 and TargetAvailableAt(pos)) then
		TileMap.GetCellAt(position).entity.stats.AddStatusEffect("Poison", Random(2, 8))
	end
end
function OnHit_Burn(source, pos)
	if (Random(0, 100) < 50 and TargetAvailableAt(pos)) then
		TileMap.GetCellAt(position).entity.stats.AddStatusEffect("Aflame", Random(4, 7))
	end
end
function OnHit_Confuse(source, pos)
	if (Random(0, 100) < 50 and TargetAvailableAt(pos)) then
		TileMap.GetCellAt(position).entity.stats.AddStatusEffect("Confuse", Random(6, 10))
	end
end
function OnHit_Daze(source, pos)
	if (Random(0, 100) < 50 and TargetAvailableAt(pos)) then
		TileMap.GetCellAt(position).entity.stats.AddStatusEffect("Stun", Random(2, 6))
	end
end

--Pushes a creature away from the attacker
function PushEntity(defender, attacker)
	if (Random(0, 100) < 50) then
		return
	end

	local diff = Coord.__new(attacker.posX - defender.posX, attacker.posY - defender.posY)
	attacker.ForceMove(diff.x, diff.y, defender.stats.Strength)
	attacker.stats.AddStatusEffect("Stun", Random(1, 3))
	Log(attacker.Name .. " is shoved back by " .. defender.Name)
end

--Heals the attacker by a small amount
function DrainHealth(defender, attacker)
	if (Random(0, 100) >= 10) then
		attacker.stats.Heal(Random(1, 5))
	end
end

--Helper function
function TargetAvailableAt(position)
	return (TileMap.WalkableTile(position.x, position.y) and TileMap.GetCellAt(position).entity ~= nil)
end

--Opens the alert panel with the journal text
function Read_DeepHunterJournal()
	local hunterJournalText = "<i>[The journal has one only one entry. The rest of the pages are blank]</i>.\n\"So, I made the deal. I've stolen something from the Ensis in return for the favor of the Deep Ones. What they forgot to mention was the horrible side-effects... \nAs soon as the sickly black tendrils came into contact with my skin, my flesh started to burn away. My mouth closed itself, yet I feel an insatiable hunger. Whatever power I've been given is not worth the torment.\nDamn those Deep Ones. I'm taking this artifact where nobody can find it again. Then, if I have the guts for it, I can end my miserable life. I built this house with my bare hands. It's going to be tough to say goodbye...\""

	Alert.CustomAlert_WithTitle("A Hunter's Journal", hunterJournalText)
end