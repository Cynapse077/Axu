function Test_Poison()
	PlayerEntity.stats.AddStatusEffect("Poison", 5)
end

function Test_Damage()
	PlayerEntity.stats.SimpleDamage(2)
end

function LevelUp()
	local amount = PlayerEntity.stats.MyLevel.XPToNext - PlayerEntity.stats.MyLevel.XP

	PlayerEntity.stats.MyLevel.AddXP(amount)
end

function SeverRandomBodyPart()
	local bodyParts = PlayerEntity.body.SeverableBodyParts()

	PlayerEntity.body.RemoveLimb(Random(1, #bodyParts))
end