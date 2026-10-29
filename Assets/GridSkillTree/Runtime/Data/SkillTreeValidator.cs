using System.Collections.Generic;
using UnityEngine;

namespace GridSkillTree
{
    public static class SkillTreeValidator
    {
        public static SkillTreeValidationResult Validate(SkillTreeData treeData)
        {
            SkillTreeValidationResult result = new();

            if (treeData == null)
            {
                result.AddError("Tree Data is null.");
                return result;
            }

            ValidateNodes(treeData, result);
            ValidateConnections(treeData, result);
            return result;
        }

        private static void ValidateNodes(SkillTreeData treeData, SkillTreeValidationResult result)
        {
            HashSet<string> ids = new();
            Dictionary<Vector2Int, string> occupiedCells = new();

            if (treeData.nodes == null)
            {
                result.AddError("Tree nodes list is null.");
                return;
            }

            foreach (SkillNodeData node in treeData.nodes)
            {
                if (node == null)
                {
                    result.AddError("Tree contains a null node.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.id))
                {
                    result.AddError($"Node at {node.gridPosition} has an empty id.");
                    continue;
                }

                if (!ids.Add(node.id))
                    result.AddError($"Duplicate node id: {node.id}");

                if (occupiedCells.TryGetValue(node.gridPosition, out string existingNodeId))
                    result.AddError($"Cell {node.gridPosition} is occupied by both '{existingNodeId}' and '{node.id}'.");
                else
                    occupiedCells.Add(node.gridPosition, node.id);

                if (node.maxLevel < 1)
                    result.AddError($"Node '{node.id}' has maxLevel < 1.");

                if (node.costs != null)
                {
                    foreach (SkillCost cost in node.costs)
                    {
                        if (cost == null)
                        {
                            result.AddWarning($"Node '{node.id}' has null cost entry.");
                            continue;
                        }

                        if (cost.currency == null)
                            result.AddWarning($"Node '{node.id}' has cost with empty currency.");

                        if (cost.baseAmount < 0)
                            result.AddError($"Node '{node.id}' has cost baseAmount < 0.");
                    }
                }

                if (node.effects != null)
                {
                    foreach (SkillEffect effect in node.effects)
                    {
                        if (effect == null)
                        {
                            result.AddWarning($"Node '{node.id}' has null effect entry.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(effect.id))
                            result.AddWarning($"Node '{node.id}' has effect with empty id.");
                    }
                }
            }
        }

        private static void ValidateConnections(SkillTreeData treeData, SkillTreeValidationResult result)
        {
            HashSet<string> existingIds = new();

            foreach (SkillNodeData node in treeData.nodes)
            {
                if (node != null && !string.IsNullOrWhiteSpace(node.id))
                    existingIds.Add(node.id);
            }

            foreach (SkillNodeData node in treeData.nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.id) || node.previousNodeIds == null)
                    continue;

                foreach (string previousId in node.previousNodeIds)
                {
                    if (string.IsNullOrWhiteSpace(previousId))
                    {
                        result.AddWarning($"Node '{node.id}' has empty previous node id.");
                        continue;
                    }

                    if (previousId == node.id)
                        result.AddError($"Node '{node.id}' depends on itself.");

                    if (!existingIds.Contains(previousId))
                        result.AddError($"Node '{node.id}' depends on missing node '{previousId}'.");
                }
            }
        }
    }
}
