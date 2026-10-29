using System.Collections.Generic;
using UnityEngine;

// Полка фамильяров под мониторами. Показывает, кто куплен и кто отдыхает.
public class FamiliarShelfView : MonoBehaviour
{
    [SerializeField] private FamiliarSlotView slotPrefab;
    [SerializeField] private Transform slotsRoot;

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

        foreach (Familiar f in G.familiars.Active)
        {
            FamiliarSlotView slot = Instantiate(slotPrefab, slotsRoot);
            slot.Bind(f);
            slots.Add(slot);
        }
    }

    private void HandlePounce(Familiar familiar, Parcel target)
    {
        foreach (FamiliarSlotView slot in slots)
            if (slot.Familiar == familiar)
                slot.PlayPounce();
    }
}
