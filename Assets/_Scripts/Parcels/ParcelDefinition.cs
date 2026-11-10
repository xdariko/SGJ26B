using UnityEngine;

// Предмет на ленте (коробка, свиток, камень...). Читается по силуэту на любой камере.
[CreateAssetMenu(menuName = "Pumpkin Post/Parcel Shape")]
public class ParcelDefinition : ScriptableObject
{
    public string displayName = "Коробка";
    public Sprite sprite;
    [Tooltip("Мусор: камень, гнилая тыква, кость")]
    public bool isTrash;
    [Tooltip("Монеты за доставку (для обычных предметов)")]
    public int value = 1;
    [Tooltip("Чей это предмет (гробик — вампирам). Выпадает только в посылках этому адресату. Any = кому угодно")]
    public Addressee addressee = Addressee.Any;

    [Header("Когда появляется")]
    [Min(1)] public int minNight = 1;
    [Tooltip("id эффекта из книги, который открывает предмет. Пусто = открыт сразу")]
    public string requiredUnlock;

    [Header("Спавн и вид")]
    [Min(0f)] public float weight = 1f;
    [Tooltip("Множитель размера на ленте")]
    public float scale = 1f;
    [Tooltip("Какая точка спрайта по высоте едет по середине ленты: 0 = нижний край, 0.5 = центр")]
    [Range(0f, 1f)] public float beltAnchor = 0.5f;
    [Tooltip("Сдвиг печати от центра спрайта, в юнитах")]
    public Vector2 stampOffset = new(0.15f, -0.05f);

    public bool IsAvailable(int night, GameStats stats)
    {
        if (night < minNight)
            return false;

        return string.IsNullOrEmpty(requiredUnlock) || stats.HasUnlock(requiredUnlock);
    }
}
