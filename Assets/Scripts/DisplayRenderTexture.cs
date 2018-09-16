using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayRenderTexture : MonoBehaviour {
	RawImage img;

	void OnEnable() {
		if (img == null)
			img = GetComponent<RawImage>();
		
		if (img.texture == null && GameObject.FindObjectOfType<MiniMap>() != null)
			img.texture = GameObject.FindObjectOfType<MiniMap>().rt;
	}

	public void SetTextureToDisplay(RenderTexture rt) {
		GetComponent<RawImage>().texture = rt;
	}
}
