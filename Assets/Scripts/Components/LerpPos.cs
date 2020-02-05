using UnityEngine;
using System.Collections;

public class LerpPos : MonoBehaviour
{

	Coord newPos;
	Vector3 newPosVec3;
	public float time = 40;
    float speedFactor = 1.0f;

	public void Init(Coord p, float spdMul)
    {
        if (p == null)
        {
            Destroy(gameObject);
        }

        speedFactor = spdMul;
		newPos = p;
		newPosVec3 = new Vector3(newPos.x, newPos.y - Manager.localMapSize.y, -1);
	}

	void Update()
    {
		if (newPos != null)
        {
			transform.position = Vector3.MoveTowards(transform.position, newPosVec3, Time.deltaTime * time * speedFactor);

			if (transform.position == newPosVec3)
            {
                Destroy(gameObject);
            }
		}
	}
}
