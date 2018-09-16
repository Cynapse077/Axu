--Adds a mechanical body part to the player's body.
function AddMechBodyPart(entity, partID)
	local bp = EntityList.GetBodyPart(partID)
	entity.body.AddBodyPart(bp)
	bp.Attach(entity.stats, true)
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
	if (Random(0, 100) > 20) then
		return
	end

	local range = 1

	for x = defender.posX - range, defender.posX + range do
		for y = defender.posY - range, defender.posY + range do
			if (Random(0, 100) < 50 and TileMap.WalkableTile(x, y)) then
				local c = TileMap.GetCellAt(x, y)

				if (c ~= null and c.entity ~= null and c.entity ~= attacker and c.entity ~= defender) then
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

					ObjectManager.SpawnEffect(1, "shock", attacker, x, y, Random(2, 6), nil, rot)
				end
			end
		end 
	end
end

function PushEntity(defender, attacker)
	if (Random(0, 100) < 50) then
		return
	end

	local diff = Coord.__new(attacker.posX - defender.posX, attacker.posY - defender.posY)
	attacker.ForceMove(diff.x, diff.y, defender.stats.Strength)
	attacker.stats.AddStatusEffect("Stun", Random(1, 4))
	Log(attacker.Name .. " is shoved back by " .. defender.Name)
end

function DrainHealth(defender, attacker)
	if (Random(0, 100) >= 10) then
		attacker.stats.Heal(Random(1, 5))
	end
end