using UnityEngine;
using System.Collections;

public class SR_EnabledBasedOnOther : MonoBehaviour {

	public SpriteRenderer parent;
	SpriteRenderer sRenderer;

	void Start() {
		sRenderer = GetComponent<SpriteRenderer>();
		sRenderer.enabled = parent.enabled;
	}
	
	// Update is called once per frame
	void Update () {
		sRenderer.enabled = parent.enabled;
	}
}
