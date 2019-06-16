
function Scrub_Radiation(stats)
	if (TurnManager.turn % 100 == 0) then
		stats.RemoveRadiation(1)
	end
end