using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridSkillTree
{
    [Serializable]
    public class SkillNodeData
    {
        [Header("Identity")]
        public string id;
        public string title;

        [TextArea(3, 8)]
        public string description;

        [Header("Grid")]
        public Vector2Int gridPosition;

        [Header("Visual")]
        public Sprite icon;
        public Color iconColor = Color.white;

        [Header("Connections")]
        public List<string> previousNodeIds = new();

        [Header("Levels")]
        [Min(1)] public int maxLevel = 1;

        [Header("Cost")]
        public List<SkillCost> costs = new();

        [Header("Effects")]
        public List<SkillEffect> effects = new();
    }
}
