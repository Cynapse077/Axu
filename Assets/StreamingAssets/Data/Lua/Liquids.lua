--Functions for handling what happens when liquids are consumed or splashed on a creature.

--Default
function OnSplash_Base(stats)
	if (stats.HasEffect("Aflame")) then
		stats.RemoveStatusEffect("Aflame")
		Log("The flames surrounding " .. stats.entity.Name .. " have been quenched.")
	end
end

--Unstable Liquid
function OnDrink_Unstable(stats) 
	Log("The unstable mixture explodes!")
	ObjectManager.SpawnExplosion(nil, stats.entity.myPos)
end

function OnSplash_Unstable(stats) 
	Log("The unstable mixture explodes!")
	ObjectManager.SpawnExplosion(nil, stats.entity.myPos)
end	

--Fresh Water
function OnDrink_Water(stats) 
	Log("Very refreshing.")
end

--Salt Water
function OnDrink_SaltWater(stats) 
	Log("Yuck! It's very salty.")
end

--Preservative
function OnCoat_Preservative(item)
	item.Preserve()
end

function OnDrink_Preservative(stats)
	stats.AddStatusEffect("Poison", 3)
end

--Salamandis
function OnDrink_Salamandis(stats)
	stats.entity.body.RegrowLimbs()
end

--Regenitrol
function OnDrink_Regenitrol(stats)
	stats.AddStatusEffect("Regen", 5)
end

function OnSplash_Regenitrol(stats)
	stats.AddStatusEffect("Regen", 2)
	OnSplash_Base(stats)
end

--Poison
function OnDrink_Poison(stats)
	stats.AddStatusEffect("Poison", 10)
end

function OnSplash_Poison(stats)
	stats.AddStatusEffect("Poison", 5)
	OnSplash_Base(stats)
end

--Mutagen
function OnSplash_Mutagen(stats)
	if (stats.entity.isPlayer) then
		stats.Radiate(Random(60, 90))
	else
		stats.AddStatusEffect("Poison", 5)
	end
end

function OnDrink_Mutagen(stats)
	if (stats.entity.isPlayer) then
		stats.Radiate(100)
	else
		stats.AddStatusEffect("Poison", 7)
	end

	OnSplash_Base(stats)
end

--Lunium
function OnDrink_Lunium(stats)
	if (stats.entity.isPlayer) then
		stats.CureRandomMutations(Random(1, 3))
	end
end

function OnSplash_Lunium(stats)
	if (stats.entity.isPlayer and Random(0, 100) < 50) then
		stats.CureRandomMutations(1)
	end

	OnSplash_Base(stats)
end

--Alcohol
function OnDrink_Alcohol(stats)
	stats.AddStatusEffect("Drunk", 20, 60)

	if (Random(0, 100) < 10) then
		stats.AddStatusEffect("Sick", Random(11, 30))
	end
end

function OnSplash_Alcohol(stats) 
	if (stats.HasEffect("Aflame")) then
		stats.AddStatusEffect("Aflame", Random(5, 13))
		Log("The splashed Alcohol feeds the flames on" .. stats.entity.Name .. "!!")
	end
end

--Lava
function OnDrink_Lava(stats)
	stats.AddStatusEffect("Aflame", Random(10, 20))
	stats.IndirectAttack(100, DamageTypes.Heat, stats.entity, "lava", true, false, false)
end

function OnSplash_Lava(stats)
	stats.AddStatusEffect("Aflame", Random(6, 14))
	stats.IndirectAttack(Random(20, 55), DamageTypes.Heat, nil, "lava", true, false, false)
end

--Vomit
function OnDrink_Vomit(stats)
	stats.AddStatusEffect("Sick", Random(11, 65))
	Log("You retch.")
end

--Blood
function OnDrink_Blood(stats)
	
end

--Vampiric Blood
function OnDrink_Blood_Vamp(stats)
	if (stats.entity.isPlayer and not stats.hasTrait("pre_vamp") and not stats.hasTrait("vampirism")) then
		stats.InitializeNewTrait(TraitList.GetTraitByID("pre_vamp"))
		Alert.CustomAlert_WithTitle("Vampirism!", "You have contracted Fledgling Vampirism from drinking tainted blood!")
		OnDrink_Blood(stats)
	end
end

--Poisonous Blood
function OnDrink_Blood_Poison(stats)
	stats.AddStatusEffect("Poison", 5)
	OnDrink_Blood(stats)
end

function OnSplash_Blood_Poison(stats)
	OnSplash_Base(stats)
	stats.AddStatusEffect("Poison", 3)
end

--Leprosic Blood
function OnDrink_Blood_Lep(stats)
	if (stats.entity.isPlayer and not stats.hasTrait("leprosy")) then
		stats.InitializeNewTrait(TraitList.GetTraitByID("leprosy"))
		Alert.CustomAlert_WithTitle("Leprosy!", "You have contracted Leprosy from drinking tainted blood!")
		OnDrink_Blood(stats)
	end
end

--Antidote
function OnDrink_Antidote(stats)
	stats.RemoveStatusEffect("Poison")
end

--Cytopke
function OnDrink_Cytopke(stats)
	stats.AddStatusEffect("Regen", 20)
end

--Float
function OnDrink_Float(stats)
	stats.AddStatusEffect("Float", 10)
	Log(stats.entity.Name .. " floats into the air.")
end

--Get a random mutation from a pool, if you do not already have all of them.
function MutateRandom(stats, mutations)
	local mutation = stats.FindRandomUnheldMutation(mutations)

	if (mutation == nil) then
		Log("It has no effect.")
	else
		stats.Mutate(mutation)
	end
end

--Specialized Mutagens
function OnDrink_Mutagen_Bird(stats)
	if (not stats.entity.isPlayer) then return end

	local mutations = {
		"wings", "beak", "hollowbones", "lithe", "deft", "nimble"
	}

	MutateRandom(stats, mutations)
end

function OnDrink_Mutagen_Spider(stats)
	if (not stats.entity.isPlayer) then return end

	local mutations = {
		"nostick", "rubberskin", "flyeyes", "chitin", "exarm", "stealthy"
	}

	MutateRandom(stats, mutations)
end

function OnDrink_Mutagen_Reptile(stats)
	if (not stats.entity.isPlayer) then return end

	local mutations = {
		"slitnostrils", "webbed", "claws", "coldblood", "scales", "serpentine", "selpig"
	}

	MutateRandom(stats, mutations)
end

function OnDrink_Mutagen_Chimera(stats)
	if (not stats.entity.isPlayer) then return end

	local mutations = {
		"exhead", "exleg", "claws", "horns", "wings", "extail", "densebones"
	}

	MutateRandom(stats, mutations)
end

function OnDrink_Mutagen_Aquatic(stats)
	if (not stats.entity.isPlayer) then return end

	local mutations = {
		"aquatic", "dorsalfin", "shell", "extail", "tendrils"
	}

	MutateRandom(stats, mutations)
end	