using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public float smoothSpeed = 5f;
    public float floorBound = 20f;
    private Vector3 _offset;
    private float _camHalfWidth;
    private float _camHalfHeight;

    void Start()
    {
        _offset = transform.position;
        Camera cam = GetComponent<Camera>();
        _camHalfHeight = cam.orthographicSize;
        _camHalfWidth = _camHalfHeight * cam.aspect;
    }

    void LateUpdate()
    {
        var player = PlayerController.Instance;
        if (player == null) return;
        Vector3 target = player.transform.position + _offset;
        target.x = Mathf.Clamp(target.x, -floorBound + _camHalfWidth,  floorBound - _camHalfWidth);
        target.y = Mathf.Clamp(target.y, -floorBound + _camHalfHeight, floorBound - _camHalfHeight);
        target.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, target, smoothSpeed * Time.deltaTime);
    }
}