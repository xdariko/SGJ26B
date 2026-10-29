using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Стена мониторов: открытые линии + один закрытый слот-витрина. Сетка 1 → 2 → 2×2.
public class MonitorWallView : MonoBehaviour
{
    [SerializeField] private List<MonitorView> monitors = new();
    [SerializeField] private GridLayoutGroup grid;
    [Tooltip("Ширина / высота одного монитора вместе с рамкой")]
    [SerializeField] private float monitorAspect = 16f / 10f;
    [SerializeField] private string lockedHint = "Новая линия\nв Ведьминой книге";

    private void Start()
    {
        G.shift.OnShiftStarted += Rebuild;
        if (G.shift.Current == ShiftController.State.Running)
            Rebuild();
    }

    private void OnDestroy()
    {
        if (G.shift != null)
            G.shift.OnShiftStarted -= Rebuild;
    }

    private void Rebuild()
    {
        IReadOnlyList<Belt> belts = G.shift.Belts;
        int lines = Mathf.Min(G.stats.lines, belts.Count, monitors.Count);
        int shown = Mathf.Min(monitors.Count, lines + 1);

        for (int i = 0; i < monitors.Count; i++)
        {
            if (i < lines)
                monitors[i].Bind(belts[i], i);
            else if (i < shown)
                monitors[i].ShowLocked(i, lockedHint);
            else
                monitors[i].Hide();
        }

        LayoutGrid(shown);
    }

    private void LayoutGrid(int count)
    {
        int cols = count <= 2 ? Mathf.Max(1, count) : 2;
        int rows = Mathf.CeilToInt(count / (float)cols);

        Rect area = ((RectTransform)grid.transform).rect;
        float w = (area.width - grid.padding.horizontal - grid.spacing.x * (cols - 1)) / cols;
        float h = (area.height - grid.padding.vertical - grid.spacing.y * (rows - 1)) / rows;

        if (w / h > monitorAspect) w = h * monitorAspect;
        else h = w / monitorAspect;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.cellSize = new Vector2(w, h);
    }
}
