using UnityEngine;
using System.Collections;

public class DestroyAfterTurns : MonoBehaviour {
	public int lifeTime = 3;
	public GameObject childObject;
	public bool shrink;

	void Start () {
		World.turnManager.incrementTurnCounter += IncrementTurnCounter;
	}

	public void IncrementTurnCounter() {
		lifeTime --;

		if (lifeTime <= 0) {
			UnregisterAndDestroy();
			return;
		}
	}

	void UnregisterAndDestroy() {
		World.turnManager.incrementTurnCounter -= IncrementTurnCounter;
		Destroy(gameObject);
	}
}
