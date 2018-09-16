using UnityEngine;

public class SnapToPixelGrid : MonoBehaviour 
{
	[SerializeField]
	public int pixelsPerUnit = 16;

	private Transform parent;

	private void Start() {
		parent = transform.parent;
	}

	private void LateUpdate()  {
		Vector3 newLocalPosition = Vector3.zero;

		newLocalPosition.x = (Mathf.Round(parent.position.x * pixelsPerUnit) / pixelsPerUnit) - parent.position.x + 0.5f;
		newLocalPosition.y = (Mathf.Round(parent.position.y * pixelsPerUnit) / pixelsPerUnit) - parent.position.y + 0.5f;

		transform.localPosition = newLocalPosition;
	}
}