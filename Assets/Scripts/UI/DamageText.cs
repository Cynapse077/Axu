using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
	public GameObject textObject;
    public GameObject bloodEffect;
    TextMeshPro toMesh;

	public void DisplayText(Color color, string text)
    {
        if (toMesh == null)
            toMesh = textObject.GetComponent<TextMeshPro>();

        toMesh.color = color;
        toMesh.text = text;
    }
}
