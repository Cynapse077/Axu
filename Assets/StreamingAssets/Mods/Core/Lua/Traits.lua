--Brain Rot
function OnTurn_BrainRot(entity, trait)
	if (trait == nil) then
		return
	end

	local progress = TurnManager.turn - trait.turnAcquired

	if (progress > 4000 and Random(0, 1000) < 1) then
		local heads = entity.body.GetBodyPartsBySlot(ItemProperty.Slot_Head)

		for key, val in pairs(heads) do
			if (val.GetStatMod("Intelligence").Amount > 1) then
				val.AddAttribute("Intelligence", -1)
				entity.stats.ChangeAttribute("Intelligence", -1)

				Log("<color=red>Your brian is deteriorating due to Brain Rot!</color>")
			end
		end

		trait.turnAcquired = TurnManager.turn
	end
end	

--Leprosy
function OnTurn_Leprosy(entity, trait)
	if (trait == nil) then
		return
	end

	local progress = TurnManager.turn - trait.turnAcquired

	if (progress > 4000 and Random(0, 500) < 1) then
		local bps = entity.body.SeverableBodyParts()

		if (#bps > 0) then
			bp = bps[Random(1, #bps)]

			if (bp.slot == ItemProperty.Slot_Head) then 
				trait.turnAcquired = TurnManager.turn
				return
			end

			Log("<color=red>Your " .. bp.displayName .. " has fallen off...</color>")
			entity.body.RemoveLimb(bp)

			trait.turnAcquired = TurnManager.turn
		end
	end
end

--Crystallization
function OnTurn_Crystallization(entity, trait)
	if (trait == nil) then
		return
	end

	local progress = TurnManager.turn - trait.turnAcquired
	local bps = entity.stats.UnCrystallizedParts()

	if (#bps > 0) then
		if (progress > 2000 and Random(0, 500) < 1) then
			bpToChange = bps[Random(1, #bps)]

			bpToChange.armor = bpToChange.armor + 1
			bpToChange.effect = TraitEffects.Crystallization
			bpToChange.name = "<color=cyan>" .. bpToChange.name .. "</color>"

			Log("Your " .. bpToChange.displayName .. " has solidified into crystal.")

			if (entity.stats.Dexterity > 3 and Random(0, 100) < 70) then
				bpToChange.AddAttribute("Dexterity", -1)
				entity.stats.ChangeAttribute("Dexterity", -1)

				Log("<color=red>Your " .. bpToChange.displayName .. " feels less Dexterous.</color>")
			end

			trait.turnAcquired = TurnManager.turn
		end
	end
end	

--Vampire Fledgling
function OnTurn_PreVamp(entity, trait)
	if (trait == nil) then
		return
	end

	local progress = TurnManager.turn - trait.turnAcquired

	if (progress > 4500) then
		Alert.CustomAlert("<color=red>You have become a full Vampire!</color>\nYou feel stronger, but your diseased body will not improve on its own.")
		entity.stats.InitializeNewTrait(TraitList.GetTraitByID("vampirism"))
		entity.stats.RemoveTrait(trait.ID)
	end
end