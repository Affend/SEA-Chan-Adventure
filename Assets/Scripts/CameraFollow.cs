using UnityEngine;

// Kamera mengikuti player. Posisi kamera "dikunci" ke grid piksel layar
// supaya gambar tetap tajam (tidak bergetar/blur) saat bergerak.
// Bisa juga diarahkan sementara ke objek lain (dipakai cutscene boss).
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smooth = 8f;
    public bool pixelSnap = true;

    Camera cam;
    float baseSize;

    bool focusOverride;
    Vector3 focusPos;
    float focusSpeed = 6f;
    float sizeTarget;

    float shakeTime, shakeDur, shakeMag;

    // Goyangkan kamera (umpan balik saat kena damage / boss meledak).
    public void Shake(float magnitude, float duration)
    {
        if (shakeTime <= 0f || magnitude >= shakeMag)
        {
            shakeMag = magnitude;
            shakeDur = Mathf.Max(0.05f, duration);
            shakeTime = shakeDur;
        }
    }

    public static void DoShake(float magnitude, float duration)
    {
        if (Camera.main == null) return;
        var cf = Camera.main.GetComponent<CameraFollow>();
        if (cf != null) cf.Shake(magnitude, duration);
    }

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            baseSize = cam.orthographicSize;
            sizeTarget = baseSize;
        }
    }

    // Arahkan kamera ke titik tertentu (mis. lokasi boss muncul).
    public void SetFocus(Vector3 pos, float zoomOut = 1.15f)
    {
        focusOverride = true;
        focusPos = pos;
        sizeTarget = baseSize * zoomOut;   // zoom keluar sedikit supaya area terlihat
    }

    // Kembali mengikuti player.
    public void ClearFocus()
    {
        focusOverride = false;
        sizeTarget = baseSize;
    }

    void LateUpdate()
    {
        Vector3 desired;

        if (focusOverride)
        {
            desired = focusPos;
        }
        else
        {
            if (target == null) return;
            desired = target.position;
        }
        desired.z = transform.position.z;

        float sp = focusOverride ? focusSpeed : smooth;
        Vector3 next = sp <= 0f
            ? desired
            : Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-sp * Time.unscaledDeltaTime));

        // ukuran kamera (zoom) ikut berubah halus
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, sizeTarget,
                1f - Mathf.Exp(-5f * Time.unscaledDeltaTime));
        }

        // kunci ke piksel layar -> gambar tetap tajam saat bergerak
        if (pixelSnap && cam != null && cam.orthographic && Screen.height > 0)
        {
            float unitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
            next.x = Mathf.Round(next.x / unitsPerPixel) * unitsPerPixel;
            next.y = Mathf.Round(next.y / unitsPerPixel) * unitsPerPixel;
        }

        // goyangan kamera (setelah dikunci ke piksel agar tetap terlihat)
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(shakeTime / shakeDur);
            next.x += Random.Range(-1f, 1f) * shakeMag * k;
            next.y += Random.Range(-1f, 1f) * shakeMag * k;
        }

        transform.position = next;
    }
}
