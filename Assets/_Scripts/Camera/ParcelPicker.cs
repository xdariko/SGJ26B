using UnityEngine;

// Найти посылку под точкой монитора. Проецируем рамку каждой посылки в экран камеры,
// поэтому работает при любом наклоне камеры и без физики.
public static class ParcelPicker
{
    // Запас в долях экрана, чтобы мелкие посылки было легче ловить
    public static float padding = 0.015f;

    private static readonly Vector3[] Corners = new Vector3[8];

    public static Parcel PickViewport(Belt belt, Camera cam, Vector2 viewportPoint)
    {
        if (belt == null || cam == null)
            return null;

        Parcel best = null;
        float bestDistance = float.MaxValue;

        foreach (Parcel p in belt.Parcels)
        {
            if (p == null || p.IsResolved)
                continue;

            if (!Contains(cam, p.Visual.ClickBounds, viewportPoint))
                continue;

            // Если посылки перекрываются, берём ту, что ближе к камере
            float distance = (cam.transform.position - p.transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = p;
            }
        }

        return best;
    }

    private static bool Contains(Camera cam, Bounds b, Vector2 point)
    {
        Vector3 min = b.min, max = b.max;
        Corners[0] = new Vector3(min.x, min.y, min.z);
        Corners[1] = new Vector3(max.x, min.y, min.z);
        Corners[2] = new Vector3(min.x, max.y, min.z);
        Corners[3] = new Vector3(max.x, max.y, min.z);
        Corners[4] = new Vector3(min.x, min.y, max.z);
        Corners[5] = new Vector3(max.x, min.y, max.z);
        Corners[6] = new Vector3(min.x, max.y, max.z);
        Corners[7] = new Vector3(max.x, max.y, max.z);

        Vector2 vMin = new(float.MaxValue, float.MaxValue);
        Vector2 vMax = new(float.MinValue, float.MinValue);

        foreach (Vector3 c in Corners)
        {
            Vector3 v = cam.WorldToViewportPoint(c);
            if (v.z <= 0f)
                return false;

            vMin = Vector2.Min(vMin, v);
            vMax = Vector2.Max(vMax, v);
        }

        return point.x >= vMin.x - padding && point.x <= vMax.x + padding
            && point.y >= vMin.y - padding && point.y <= vMax.y + padding;
    }
}
