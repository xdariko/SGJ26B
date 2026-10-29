using UnityEngine;

// То же искажение «рыбий глаз», что в шейдере CCTV. Нужно, чтобы клик попадал туда, куда игрок видит.
public static class CctvMath
{
    public static Vector2 Distort(Vector2 uv, float distortion)
    {
        Vector2 d = uv * 2f - Vector2.one;
        float r2 = d.sqrMagnitude;
        d *= 1f + distortion * r2;
        return d * 0.5f + new Vector2(0.5f, 0.5f);
    }
}
