using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Encounter {
	public string Name, ID;
	public string alertTitle, alertText;
	public bool repeatable;
	public ProgressFlags[] progressReqs;
	public int lvlReq;

}

public class HostileEncounter : Encounter {
	public GroupBlueprint spawnGroup;
}

public class QuestEncounter : Encounter {
	public NPC_Blueprint questGiver;
	public Quest quest;
}
