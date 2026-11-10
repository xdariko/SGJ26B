using System;
using UnityEngine;

// Камера над лентой. Рендерит в свою RenderTexture: разрешение = чёткость, видимые слои = уровень камеры.
[RequireComponent(typeof(Camera))]
public class BeltCamera : MonoBehaviour
{
    private Camera cam;
    private RenderTexture rt;

    public Camera Camera => cam;
    public Texture Output => rt;
    public int Level { get; private set; } = -1;

    public event Action<Texture> OnOutputChanged;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void Setup(int camLevel, int sharpness)
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        Vector2Int[] resolutions = G.config.cameraResolutions;
        Vector2Int res = resolutions[Mathf.Clamp(sharpness, 0, resolutions.Length - 1)];

        if (rt == null || rt.width != res.x || rt.height != res.y)
        {
            ReleaseTexture();
            rt = new RenderTexture(res.x, res.y, 16)
            {
                name = $"{name}_RT",
                filterMode = FilterMode.Bilinear,
                // Сглаживание краёв: без него на высоких уровнях «Протереть глаза» видны лесенки
                antiAliasing = 2,
                wrapMode = TextureWrapMode.Clamp
            };
            rt.Create();
            cam.targetTexture = rt;
            OnOutputChanged?.Invoke(rt);
        }

        Level = camLevel;
        CamVision.Apply(cam, camLevel);
    }

    private void OnDestroy()
    {
        ReleaseTexture();
    }

    private void ReleaseTexture()
    {
        if (rt == null)
            return;

        if (cam != null)
            cam.targetTexture = null;

        rt.Release();
        Destroy(rt);
        rt = null;
    }
}
