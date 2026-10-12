using UnityEngine;

// Kamera mengikuti player. Posisi kamera "dikunci" ke grid piksel layar
// supaya gambar tetap tajam (tidak bergetar/blur) saat bergerak.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smooth = 8f;
    public bool pixelSnap = true;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 p = target.position;
        p.z = transform.position.z;

        Vector3 next = smooth <= 0f
            ? p
            : Vector3.Lerp(transform.position, p, 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime));

        // kunci ke piksel layar -> gambar tetap tajam saat bergerak
        if (pixelSnap && cam != null && cam.orthographic && Screen.height > 0)
        {
            float unitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
            next.x = Mathf.Round(next.x / unitsPerPixel) * unitsPerPixel;
            next.y = Mathf.Round(next.y / unitsPerPixel) * unitsPerPixel;
        }

        transform.position = next;
    }
}
