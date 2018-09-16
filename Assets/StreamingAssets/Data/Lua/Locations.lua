--NOTE: This feature has yet to be implemented ingame.

--Volcano underground
function OnTurn_Volcano()
	if (PlayerEntity == nil) then return end

	if (PlayerEntity.stats.HeatResist < 50 and Random(0, 100) < 5) then
		PlayerEntity.stats.IndirectAttack(Random(1, 4), DamageTypes.Heat, nil, "<color=orange>heat</color>", true, false, true)
	end
end

--Ice Caves underground
function OnTurn_IceCaves()
	if (PlayerEntity == nil) then return end

	if (PlayerEntity.stats.ColdResist < 50 and Random(0, 100) < 5) then
		PlayerEntity.stats.AddStatusEffect("Slow", Random(1, 3))
	end
end	