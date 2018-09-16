using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class TurnTimer {

	public int duration;
	public int remainingTime;
	public Action endAction;

	public TurnTimer(int dur, Action eAction) {
		duration = dur;
		endAction = eAction;

		World.turnManager.incrementTurnCounter += DecrementTimer;
	}

	public void DecrementTimer() {
		remainingTime --;

		if (remainingTime <= 0)
			Finished();
	}

	public void Finished() {
		endAction();
		World.turnManager.incrementTurnCounter -= DecrementTimer;
	}
}
