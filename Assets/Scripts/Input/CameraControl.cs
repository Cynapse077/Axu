using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public Vector3 startPos;
    Vector3 targetPos = Vector3.zero;
    Transform target;

    bool started = false;

    float minX, maxX, minY, maxY;
    float scale = 4f;
    Camera cam;

    public void Init()
    {
        cam = Camera.main;
        transform.localPosition = startPos;
        started = true;
        targetPos.z = -10;
        Resize();
    }

    public void SetTargetTransform(Transform targetTransform)
    {
        target = targetTransform;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            ChangeScale(2);
        }
        else if (Input.GetKeyDown(KeyCode.PageDown))
        {
            ChangeScale(-2);
        }
    }

    void ChangeScale(int amount)
    {
        scale += amount;
        scale = Mathf.Clamp(scale, 2, 10);
        Resize();
    }

    void LateUpdate()
    {
        if (started && target != null)
        {
            targetPos.x = Mathf.Round(target.position.x * Manager.TileResolution) / Manager.TileResolution;
            targetPos.y = Mathf.Round(target.position.y * Manager.TileResolution) / Manager.TileResolution;
            ClampToMap();
            transform.localPosition = targetPos;
        }
    }

    public void Resize()
    {
        cam.orthographicSize = Screen.height / (float)Manager.TileResolution / scale;
        float horExtent = cam.orthographicSize * Screen.width / Screen.height;

        minY = cam.orthographicSize - (Manager.localMapSize.y + 6);
        maxY = Manager.localMapSize.y - cam.orthographicSize - (Manager.localMapSize.y - 4);
        minX = horExtent - 4;
        maxX = Manager.localMapSize.x - horExtent + 6;

        if (cam.orthographicSize >= Manager.localMapSize.y / 2f)
        {
            minY = maxY = -Manager.localMapSize.y / 2f;
        }

        if (cam.orthographicSize / (scale / 4f) >= Manager.localMapSize.x / 2f)
        {
            minX = maxX = Manager.localMapSize.x / 2f;
        }
    }
    public void ForcePosition()
    {
        transform.localPosition = targetPos;
    }

    void ClampToMap()
    {
        Vector3 newPos = targetPos;
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
        targetPos = newPos;
    }
}