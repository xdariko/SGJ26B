using System;
using System.Collections.Generic;
using GridSkillTree;
using UnityEngine;
using UnityEngine.UI;

// Выделение важных узлов книги: открытие новых посылок, фамильяров и лент.
// За узлом рисованный ромб (с квадратом узла выходит звезда) и мягкое свечение.
// Группа узла определяется по Unlock-эффектам узла. Доступный к покупке узел медленно пульсирует.
// Висит на нашей копии префаба узла, саму тулзу не трогает.
[RequireComponent(typeof(SkillNodeButton))]
public class SkillNodeHighlight : MonoBehaviour
{
    [Serializable]
    public class Group
    {
        public string name;
        [Tooltip("id эффектов: узел с любым из них получает выделение этой группы")]
        public List<string> effectIds = new();
        public Color color = Color.white;
        [Tooltip("Размер ромба и свечения относительно обычного (финал крупнее)")]
        public float scale = 1f;
        [Tooltip("Множитель пульса и яркости свечения")]
        public float emphasis = 1f;
    }

    [SerializeField] private Image badge;
    [SerializeField] private Image glow;
    [SerializeField] private List<Group> groups = new()
    {
        new Group
        {
            name = "Финал",
            effectIds = new() { Effects.FinalOrder },
            color = new Color(0.96f, 0.78f, 0.36f),
            scale = 1.35f,
            emphasis = 1.8f
        },
        new Group
        {
            name = "Новые посылки",
            effectIds = new() { Effects.OpenCurse, Effects.OpenMimic, Effects.OpenGhost },
            color = new Color(0.78f, 0.46f, 0.55f)
        },
        new Group
        {
            name = "Фамильяры",
            effectIds = new() { Effects.FamCat, Effects.FamRaven, Effects.FamOwl, Effects.FamBat },
            color = new Color(0.86f, 0.68f, 0.42f)
        },
        new Group
        {
            name = "Ленты",
            effectIds = new() { Effects.Line2, Effects.Line3, Effects.Line4 },
            color = new Color(0.50f, 0.72f, 0.74f)
        }
    };

    [Header("Яркость по состоянию узла")]
    [Range(0f, 1f)] [SerializeField] private float lockedAlpha = 0.35f;
    [Range(0f, 1f)] [SerializeField] private float boughtAlpha = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float glowAlpha = 0.55f;

    [Header("Пульс, пока узел можно купить")]
    [SerializeField] private float pulsePeriod = 2.2f;
    [SerializeField] private float pulseScale = 0.06f;
    [SerializeField] private float spinDegrees = 4f;

    private SkillNodeButton button;
    private SkillNodeData shown;
    private Group group;
    private float phase;

    private void Awake()
    {
        button = GetComponent<SkillNodeButton>();
        phase = UnityEngine.Random.value * 10f;
        SetVisible(false);
    }

    private void LateUpdate()
    {
        // Init у кнопки вызывается после Instantiate: подхватываем узел, как только он появился
        if (button.Node != shown)
        {
            shown = button.Node;
            group = FindGroup(shown);
            SetVisible(group != null);
        }

        if (group == null)
            return;

        SkillNodeVisualState state = G.tree != null ? G.tree.GetVisualState(shown) : SkillNodeVisualState.Available;
        bool available = state == SkillNodeVisualState.Available;
        float alpha = state switch
        {
            SkillNodeVisualState.Locked => group.emphasis > 1f ? 1f : lockedAlpha,
            SkillNodeVisualState.Maxed => boughtAlpha,
            _ => 1f
        };

        // Пульс на unscaled time: книга открыта на паузе.
        // Финал пульсирует и тогда, когда на него ещё не хватает денег: это цель игры
        bool pulsing = available || (group.emphasis > 1f && state == SkillNodeVisualState.Locked);
        float wave = pulsing ? 0.5f + 0.5f * Mathf.Sin((Time.unscaledTime + phase) * Mathf.PI * 2f / pulsePeriod) : 0f;

        Color c = group.color;
        c.a = alpha;
        badge.color = c;
        badge.rectTransform.localScale = Vector3.one * group.scale * (1f + pulseScale * group.emphasis * wave);
        badge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, spinDegrees * Mathf.Sin((Time.unscaledTime + phase) * 0.7f));

        if (glow != null)
        {
            Color g = group.color;
            g.a = pulsing ? Mathf.Clamp01(glowAlpha * group.emphasis * (0.6f + 0.4f * wave)) : 0f;
            glow.color = g;
            glow.rectTransform.localScale = Vector3.one * group.scale * (1f + pulseScale * group.emphasis * wave);
        }
    }

    private Group FindGroup(SkillNodeData node)
    {
        if (node == null)
            return null;

        foreach (SkillEffect e in node.effects)
            if (e.valueMode == SkillValueMode.Unlock)
                foreach (Group g in groups)
                    if (g.effectIds.Contains(e.id))
                        return g;
        return null;
    }

    private void SetVisible(bool on)
    {
        if (badge != null)
            badge.enabled = on;
        if (glow != null)
            glow.enabled = on;
    }
}
