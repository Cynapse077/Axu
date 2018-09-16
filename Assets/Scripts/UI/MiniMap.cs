using UnityEngine;
using System.Collections;

public class MiniMap : MonoBehaviour {
	public RenderTexture rt;

	float openOrthoSize, miniOrthoSize;
	Camera miniMap;
	float scaleFactor = 4f;

	void Start () {
		miniMap = GetComponent<Camera>();
		miniMap.aspect = 1f;
		openOrthoSize = (Screen.height / 16f / scaleFactor);
		miniOrthoSize = openOrthoSize / 4f;

		rt = new RenderTexture(Screen.width, Screen.height, 0);
		rt.filterMode = FilterMode.Point;
		miniMap.targetTexture = rt;

		DisplayRenderTexture[] drt = GameObject.FindObjectsOfType<DisplayRenderTexture>();
		foreach (DisplayRenderTexture d in drt) {
			d.SetTextureToDisplay(rt);
		}

		Transition(false);
	}

	public void Transition(bool fullMap) {
		miniMap.orthographicSize = (fullMap) ? openOrthoSize : miniOrthoSize;
		miniMap.aspect = (fullMap) ? Camera.main.aspect : 1f;

		if (World.tileMap != null)
			miniMap.farClipPlane = (World.tileMap.currentElevation == 0) ? 100 : 0.02f;
		else
			Camera.main.farClipPlane = (fullMap) ? 0.02f : 100f;
	}
}
