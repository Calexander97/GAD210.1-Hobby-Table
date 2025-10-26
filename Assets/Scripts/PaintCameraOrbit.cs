// PaintCameraOrbit.cs
using UnityEngine;

public class PaintCameraOrbit : MonoBehaviour
{
    public Transform pivot;         // set at runtime to "current unit" center
    public Camera cam;              // assign your paint camera (or Camera.main in Awake)
    public float orbitSpeed = 120f;
    public float zoomSpeed = 5f;
    public float minDist = 1.5f;
    public float maxDist = 8f;

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        if (!pivot || !cam) return;

        // Orbit (RMB)
        if (Input.GetMouseButton(1))
        {
            float dx = Input.GetAxis("Mouse X");
            transform.RotateAround(pivot.position, Vector3.up, dx * orbitSpeed * Time.deltaTime);
        }

        // Zoom (wheel) by dollying along view vector, clamped to pivot distance
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            Vector3 dir = (cam.transform.position - pivot.position).normalized;
            float dist = Vector3.Distance(cam.transform.position, pivot.position);
            dist = Mathf.Clamp(dist - scroll * zoomSpeed, minDist, maxDist);
            cam.transform.position = pivot.position + dir * dist;
        }

        // Always look at pivot
        cam.transform.LookAt(pivot.position);
    }
}
