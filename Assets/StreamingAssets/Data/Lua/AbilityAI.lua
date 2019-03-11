function Empty(skill, entity, target)
	return false
end

function Healing(skill, entity, target)
	if (target.stats.health <= target.stats.maxHealth / 2) then
		return true
	end

	return false
end


function Restoration(skill, entity, target)
	if (target.stats.stamina <= target.stats.maxStamina / 2) then
		return true
	end

	return false
end

function Sprint(skill, entity, target)
	if (not target.stats.hasEffect("Haste")) then
		return false
	end

	return true
end

function Beam(skill, entity, target)
	if (target.posX == entity.posX or target.posY == entity.posY) then
		return true
	end
			
	return false
end

function Charge(skill, entity, target)
	if (entity.myPos.DistanceTo(target.myPos) > skill.range) then
		return false
	end

	if (target.posX == entity.posX or target.posY == entity.posY) then
		return true
	end
			
	return false
end

function Line(skill, entity, target)
	local line = BLine.GetPoints(entity.myPos, target.myPos)

	for key,pos in pairs(line) do
		if (FriendlyAt(entity, pos.x, pos.y) or not TileMap.WalkableTile(pos.x, pos.y)) then
			return false
		elseif (pos.x == target.posX and pos.y == target.posY) then
			return true
		end
	end

	return false
end

function Cone(skill, entity, target)
	local length = 3 + skill.level

	if (length > 7) then
		length = 7
	end

	local spawns = PositionsInCone(caster, caster.myPos, direction, length)

	for key,pos in pairs(spawns) do
		if (FriendlyAt(entity, pos.x, pos.y)) then
			return false
		elseif (pos.x == target.posX and pos.y == target.posY) then
			return true
		end
	end

	return false
end

function FriendlyAt(entity, x, y)
	if (TileMap.WalkableTile(x, y)) then
		local cell = TileMap.GetCellAt(x, y)

		if (cell == nil or cell.entity == nil) then
			return false
		end

		if (cell.entity == entity) then
			return true
		end

		if (entity.AI.isFollower()) then
			return (cell.entity.isPlayer or cell.entity.AI.isFollower())
		end

		if (not cell.entity.isPlayer) then
			return cell.entity.AI.npcBase.faction.ID == entity.AI.npcBase.faction.ID
		end
	end

	return false
end

function AdjacentToTarget(skill, entity, target)
	return (entity.myPos.DistanceTo(target.myPos) < 2)
end

function AOE(skill, entity, target)
	local hitsTarget = false

	for x = -1, 1 do
		for y = -1, 1 do
			if (FriendlyAt(entity, target.posX + x, target.posY + y)) then
				return false
			elseif (x == 0 and y == 0) then
				hitsTarget = true
			end
		end
    end

	return hitsTarget
end

function AOE_Webs(skill, entity, target)
	local radius = skill.level
	local hitsTarget = false

	for x = -radius, radius do
		for y = -radius, radius do
			if (x == 0 and y == 0) then
				hitsTarget = true
			end
		end
    end

	return hitsTarget
end

function AdjacentToTarget_AOE(skill, entity, target)
	return (AOE(skill, entity, target) and AdjacentToTarget(skill, entity, target))
end

function Clamp(num, min, max)
	if (num < min) then
		num = min
	elseif (num > max) then
		num = max
	end

	return num
end