using UnityEngine;

// Скрытые признаки посылок лежат на отдельных слоях. Уровень камеры = какие слои она рендерит.
public static class CamVision
{
    public const string ParcelsLayer = "Parcels";
    public const string StampLayer = "CamStamp";
    public const string CurseLayer = "CamCurse";
    public const string GhostLayer = "CamGhost";

    public static int ParcelsMask => LayerMask.GetMask(ParcelsLayer);

    public static int GetCullingMask(int camLevel, int baseMask)
    {
        int mask = baseMask | LayerMask.GetMask(ParcelsLayer);

        if (camLevel >= 1) mask |= LayerMask.GetMask(StampLayer);
        if (camLevel >= 2) mask |= LayerMask.GetMask(CurseLayer);
        if (camLevel >= 3) mask |= LayerMask.GetMask(GhostLayer);

        return mask;
    }

    public static void Apply(Camera cam, int camLevel)
    {
        if (cam == null)
            return;

        int hidden = LayerMask.GetMask(StampLayer, CurseLayer, GhostLayer);
        cam.cullingMask = GetCullingMask(camLevel, cam.cullingMask & ~hidden);
    }
}
