using System.Collections.Generic;
using UnityEngine;

// Фамильяр: что снимает, как часто, чем открывается. Видит только то, что видит камера.
[CreateAssetMenu(menuName = "Pumpkin Post/Familiar")]
public class FamiliarDefinition : ScriptableObject
{
    public string displayName = "Чёрный кот";
    public Sprite sprite;

    [Header("Открытие и скорость")]
    [Tooltip("Unlock-эффект из книги, который даёт фамильяра")]
    public string unlockEffect = Effects.FamCat;
    [Tooltip("Эффект книги, ускоряющий именно этого фамильяра (доля, 0.2 = +20%)")]
    public string rateEffect;
    [Tooltip("Секунды отдыха после работы")]
    public float cooldown = 4f;
    [Tooltip("Сколько летит до посылки, прежде чем снять её")]
    public float pounceTime = 0.35f;

    [Header("Цели")]
    public List<ParcelKind> targets = new() { ParcelKind.Trash };
    [Tooltip("Выпускает призраков из посылок")]
    public bool releasesGhosts;

    [Header("Где ловит")]
    [Tooltip("Ловит посылки только в этой части ленты (доля длины), чтобы было видно на мониторе")]
    [Range(0f, 1f)] public float zoneStart = 0.3f;
    [Range(0f, 1f)] public float zoneEnd = 0.92f;
}
