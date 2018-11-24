-- Basic incremental healing skills
function Healing(caster, skill)
	local amount = skill.totalDice.Roll()

	caster.stats.Heal(amount)
	ApplyChanges(caster, skill)
end

--Heal to full health
function FullHeal(caster, skill)
	local amount = caster.stats.maxHealth

	caster.stats.Heal(amount)
	ApplyChanges(caster, skill)
end

--Reduce physical damage by half for the duration.
function Brace(caster, skill)
	local amount = caster.stats.Endurance + skill.level

	caster.stats.AddStatusEffect("Shield", amount)
	ApplyChanges(caster, skill)
end	

--Stamina Restoration.
function RestoreStamina(caster, skill)
	local amount = skill.totalDice.Roll()

	caster.stats.RestoreStamina(amount) 
	ApplyChanges(caster, skill)
end

--Teleports the caster to their home base.
function ReturnHome(caster, skill)
	if (ObjectManager.SafeToRest()) then
		TileMap.GoToArea("Home")
		TileMap.HardRebuild()
		ApplyChanges(caster, skill)
	else
		Log("It is too dangerous to leave.")
	end
end

--Give Haste for X number of turns.
function Sprint(caster, skill)
	if (caster.isPlayer) then
		Log("You begin to sprint.")
	else
		Log(caster.Name .. " begins to sprint.")
	end

	caster.stats.AddStatusEffect("Haste", caster.stats.Endurance + skill.level - 2)
	ApplyChanges(caster, skill)
end

--Move in a single direction, attacking the first target in the way.
function Charge(caster, direction, skill)
	local amount = skill.level + 3

	caster.Charge(direction, amount)

	if (caster.isPlayer) then
		caster.body.TrainLimbOfType(ItemProperty.Slot_Leg)
	end

	ApplyChanges(caster, skill)
end

--Attack a body part directly.
function CalledShot(caster, direction, skill)
	local targetCell = TileMap.GetCellAt(caster.myPos + direction)

	if (targetCell ~= nil and targetCell.entity ~= nil) then
		if (targetCell.entity.isPlayer) then
			UI.CloseWindows()
		else
			UI.CalledShot(targetCell.entity.body)
		end
	end

	ApplyChanges(caster, skill)
end

--Bash target with equipped shield
function ShieldBash(caster, direction, skill)
	if (not TargetAvailableInDirection(caster.myPos, direction)) then
		return
	end

	local targetStats = TileMap.GetCellAt(caster.myPos + direction).entity.stats
	local amount = skill.totalDice.Roll()

	targetStats.IndirectAttack(amount, DamageTypes.Blunt, caster, "shield", false)
	targetStats.AddStatusEffect("Stun", Random(1, 4))
	targetStats.entity.ForceMove(direction.x, direction.y, caster.stats.Strength)

	if (caster.isPlayer) then
		caster.body.TrainLimbOfType(ItemProperty.Slot_Arm)
	end

	ApplyChanges(caster, skill)
end

--Push an entity back
function Shove(caster, direction, skill)
	if (not TargetAvailableInDirection(caster.myPos, direction)) then
		return
	end

	local target = TileMap.GetCellAt(caster.myPos + direction).entity

	target.ForceMove(direction.x, direction.y, caster.stats.Strength + skill.level)
	target.stats.AddStatusEffect("Stun", Random(0, 3))
	Log_Combat("shoves", caster.Name, target.Name, target.isPlayer)

	if (caster.isPlayer) then
		caster.body.TrainLimbOfType(ItemProperty.Slot_Arm)
	end

	ApplyChanges(caster, skill)
end

