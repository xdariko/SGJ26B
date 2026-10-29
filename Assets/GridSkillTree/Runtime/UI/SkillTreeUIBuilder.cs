using System.Collections.Generic;
using UnityEngine;

namespace GridSkillTree
{
    public class SkillTreeUIBuilder : MonoBehaviour
    {
        [SerializeField] private SkillTreeRuntime runtime;
        [SerializeField] private SkillTreeUIConfig config;
        [SerializeField] private RectTransform nodesRoot;
        [SerializeField] private RectTransform linesRoot;
        [SerializeField] private SkillTooltip tooltip;

        private readonly List<SkillNodeButton> spawnedButtons = new();
        private readonly List<ConnectionView> spawnedLines = new();
        private readonly Dictionary<string, RectTransform> nodeRectsById = new();
        private readonly Dictionary<string, SkillNodeData> nodesById = new();

        private class ConnectionView
        {
            public SkillTreeConnectionLine line;
            public string fromNodeId;
            public string toNodeId;
        }

        private void Start()
        {
            Build();

            if (runtime != null)
                runtime.OnTreeChanged += Refresh;

            if (runtime != null && runtime.Wallet != null)
                runtime.Wallet.OnCurrencyChanged += HandleCurrencyChanged;
        }

        private void OnDestroy()
        {
            if (runtime != null)
                runtime.OnTreeChanged -= Refresh;

            if (runtime != null && runtime.Wallet != null)
                runtime.Wallet.OnCurrencyChanged -= HandleCurrencyChanged;
        }

        private void HandleCurrencyChanged(CurrencyDefinition currency, int amount)
        {
            Refresh();
        }

        public void Build()
        {
            Clear();

            if (runtime == null || runtime.TreeData == null || config == null || config.nodeButtonPrefab == null || nodesRoot == null)
            {
                Debug.LogError("SkillTreeUIBuilder is missing references.");
                return;
            }

            SkillTreeValidationResult validation = SkillTreeValidator.Validate(runtime.TreeData);

            if (!validation.IsValid)
            {
                Debug.LogError(validation.GetReport());
                return;
            }

            if (validation.warnings.Count > 0)
                Debug.LogWarning(validation.GetReport());

            CacheNodes();
            BuildNodes();
            BuildConnections();
            Refresh();
        }

        public void Refresh()
        {
            foreach (SkillNodeButton button in spawnedButtons)
            {
                if (button == null || button.Node == null)
                    continue;

                bool visible = runtime.ShouldShowNode(button.Node);
                button.gameObject.SetActive(visible);

                if (visible)
                    button.Refresh();
            }

            foreach (ConnectionView connection in spawnedLines)
            {
                if (connection == null || connection.line == null)
                    continue;

                bool visible = IsNodeVisible(connection.fromNodeId) && IsNodeVisible(connection.toNodeId);
                connection.line.gameObject.SetActive(visible);
            }

            if (tooltip != null)
                tooltip.RefreshCurrent();
        }

        private void CacheNodes()
        {
            nodesById.Clear();

            foreach (SkillNodeData node in runtime.TreeData.nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.id))
                    continue;

                nodesById[node.id] = node;
            }
        }

        private void BuildNodes()
        {
            foreach (SkillNodeData node in runtime.TreeData.nodes)
            {
                if (node == null)
                    continue;

                SkillNodeButton button = Instantiate(config.nodeButtonPrefab, nodesRoot);
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.anchoredPosition = GridToUIPosition(node.gridPosition);

                button.Init(node, runtime, config, tooltip);

                spawnedButtons.Add(button);
                nodeRectsById[node.id] = rect;
            }
        }

        private void BuildConnections()
        {
            if (config.connectionLinePrefab == null || linesRoot == null)
                return;

            foreach (SkillNodeData node in runtime.TreeData.nodes)
            {
                if (node == null || node.previousNodeIds == null)
                    continue;

                if (!nodeRectsById.TryGetValue(node.id, out RectTransform currentRect))
                    continue;

                foreach (string previousNodeId in node.previousNodeIds)
                {
                    if (!nodeRectsById.TryGetValue(previousNodeId, out RectTransform previousRect))
                        continue;

                    SkillTreeConnectionLine line = Instantiate(config.connectionLinePrefab, linesRoot);
                    line.SetPoints(previousRect.anchoredPosition, currentRect.anchoredPosition, config.connectionThickness);

                    spawnedLines.Add(new ConnectionView
                    {
                        line = line,
                        fromNodeId = previousNodeId,
                        toNodeId = node.id
                    });
                }
            }
        }

        private bool IsNodeVisible(string nodeId)
        {
            return !string.IsNullOrWhiteSpace(nodeId)
                && nodesById.TryGetValue(nodeId, out SkillNodeData node)
                && runtime.ShouldShowNode(node);
        }

        private Vector2 GridToUIPosition(Vector2Int gridPosition)
        {
            float x = gridPosition.x * (config.cellSize.x + config.spacing.x);
            float y = gridPosition.y * (config.cellSize.y + config.spacing.y);
            return new Vector2(x, y);
        }

        private void Clear()
        {
            ClearChildren(nodesRoot);
            ClearChildren(linesRoot);

            spawnedButtons.Clear();
            spawnedLines.Clear();
            nodeRectsById.Clear();
            nodesById.Clear();
        }

        private void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;

                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }
    }
}
