using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GridSkillTree
{
    public class SkillTooltip : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text effectsText;
        [SerializeField] private TMP_Text statusText;
        [Tooltip("Формат строки уровня: {0} = текущий, {1} = максимум")]
        [SerializeField] private string levelFormat = "Level: {0}/{1}";

        [Header("Currency Cost View")]
        [SerializeField] private RectTransform costRowsRoot;
        [SerializeField] private CurrencyAmountView costRowPrefab;

        [Header("Position")]
        [SerializeField] private float offsetFromNode = 16f;
        [SerializeField] private float screenPadding = 16f;
        [SerializeField] private bool followTarget = true;

        [Header("Draw On Top")]
        [SerializeField] private bool moveToRootCanvas = true;
        [SerializeField] private int sortingOrder = 32767;

        private readonly List<GameObject> generatedCostRows = new();

        private RectTransform tooltipRect;
        private RectTransform currentTarget;
        private SkillNodeData currentNode;
        private SkillTreeRuntime currentRuntime;

        private Canvas rootCanvas;
        private Canvas tooltipCanvas;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (root == null)
                root = gameObject;

            tooltipRect = root.GetComponent<RectTransform>();
            SetupTopLayer();
            DisableTooltipRaycasts();

            Hide();
        }

        private void LateUpdate()
        {
            if (!followTarget || root == null || !root.activeSelf || currentTarget == null)
                return;

            UpdatePosition(currentTarget);
        }

        public void Show(SkillNodeData node, SkillTreeRuntime runtime, RectTransform target)
        {
            if (node == null || runtime == null || target == null || root == null)
                return;

            currentNode = node;
            currentRuntime = runtime;
            currentTarget = target;

            SetupTopLayer();
            DisableTooltipRaycasts();

            root.SetActive(true);
            root.transform.SetAsLastSibling();

            RefreshCurrent();
            UpdatePosition(target);
        }

        public void RefreshCurrent()
        {
            if (currentNode == null || currentRuntime == null || root == null || !root.activeSelf)
                return;

            int level = currentRuntime.GetLevel(currentNode.id);
            bool maxed = level >= currentNode.maxLevel;
            int nextLevel = Mathf.Clamp(level + 1, 1, currentNode.maxLevel);

            if (titleText != null)
                titleText.text = string.IsNullOrWhiteSpace(currentNode.title) ? currentNode.id : currentNode.title;

            if (descriptionText != null)
                descriptionText.text = currentNode.description;

            if (levelText != null)
                levelText.text = string.Format(levelFormat, level, currentNode.maxLevel);

            RefreshCostView(currentNode, currentRuntime, maxed);

            if (effectsText != null)
                effectsText.text = BuildEffectsText(currentNode, nextLevel);

            if (statusText != null)
                statusText.text = "Status: " + currentRuntime.GetStatusText(currentNode);
        }

        public void Hide()
        {
            currentNode = null;
            currentRuntime = null;
            currentTarget = null;

            ClearGeneratedCostRows();

            if (root != null)
                root.SetActive(false);
        }

        private void RefreshCostView(SkillNodeData node, SkillTreeRuntime runtime, bool maxed)
        {
            ClearGeneratedCostRows();

            if (costRowsRoot == null || costRowPrefab == null)
                return;

            if (maxed)
                return;

            IReadOnlyList<CurrencyAmount> costs = runtime.GetCosts(node);
            if (costs == null || costs.Count == 0)
                return;

            for (int i = 0; i < costs.Count; i++)
            {
                CurrencyAmount cost = costs[i];

                if (cost == null || cost.currency == null)
                    continue;

                CurrencyAmountView row = Instantiate(costRowPrefab, costRowsRoot);
                row.SetData(cost.currency, cost.amount);
                generatedCostRows.Add(row.gameObject);
            }
        }

        private string BuildEffectsText(SkillNodeData node, int nextLevel)
        {
            if (node.effects == null || node.effects.Count == 0)
                return "Effects: none";

            StringBuilder builder = new("Effects:\n");

            foreach (SkillEffect effect in node.effects)
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.id))
                    continue;

                builder.Append("- ").Append(effect.id);

                if (effect.valueMode == SkillValueMode.Unlock)
                    builder.Append(" unlock");
                else
                    builder.Append(": ").Append(effect.GetValue(nextLevel).ToString("0.##"));

                builder.Append('\n');
            }

            return builder.ToString().TrimEnd();
        }

        private void ClearGeneratedCostRows()
        {
            for (int i = generatedCostRows.Count - 1; i >= 0; i--)
            {
                GameObject row = generatedCostRows[i];

                if (row != null)
                    Destroy(row);
            }

            generatedCostRows.Clear();
        }

        private void SetupTopLayer()
        {
            if (root == null)
                return;

            rootCanvas = FindRootCanvasOutsideTooltip();

            if (rootCanvas == null)
                return;

            if (moveToRootCanvas && root.transform.parent != rootCanvas.transform)
                root.transform.SetParent(rootCanvas.transform, false);

            root.transform.SetAsLastSibling();

            tooltipCanvas = root.GetComponent<Canvas>();
            if (tooltipCanvas == null)
                tooltipCanvas = root.AddComponent<Canvas>();

            tooltipCanvas.overrideSorting = true;
            tooltipCanvas.sortingOrder = sortingOrder;
            tooltipCanvas.sortingLayerID = rootCanvas.sortingLayerID;
        }

        private Canvas FindRootCanvasOutsideTooltip()
        {
            Transform searchFrom = root != null && root.transform.parent != null
                ? root.transform.parent
                : transform.parent;

            if (searchFrom == null)
                searchFrom = transform;

            Canvas[] canvases = searchFrom.GetComponentsInParent<Canvas>(true);

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];

                if (canvas != null && canvas != tooltipCanvas && canvas.isRootCanvas)
                    return canvas;
            }

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];

                if (canvas != null && canvas != tooltipCanvas)
                    return canvas;
            }

            return null;
        }

        private void DisableTooltipRaycasts()
        {
            if (root == null)
                return;

            canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = root.AddComponent<CanvasGroup>();

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                if (graphic != null)
                    graphic.raycastTarget = false;
            }
        }

        private void UpdatePosition(RectTransform target)
        {
            if (tooltipRect == null || target == null)
                return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

            if (rootCanvas == null)
                rootCanvas = FindRootCanvasOutsideTooltip();

            if (rootCanvas == null)
                return;

            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            Camera targetCamera = GetCanvasCamera(targetCanvas);
            Camera rootCamera = GetCanvasCamera(rootCanvas);

            Vector3[] worldCorners = new Vector3[4];
            target.GetWorldCorners(worldCorners);

            Vector2 bottomLeft = WorldToCanvasLocal(canvasRect, worldCorners[0], targetCamera, rootCamera);
            Vector2 topRight = WorldToCanvasLocal(canvasRect, worldCorners[2], targetCamera, rootCamera);

            float nodeLeft = Mathf.Min(bottomLeft.x, topRight.x);
            float nodeRight = Mathf.Max(bottomLeft.x, topRight.x);
            float nodeBottom = Mathf.Min(bottomLeft.y, topRight.y);
            float nodeTop = Mathf.Max(bottomLeft.y, topRight.y);
            float nodeCenterX = (nodeLeft + nodeRight) * 0.5f;
            float nodeCenterY = (nodeBottom + nodeTop) * 0.5f;

            Vector2 tooltipSize = GetTooltipSize();
            Rect safeRect = GetSafeCanvasRect(canvasRect);

            Vector2 position = ChoosePosition(
                nodeLeft,
                nodeRight,
                nodeBottom,
                nodeTop,
                nodeCenterX,
                nodeCenterY,
                tooltipSize,
                safeRect
            );

            tooltipRect.pivot = new Vector2(0.5f, 0.5f);
            tooltipRect.anchoredPosition = ClampCenterToSafeRect(position, tooltipSize, safeRect);
        }

        private Vector2 ChoosePosition(
            float nodeLeft,
            float nodeRight,
            float nodeBottom,
            float nodeTop,
            float nodeCenterX,
            float nodeCenterY,
            Vector2 tooltipSize,
            Rect safeRect)
        {
            float halfWidth = tooltipSize.x * 0.5f;
            float halfHeight = tooltipSize.y * 0.5f;

            float aboveY = nodeTop + offsetFromNode + halfHeight;
            float belowY = nodeBottom - offsetFromNode - halfHeight;
            float rightX = nodeRight + offsetFromNode + halfWidth;
            float leftX = nodeLeft - offsetFromNode - halfWidth;

            bool fitsAbove = aboveY + halfHeight <= safeRect.yMax;
            bool fitsBelow = belowY - halfHeight >= safeRect.yMin;
            bool fitsRight = rightX + halfWidth <= safeRect.xMax;
            bool fitsLeft = leftX - halfWidth >= safeRect.xMin;

            if (fitsAbove)
                return new Vector2(nodeCenterX, aboveY);

            if (fitsBelow)
                return new Vector2(nodeCenterX, belowY);

            if (fitsLeft)
                return new Vector2(leftX, nodeCenterY);

            if (fitsRight)
                return new Vector2(rightX, nodeCenterY);

            Vector2 fallback = new(nodeCenterX, aboveY);

            float overflowAbove = Mathf.Max(0f, aboveY + halfHeight - safeRect.yMax);
            float overflowBelow = Mathf.Max(0f, safeRect.yMin - (belowY - halfHeight));

            if (overflowBelow < overflowAbove)
                fallback = new Vector2(nodeCenterX, belowY);

            return fallback;
        }

        private Vector2 GetTooltipSize()
        {
            float width = tooltipRect.rect.width;
            float height = tooltipRect.rect.height;

            if (width <= 0f || height <= 0f)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
                width = tooltipRect.rect.width;
                height = tooltipRect.rect.height;
            }

            if (width <= 0f)
                width = tooltipRect.sizeDelta.x;

            if (height <= 0f)
                height = tooltipRect.sizeDelta.y;

            return new Vector2(Mathf.Max(width, 1f), Mathf.Max(height, 1f));
        }

        private Vector2 WorldToCanvasLocal(RectTransform canvasRect, Vector3 worldPosition, Camera sourceCamera, Camera targetCamera)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldPosition);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                targetCamera,
                out Vector2 localPoint
            );

            return localPoint;
        }

        private Rect GetSafeCanvasRect(RectTransform canvasRect)
        {
            Rect rect = canvasRect.rect;

            rect.xMin += screenPadding;
            rect.xMax -= screenPadding;
            rect.yMin += screenPadding;
            rect.yMax -= screenPadding;

            return rect;
        }

        private Vector2 ClampCenterToSafeRect(Vector2 center, Vector2 size, Rect safeRect)
        {
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;

            float minX = safeRect.xMin + halfWidth;
            float maxX = safeRect.xMax - halfWidth;
            float minY = safeRect.yMin + halfHeight;
            float maxY = safeRect.yMax - halfHeight;

            if (minX <= maxX)
                center.x = Mathf.Clamp(center.x, minX, maxX);

            if (minY <= maxY)
                center.y = Mathf.Clamp(center.y, minY, maxY);

            return center;
        }

        private Camera GetCanvasCamera(Canvas canvas)
        {
            if (canvas == null)
                return null;

            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
    }
}