--Topple an adjacent entity
function Trip(caster, direction, skill)
	if (not TargetAvailableInDirection(caster.myPos, direction)) then
		return
	end

	local target = TileMap.GetCellAt(caster.myPos + direction).entity
	local amount = skill.totalDice.Roll()

	target.stats.AddStatusEffect("Topple", Random(2, 6))
	target.stats.IndirectAttack(amount, DamageTypes.Blunt, caster, caster.Name, false)

	if (caster.isPlayer) then
		caster.body.TrainLimbOfType(ItemProperty.Slot_Arm)
	end

	ApplyChanges(caster, skill)
end

--Blind an entity
function Blind(caster, direction, skill)
	if (not TargetAvailableInDirection(caster.myPos, direction)) then
		return
	end

	local target = TileMap.GetCellAt(caster.myPos + direction).entity

	target.stats.AddStatusEffect("Blind", Random(3, 7))
	Log(caster.Name .. " blinds " .. target.Name .. ".")

	if (target.isPlayer) then
		TileMap.SoftRebuild()
	end

	ApplyChanges(caster, skill)
end

--Cause bleeding. Given via "Horns" mutation.
function Gore(caster, direction, skill)
	if (not TargetAvailableInDirection(caster.myPos, direction)) then
		return
	end

	local target = TileMap.GetCellAt(caster.myPos + direction).entity
	local amount = skill.totalDice.Roll()

	target.stats.AddStatusEffect("Bleed", Random(2, 6))
	target.stats.IndirectAttack(amount, DamageTypes.Pierce, caster, caster.Name, false)

	ApplyChanges(caster, skill)
end	

--Swap places with a target, and attack them.
function Juke(caster, direction, skill)
	if (not TargetAvailableInDirection(caster.myPos, direction)) then
		return
	end

	local target = TileMap.GetCellAt(caster.myPos + direction).entity

	Log(caster.Name .. " leaps over " .. target.Name .. ", attacking them in the air.")

	if (not caster.isPlayer or not target.AI.isStationary) then
		caster.SwapPosition(direction, target)
	end

	target.stats.AddStatusEffect("Stun", Random(2, 5))
	SoundManager.AttackSound2()

	local weapon = caster.body.MainHand.equippedItem

	if (target.stats.TakeDamage(weapon, caster.stats.Dexterity + skill.level, caster)) then
		if (caster.isPlayer) then
			caster.body.TrainLimbOfType(ItemProperty.Slot_Arm)
			caster.body.TrainLimbOfType(ItemProperty.Slot_Leg)
		end
	end

	ApplyChanges(caster, skill)
end

--Instantiates effects in a cone, given a direction, origin, and length
function Cone(caster, direction, skill)
	local length = 3 + skill.level

	if (length > 7) then
		length = 7
	end

	local spawns = PositionsInCone(caster, caster.myPos, direction, length)

	for key,pos in pairs(spawns) do
		SpawnEffect(caster, pos.x, pos.y, skill, 0)
	end

	ApplyChanges(caster, skill)
end

--Instantiates an effect over the points of a Bresenham line.
function Line(caster, final, skill)
	local line = BLine.GetPoints(caster.myPos, final)

	for key,pos in pairs(line) do
		SpawnEffect(caster, pos.x, pos.y, skill, 0)
	end

	if (caster.isPlayer) then
		caster.body.TrainLimbOfType(ItemProperty.Slot_Head)
	end

	ApplyChanges(caster, skill)
end

--Cardinal/Orthagonal Directional beam.
function Beam(caster, direction, skill)
	local x = caster.posX + direction.x
	local y = caster.posY + direction.y
	local rotation = 0

	if (direction.x == 0 and direction.y == 0) then
		return
	end

	if (direction.x ~= 0 and direction.y ~= 0) then
		if (direction.x > 0) then
			if (direction.y > 0) then rotation = 45 else rotation = -45 end
		else
			if (direction.y > 0) then rotation = -45 else rotation = 45 end
		end
	elseif (direction.x == 0 and direction.y ~= 0) then
		rotation = 90
	end

	while (x >= 0 and x < LocalMapSize.x and y >= 0 and y < LocalMapSize.y) do
		if (not TileMap.PassThroughableTile(x, y)) then 
			break 
		end

		SpawnEffect(caster, x, y, skill, rotation)
		x = x + direction.x
		y = y + direction.y
	end

	if (caster.isPlayer) then
		caster.body.TrainLimbOfType(ItemProperty.Slot_Head)
	end

	ApplyChanges(caster, skill)
