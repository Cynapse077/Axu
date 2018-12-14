using UnityEngine;
using System.Collections;

public class TimedDestroy : MonoBehaviour {

	public float destroyTime = 0.2f;
	public bool deactivate = false;

	void OnEnable () {
		Invoke("KillMe", destroyTime);
	}

	void KillMe() {
		if (deactivate) {
			if (transform.parent != null)
				transform.parent = null;

			SimplePool.Despawn(gameObject);
		} else
			Destroy(gameObject); 
	}
}
