using UnityEngine;
using System.Collections;

public class LerpPos : MonoBehaviour {

	Coord newPos;
	Vector3 newPosVec3;
	public float time = 40;

	public void Init(Coord p) {
        if (p == null)
            Destroy(gameObject);
        
		newPos = p;
		newPosVec3 = new Vector3(newPos.x, newPos.y - Manager.localMapSize.y, -1);
	}

	void Update() {
		if (newPos != null) {
			transform.position = Vector3.MoveTowards(transform.position, newPosVec3, Time.deltaTime * time);

			if (transform.position == newPosVec3)
				Destroy(gameObject);
		}
	}
}