end

--Square of effects with a set radius.
function Square(caster, position, skill)
	local radius = 1

	for x = -radius, radius do
		for y = -radius, radius do
			local c = Coord.__new(position.x + x, position.y + y)

			if (c ~= caster.myPos) then
				SpawnEffect(caster, c.x, c.y, skill, 0)
			end
		end
	end

	if (caster.isPlayer) then
		caster.body.TrainLimbOfType(ItemProperty.Slot_Head)
		TileMap.SoftRebuild()
	end

	ApplyChanges(caster, skill)
end

--Create web objects around the caster
function SpinWebs(caster, position, skill)
	local radius = skill.level

	for x = -radius, radius do
		for y = -radius, radius do
			if (Random(0, 100) < 30) then
				local c = Coord.__new(position.x + x, position.y + y)

				if (c ~= caster.myPos) then
					ObjectManager.NewObject("Web", c)
				end
			end
		end
	end

	ApplyChanges(caster, skill)
end

--Summon a specific NPC type to fight for you
function Summon(caster, skill, npcID)
	empty = caster.GetEmptyCoords(2)

	if (#empty <= 0) then
		return
	end

	localPos = empty[Random(1, #empty)]

	npc = NPC.__new(npcID, TileMap.WorldPosition, localPos, TileMap.currentElevation)
	npc.maxHealth = npc.maxHealth + (5 * skill.level)
	npc.Attributes["Strength"] = npc.Attributes["Strength"] + skill.level
	npc.MakeFollower()
	npc.AddFlag(NPC_Flags.Deteriortate_HP)
	ObjectManager.SpawnNPC(npc)

	ApplyChanges(caster, skill)
end

--Teleport to a specified location
function Blink(caster, targetPos, skill)
	caster.cell.UnSetEntity(caster)
	caster.myPos = targetPos
	caster.ForcePosition()
	caster.SetCell()
	caster.BeamDown()

	Log(caster.Name .. " teleports away!")
	SoundManager.TeleportSound()

	if (caster.isPlayer) then
		TileMap.SoftRebuild()
		caster.body.TrainLimbOfType(ItemProperty.Slot_Head)
	end

	ApplyChanges(caster, skill)
end

--Base grappling skill
function Grapple(caster, skill)
	if (caster.isPlayer) then
		UI.OpenGrapple()
	end

	ApplyChanges(caster, skill)
end

--Vampirism - Drain Blood
function DrainBlood(caster, direction, skill)
	if (not TargetAvailableInDirection(caster.myPos, direction)) then
		return
	end

	local target = TileMap.GetCellAt(caster.myPos + direction).entity
	local amount = skill.totalDice.Roll()

	if (target.stats.IndirectAttack(amount, DamageTypes.Pierce, caster, "bite", true) > 0) then
		Log("<color=red>" .. caster.Name .. " drains " .. target.Name .. "'s blood, and heals " .. amount .. " damage.</color>")
		target.stats.AddStatusEffect("Bleed", Random(3, 6))

		caster.stats.Heal(amount)

		--Give player vampirism
		if (target.isPlayer and Random(0, 100) < 100) then
			if (not target.stats.hasTrait("pre_vamp")) then
				target.stats.InitializeNewTrait(TraitList.GetTraitByID("pre_vamp"))
				Alert.CustomAlert("You have been bitten by a Vampire, and become a Fledgling Vampire yourself! Cure this disease or become one of them!")
			end
		end
	end

	ApplyChanges(caster, skill)
end	

function SpawnEffect(caster, x, y, skill, rotation)
	local effectName = ""
	local id = 0
	local damage = skill.totalDice.Roll()

	if (damage > 0) then
		damage = damage + caster.stats.Intelligence
	end

	if (skill.damageType == DamageTypes.Heat) then
		id = 0
		effectName = "heat"
	elseif (skill.damageType == DamageTypes.Energy) then
		id = 1
		effectName = "shock"
	elseif (skill.damageType == DamageTypes.Venom) then
		id = 2
		effectName = "poison"
		damage = Random(1, 4)
	elseif (skill.damageType == DamageTypes.NonLethal) then
		damage = 0

		if (skill.HasTag(AbilityTags.Blind)) then
			id = 5
			effectName = "ink"
		else
			id = 3
			effectName = "gas"
		end
	elseif (skill.damageType == DamageTypes.Cold) then
		id = 4
		effectName = "chill"
	elseif (skill.damageType == DamageTypes.Blunt) then
		id = 7
		effectName = "earth"
	elseif (skill.damageType == DamageTypes.Radiation) then
		id = 8
		effectName = "radiation"
	else
		return
	end

	ObjectManager.SpawnEffect(id, effectName, caster, x, y, damage, skill, rotation)
end

function TargetAvailableInDirection(position, direction)
	return (TileMap.WalkableTile(position.x + direction.x, position.y + direction.y) and TileMap.GetCellAt(position + direction).entity ~= nil)
end

function Clamp(num, min, max)
	if (num < min) then
		num = min
	elseif (num > max) then
		num = max
	end

	return num
end

function Cast(caster, skill)
	if (caster.isPlayer) then
		UI.CloseWindows()
	end

	if (not CanUseSkill(caster, skill)) then
		return
	end

	if (skill.castType ~= CastType.Instant) then
		Input.activeSkill = skill

		if (skill.castType == CastType.Direction) then
			Input.UseDirectionalSkill(skill)
		elseif (skill.castType == CastType.Target) then
			Input.UseSelectTileSkill(skill)
		end

		return
	end

	--Exceptions
	if (skill.HasTag(AbilityTags.Summon)) then
		skill.CallScriptFunction(caster, skill, skill.luaAction.variable)
	elseif (skill.HasTag(AbilityTags.Small_Square)) then
		skill.CallScriptFunction(caster, caster.myPos, skill)
	else
		skill.CallScriptFunction(caster, skill)
	end
end

function Cast_Coordinate(caster, pos, skill)
	if (caster.isPlayer) then
		UI.CloseWindows()
	end

	if (not CanUseSkill(caster, skill)) then
		return
	end

	--LuaManager.CallScriptFunction(skill.luaAction.scriptName, skill.luaAction.functionName, caster, pos, skill)
	skill.CallScriptFunction(caster, pos, skill)
end

function CanUseSkill(caster, skill)
	if (skill.cooldown > 0) then
		if (caster.isPlayer) then
			Alert.CustomAlert("This ability is on cooldown. It will be available in <color=yellow><b>" .. skill.cooldown .. "</b></color> turns.")
		end
		return false
	end

	if (caster.stats.stamina < skill.staminaCost) then
		if (caster.isPlayer) then
			Alert.NewAlert("Not_Enough_Stamina")
		end

		return false
	end

	return true
end

function ApplyChanges(caster, skill)
	if (skill.HasTag(AbilityTags.OpensNewWindow)) then
		return
	end

	caster.stats.UseStamina(skill.staminaCost)
	skill.InitializeCooldown()

	if (caster.isPlayer) then
		skill.AddXP(caster.stats.Intelligence)
	end

	if (skill.HasTag(AbilityTags.Radiate_Self)) then
		if (caster.isPlayer and Random(0, 100) <= 20) then
			caster.stats.Radiate(Random(0, 6))
		end
	end

	caster.EndTurn(0.3, skill.timeCost)
end