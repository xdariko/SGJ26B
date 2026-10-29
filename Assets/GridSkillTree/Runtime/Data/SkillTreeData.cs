using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GridSkillTree
{
    [CreateAssetMenu(menuName = "Grid Skill Tree/Skill Tree Data")]
    public class SkillTreeData : ScriptableObject
    {
        [Tooltip("Stable id used for saving. Do not rename it after release.")]
        public string treeId = "main_tree";

        public List<SkillNodeData> nodes = new();

        public SkillNodeData GetNodeById(string id)
        {
            return nodes.FirstOrDefault(node => node != null && node.id == id);
        }

        public bool ContainsNode(string id)
        {
            return nodes.Any(node => node != null && node.id == id);
        }
    }
}
