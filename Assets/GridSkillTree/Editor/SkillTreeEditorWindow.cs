using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GridSkillTree.Editor
{
    public class SkillTreeEditorWindow : EditorWindow
    {
        private const float CellSize = 80f;
        private const float NodeSize = 56f;
        private const int GridHalfSize = 12;
        private const float InspectorWidth = 360f;

        private SkillTreeData treeData;
        private SkillNodeData selectedNode;
        private Vector2 gridScroll;
        private Vector2 inspectorScroll;
        private bool isConnecting;
        private SkillNodeData connectionStartNode;

        private readonly Dictionary<Vector2Int, SkillNodeData> nodesByPosition = new();

        [MenuItem("Window/Grid Skill Tree/Editor")]
        public static void Open()
        {
            SkillTreeEditorWindow window = GetWindow<SkillTreeEditorWindow>();
            window.titleContent = new GUIContent("Skill Tree Editor");
            window.minSize = new Vector2(900f, 600f);
            window.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (treeData == null)
            {
                DrawEmptyState();
                return;
            }

            RebuildLookup();

            EditorGUILayout.BeginHorizontal();
            DrawGridPanel();
            DrawInspectorPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            treeData = (SkillTreeData)EditorGUILayout.ObjectField(treeData, typeof(SkillTreeData), false, GUILayout.Width(320f));
            if (EditorGUI.EndChangeCheck())
            {
                selectedNode = null;
                CancelConnection();
            }

            if (GUILayout.Button("Create", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                CreateNewTreeAsset();

            using (new EditorGUI.DisabledScope(treeData == null))
            {
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    ValidateTree();

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    SaveTree();
            }

            GUILayout.FlexibleSpace();

            if (isConnecting && connectionStartNode != null)
            {
                GUILayout.Label("Connecting from: " + connectionStartNode.id);

                if (GUILayout.Button("Cancel", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    CancelConnection();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create Skill Tree", GUILayout.Width(180f), GUILayout.Height(34f)))
                CreateNewTreeAsset();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private void DrawGridPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Rect hintRect = EditorGUILayout.GetControlRect(false, 24f);
            GUI.Box(hintRect, GUIContent.none, EditorStyles.toolbar);
            GUI.Label(new Rect(hintRect.x + 8f, hintRect.y + 4f, hintRect.width, 20f),
                "Double click empty cell: create node. Right click node: menu. Drag is not needed: edit Grid Position in inspector.");

            Rect gridRect = GUILayoutUtility.GetRect(10f, 100000f, 10f, 100000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.Box(gridRect, GUIContent.none);

            Rect contentRect = new Rect(0f, 0f, GridHalfSize * 2 * CellSize + CellSize, GridHalfSize * 2 * CellSize + CellSize);
            gridScroll = GUI.BeginScrollView(gridRect, gridScroll, contentRect);

            DrawGridBackground();
            DrawConnections();
            DrawNodes();
            HandleGridInput();

            GUI.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawGridBackground()
        {
            Handles.BeginGUI();
            Color oldColor = Handles.color;

            for (int x = -GridHalfSize; x <= GridHalfSize; x++)
            {
                Vector2 from = GridToEditorPosition(new Vector2Int(x, -GridHalfSize));
                Vector2 to = GridToEditorPosition(new Vector2Int(x, GridHalfSize)) + new Vector2(0f, CellSize);
                Handles.color = x == 0 ? new Color(1f, 1f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0.08f);
                Handles.DrawLine(from, to);
            }

            for (int y = -GridHalfSize; y <= GridHalfSize; y++)
            {
                Vector2 from = GridToEditorPosition(new Vector2Int(-GridHalfSize, y));
                Vector2 to = GridToEditorPosition(new Vector2Int(GridHalfSize, y)) + new Vector2(CellSize, 0f);
                Handles.color = y == 0 ? new Color(1f, 1f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0.08f);
                Handles.DrawLine(from, to);
            }

            Handles.color = oldColor;
            Handles.EndGUI();
        }

        private void DrawConnections()
        {
            Handles.BeginGUI();
            Color oldColor = Handles.color;
            Handles.color = new Color(0.7f, 0.9f, 1f, 0.85f);

            foreach (SkillNodeData node in treeData.nodes)
            {
                if (node == null || node.previousNodeIds == null)
                    continue;

                Vector2 currentCenter = GridToEditorPosition(node.gridPosition) + Vector2.one * CellSize * 0.5f;

                foreach (string previousId in node.previousNodeIds)
                {
                    SkillNodeData previousNode = treeData.GetNodeById(previousId);
                    if (previousNode == null)
                        continue;

                    Vector2 previousCenter = GridToEditorPosition(previousNode.gridPosition) + Vector2.one * CellSize * 0.5f;
                    Handles.DrawAAPolyLine(4f, previousCenter, currentCenter);
                }
            }

            Handles.color = oldColor;
            Handles.EndGUI();
        }

        private void DrawNodes()
        {
            foreach (SkillNodeData node in treeData.nodes)
            {
                if (node == null)
                    continue;

                Rect nodeRect = GetNodeRect(node);
                bool selected = selectedNode == node;
                bool connectionStart = isConnecting && connectionStartNode == node;

                Color oldColor = GUI.color;
                GUI.color = connectionStart ? Color.cyan : selected ? Color.yellow : Color.white;
                GUI.Box(nodeRect, GUIContent.none, EditorStyles.helpBox);
                GUI.color = oldColor;

                Rect iconRect = new Rect(nodeRect.x + 12f, nodeRect.y + 6f, 32f, 32f);
                if (node.icon != null)
                {
                    Color previousGuiColor = GUI.color;
                    GUI.color = node.iconColor;
                    GUI.DrawTexture(iconRect, node.icon.texture, ScaleMode.ScaleToFit, true);
                    GUI.color = previousGuiColor;
                }
                else
                {
                    GUI.Box(iconRect, "?", EditorStyles.centeredGreyMiniLabel);
                }

                GUI.Label(new Rect(nodeRect.x + 4f, nodeRect.y + 40f, nodeRect.width - 8f, 14f),
                    string.IsNullOrWhiteSpace(node.title) ? node.id : node.title,
                    EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void HandleGridInput()
        {
            Event e = Event.current;
            if (e.type != EventType.MouseDown)
                return;

            Vector2Int cell = EditorPositionToGrid(e.mousePosition);
            SkillNodeData clickedNode = GetNodeAtCell(cell);

            if (e.button == 0)
            {
                if (clickedNode != null)
                {
                    if (isConnecting && connectionStartNode != null && connectionStartNode != clickedNode)
                    {
                        AddConnection(connectionStartNode, clickedNode);
                        e.Use();
                        return;
                    }

                    selectedNode = clickedNode;
                    e.Use();
                    Repaint();
                    return;
                }

                if (e.clickCount == 2)
                {
                    CreateNodeAt(cell);
                    e.Use();
                }
            }
            else if (e.button == 1 && clickedNode != null)
            {
                ShowNodeMenu(clickedNode);
                e.Use();
            }
        }

        private void DrawInspectorPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(InspectorWidth));
            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);

            if (selectedNode == null)
            {
                EditorGUILayout.HelpBox("Select a node to edit it.", MessageType.Info);
            }
            else
            {
                DrawSelectedNodeInspector();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedNodeInspector()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Selected Node", EditorStyles.boldLabel);
            selectedNode.id = EditorGUILayout.TextField("Id", selectedNode.id);
            selectedNode.title = EditorGUILayout.TextField("Title", selectedNode.title);
            selectedNode.description = EditorGUILayout.TextField("Description", selectedNode.description, GUILayout.Height(70f));
            selectedNode.gridPosition = EditorGUILayout.Vector2IntField("Grid Position", selectedNode.gridPosition);
            selectedNode.icon = (Sprite)EditorGUILayout.ObjectField("Icon", selectedNode.icon, typeof(Sprite), false);
            selectedNode.iconColor = EditorGUILayout.ColorField("Icon Color", selectedNode.iconColor);
            selectedNode.maxLevel = Mathf.Max(1, EditorGUILayout.IntField("Max Level", selectedNode.maxLevel));

            GUILayout.Space(8f);
            DrawStringList("Previous Nodes", selectedNode.previousNodeIds);
            GUILayout.Space(8f);
            DrawCostsList();
            GUILayout.Space(8f);
            DrawEffectsList();

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(treeData);
        }

        private void DrawStringList(string label, List<string> values)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            if (values == null)
            {
                EditorGUILayout.HelpBox("List is not initialized.", MessageType.Warning);
                return;
            }

            int removeIndex = -1;

            for (int i = 0; i < values.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                values[i] = EditorGUILayout.TextField(values[i]);

                if (GUILayout.Button("-", GUILayout.Width(24f)))
                    removeIndex = i;

                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
                values.RemoveAt(removeIndex);

            if (GUILayout.Button("Add Previous Node"))
                values.Add(string.Empty);
        }

        private void DrawCostsList()
        {
            EditorGUILayout.LabelField("Costs", EditorStyles.boldLabel);

            if (selectedNode.costs == null)
                selectedNode.costs = new List<SkillCost>();

            int removeIndex = -1;

            for (int i = 0; i < selectedNode.costs.Count; i++)
            {
                SkillCost cost = selectedNode.costs[i];

                if (cost == null)
                {
                    cost = new SkillCost();
                    selectedNode.costs[i] = cost;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Cost " + (i + 1), EditorStyles.boldLabel);

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    removeIndex = i;

                EditorGUILayout.EndHorizontal();

                cost.currency = (CurrencyDefinition)EditorGUILayout.ObjectField("Currency", cost.currency, typeof(CurrencyDefinition), false);
                cost.baseAmount = Mathf.Max(0, EditorGUILayout.IntField("Base Amount", cost.baseAmount));
                cost.formula = (CostFormulaType)EditorGUILayout.EnumPopup("Formula", cost.formula);
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
                selectedNode.costs.RemoveAt(removeIndex);

            if (GUILayout.Button("Add Cost"))
                selectedNode.costs.Add(new SkillCost());
        }

        private void DrawEffectsList()
        {
            EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);

            if (selectedNode.effects == null)
                selectedNode.effects = new List<SkillEffect>();

            int removeIndex = -1;

            for (int i = 0; i < selectedNode.effects.Count; i++)
            {
                SkillEffect effect = selectedNode.effects[i];

                if (effect == null)
                {
                    effect = new SkillEffect();
                    selectedNode.effects[i] = effect;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Effect " + (i + 1), EditorStyles.boldLabel);

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    removeIndex = i;

                EditorGUILayout.EndHorizontal();

                effect.id = EditorGUILayout.TextField("Id", effect.id);
                effect.valueMode = (SkillValueMode)EditorGUILayout.EnumPopup("Value Mode", effect.valueMode);
                effect.baseValue = EditorGUILayout.FloatField("Base Value", effect.baseValue);
                effect.growthFormula = (GrowthFormulaType)EditorGUILayout.EnumPopup("Growth", effect.growthFormula);
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
                selectedNode.effects.RemoveAt(removeIndex);

            if (GUILayout.Button("Add Effect"))
                selectedNode.effects.Add(new SkillEffect());
        }

        private void CreateNodeAt(Vector2Int cell)
        {
            Undo.RecordObject(treeData, "Create Skill Node");

            string id = MakeUniqueId("node");
            SkillNodeData node = new()
            {
                id = id,
                title = id,
                gridPosition = cell,
                maxLevel = 1
            };

            treeData.nodes.Add(node);
            selectedNode = node;
            EditorUtility.SetDirty(treeData);
        }

        private void ShowNodeMenu(SkillNodeData node)
        {
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Select"), false, () => selectedNode = node);
            menu.AddItem(new GUIContent("Start Connection"), false, () => StartConnection(node));
            menu.AddItem(new GUIContent("Delete"), false, () => DeleteNode(node));
            menu.ShowAsContext();
        }

        private void StartConnection(SkillNodeData node)
        {
            isConnecting = true;
            connectionStartNode = node;
        }

        private void CancelConnection()
        {
            isConnecting = false;
            connectionStartNode = null;
        }

        private void AddConnection(SkillNodeData from, SkillNodeData to)
        {
            Undo.RecordObject(treeData, "Add Skill Connection");

            if (to.previousNodeIds == null)
                to.previousNodeIds = new List<string>();

            if (!to.previousNodeIds.Contains(from.id))
                to.previousNodeIds.Add(from.id);

            CancelConnection();
            EditorUtility.SetDirty(treeData);
        }

        private void DeleteNode(SkillNodeData node)
        {
            Undo.RecordObject(treeData, "Delete Skill Node");
            treeData.nodes.Remove(node);

            foreach (SkillNodeData other in treeData.nodes)
                other.previousNodeIds?.Remove(node.id);

            if (selectedNode == node)
                selectedNode = null;

            EditorUtility.SetDirty(treeData);
        }

        private void RebuildLookup()
        {
            nodesByPosition.Clear();

            foreach (SkillNodeData node in treeData.nodes)
            {
                if (node != null)
                    nodesByPosition[node.gridPosition] = node;
            }
        }

        private SkillNodeData GetNodeAtCell(Vector2Int cell)
        {
            nodesByPosition.TryGetValue(cell, out SkillNodeData node);
            return node;
        }

        private Rect GetNodeRect(SkillNodeData node)
        {
            Vector2 position = GridToEditorPosition(node.gridPosition);
            float offset = (CellSize - NodeSize) * 0.5f;
            return new Rect(position.x + offset, position.y + offset, NodeSize, NodeSize);
        }

        private Vector2 GridToEditorPosition(Vector2Int gridPosition)
        {
            float x = (gridPosition.x + GridHalfSize) * CellSize;
            float y = (GridHalfSize - gridPosition.y) * CellSize;
            return new Vector2(x, y);
        }

        private Vector2Int EditorPositionToGrid(Vector2 editorPosition)
        {
            int x = Mathf.FloorToInt(editorPosition.x / CellSize) - GridHalfSize;
            int y = GridHalfSize - Mathf.FloorToInt(editorPosition.y / CellSize);
            return new Vector2Int(x, y);
        }

        private string MakeUniqueId(string prefix)
        {
            int index = treeData.nodes.Count + 1;
            string id = prefix + "_" + index;

            while (treeData.ContainsNode(id))
            {
                index++;
                id = prefix + "_" + index;
            }

            return id;
        }

        private void CreateNewTreeAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Skill Tree", "NewSkillTree", "asset", "Choose location");
            if (string.IsNullOrWhiteSpace(path))
                return;

            SkillTreeData asset = CreateInstance<SkillTreeData>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            treeData = asset;
            selectedNode = null;
        }

        private void ValidateTree()
        {
            SkillTreeValidationResult result = SkillTreeValidator.Validate(treeData);

            if (result.IsValid)
                Debug.Log(result.GetReport(), treeData);
            else
                Debug.LogError(result.GetReport(), treeData);
        }

        private void SaveTree()
        {
            EditorUtility.SetDirty(treeData);
            AssetDatabase.SaveAssets();
        }
    }
}
