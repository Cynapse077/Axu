using UnityEngine;

public class SnapToPixelGrid : MonoBehaviour
{
    private Transform parent;

    private void Start()
    {
        parent = transform.parent;
    }

    private void LateUpdate()
    {
        Vector3 newLocalPosition = Vector3.zero;

        newLocalPosition.x = (Mathf.Round(parent.position.x * Manager.TileResolution) / Manager.TileResolution) - parent.position.x + 0.5f;
        newLocalPosition.y = (Mathf.Round(parent.position.y * Manager.TileResolution) / Manager.TileResolution) - parent.position.y + 0.5f;

        transform.localPosition = newLocalPosition;
    }
}