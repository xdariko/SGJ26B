using System.Collections.Generic;
using UnityEngine;

// Полка с подушками под мониторами. У каждого фамильяра своя подушка; пока не куплен — подушка пустая.
public class FamiliarShelfView : MonoBehaviour
{
    [SerializeField] private FamiliarSlotView slotPrefab;
    [Tooltip("Точки посадки на подушках, слева направо")]
    [SerializeField] private RectTransform[] cushions;
    [Tooltip("Кто на какой подушке сидит (по индексу). Кого нет в списке — на первую свободную")]
    [SerializeField] private FamiliarDefinition[] seating;

    private readonly List<FamiliarSlotView> slots = new();

    private void Start()
    {
        G.familiars.OnRosterChanged += Rebuild;
        G.familiars.OnPounce += HandlePounce;
        Rebuild();
    }

    private void OnDestroy()
    {
        if (G.familiars == null)
            return;
        G.familiars.OnRosterChanged -= Rebuild;
        G.familiars.OnPounce -= HandlePounce;
    }

    private void Update()
    {
        foreach (FamiliarSlotView slot in slots)
            slot.Refresh();
    }

    private void Rebuild()
    {
        foreach (FamiliarSlotView slot in slots)
            Destroy(slot.gameObject);
        slots.Clear();

        bool[] taken = new bool[cushions.Length];
        List<Familiar> unseated = new();

        foreach (Familiar f in G.familiars.Active)
        {
            int seat = System.Array.IndexOf(seating, f.def);
            if (seat >= 0 && seat < cushions.Length && !taken[seat])
            {
                taken[seat] = true;
                Spawn(f, cushions[seat]);
            }
            else
                unseated.Add(f);
        }

        foreach (Familiar f in unseated)
        {
            int seat = System.Array.IndexOf(taken, false);
            if (seat < 0)
                break;
            taken[seat] = true;
            Spawn(f, cushions[seat]);
        }
    }

    private void Spawn(Familiar f, RectTransform cushion)
    {
        FamiliarSlotView slot = Instantiate(slotPrefab, cushion);
        RectTransform rt = (RectTransform)slot.transform;
        rt.anchoredPosition = Vector2.zero;
        slot.Bind(f);
        slots.Add(slot);
    }

    private void HandlePounce(Familiar familiar, Parcel target)
    {
        foreach (FamiliarSlotView slot in slots)
            if (slot.Familiar == familiar)
                slot.PlayPounce();
    }
}
