using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GridSkillTree
{
    public class SkillNodeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text levelText;

        private SkillNodeData node;
        private SkillTreeRuntime runtime;
        private SkillTreeUIConfig config;
        private SkillTooltip tooltip;
        private RectTransform rectTransform;
        private bool pointerInside;

        public SkillNodeData Node => node;

        public void Init(SkillNodeData node, SkillTreeRuntime runtime, SkillTreeUIConfig config, SkillTooltip tooltip)
        {
            this.node = node;
            this.runtime = runtime;
            this.config = config;
            this.tooltip = tooltip;

            rectTransform = GetComponent<RectTransform>();
            pointerInside = false;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClick);
            }

            Refresh();
        }

        public void Refresh()
        {
            if (node == null || runtime == null)
                return;

            if (iconImage != null)
            {
                iconImage.sprite = node.icon;
                iconImage.color = node.iconColor;
                iconImage.enabled = node.icon != null;
            }

            int level = runtime.GetLevel(node.id);

            if (levelText != null)
                levelText.text = $"{level}/{node.maxLevel}";

            ApplyState(runtime.GetVisualState(node));
        }

        private void ApplyState(SkillNodeVisualState state)
        {
            if (backgroundImage != null && config != null)
            {
                backgroundImage.color = state switch
                {
                    SkillNodeVisualState.Locked => config.lockedColor,
                    SkillNodeVisualState.Available => config.availableColor,
                    SkillNodeVisualState.Maxed => config.maxedColor,
                    _ => Color.white
                };
            }

            if (button != null)
                button.interactable = state == SkillNodeVisualState.Available;
        }

        private void OnClick()
        {
            if (runtime == null || node == null)
                return;

            bool bought = runtime.Buy(node);

            if (!bought)
                return;

            Refresh();

            if (pointerInside && tooltip != null)
                tooltip.Show(node, runtime, rectTransform != null ? rectTransform : GetComponent<RectTransform>());
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;

            if (tooltip != null && node != null && runtime != null)
                tooltip.Show(node, runtime, rectTransform != null ? rectTransform : GetComponent<RectTransform>());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            tooltip?.Hide();
        }

        private void OnDisable()
        {
            pointerInside = false;
            tooltip?.Hide();
        }
    }
}
