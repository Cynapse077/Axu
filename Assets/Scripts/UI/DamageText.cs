using UnityEngine;
using System.Collections;

public class DamageText : MonoBehaviour {

	public GameObject textObject;
	public GameObject[] shadowObjects;
    public GameObject bloodEffect;
	TextMesh toMesh;

	public void DisplayText(Color color, string text) {
        toMesh = textObject.GetComponent<TextMesh>();

        for (int i = 0; i < shadowObjects.Length; i++) {
            TextMesh soMesh = shadowObjects[i].GetComponent<TextMesh>();
            soMesh.color = Color.black;
            soMesh.text = text;
        }
		if (text.Length > 1)
			transform.localScale = new Vector3(0.8f, 1f, 1f);
		else if (text.Length > 2)
			transform.localScale = new Vector3(0.6f, 1f, 1f);

        toMesh.color = color;
        toMesh.text = text;
    }
}
