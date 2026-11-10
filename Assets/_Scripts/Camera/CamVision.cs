using UnityEngine;

// Признаки посылок лежат на отдельных слоях. Марки видны только с «Ровной резьбой» (уровень 1),
// свечение проклятий, мимиков и призраков камера видит всегда (насколько ярко — решает ParcelVisual).
public static class CamVision
{
    public const string ParcelsLayer = "Parcels";
    public const string StampLayer = "CamStamp";
    public const string CurseLayer = "CamCurse";
    public const string GhostLayer = "CamGhost";

    public static int ParcelsMask => LayerMask.GetMask(ParcelsLayer);

    public static int GetCullingMask(int camLevel, int baseMask)
    {
        int mask = baseMask | LayerMask.GetMask(ParcelsLayer, CurseLayer, GhostLayer);

        if (camLevel >= 1) mask |= LayerMask.GetMask(StampLayer);

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
