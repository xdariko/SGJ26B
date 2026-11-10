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

        [Tooltip("Разные описания для уровней разделяются ||: показывается описание следующего уровня (у купленного до конца — последнее)")]
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

        public const string LevelSeparator = "||";

        // Описание для узла с текущим уровнем level: части через || идут по уровням,
        // последняя часть повторяется для всех следующих уровней
        public string GetDescription(int level)
        {
            if (string.IsNullOrEmpty(description) || !description.Contains(LevelSeparator))
                return description;

            string[] parts = description.Split(new[] { LevelSeparator }, StringSplitOptions.None);
            int index = Mathf.Clamp(Mathf.Min(level, maxLevel - 1), 0, parts.Length - 1);
            return parts[index].Trim();
        }
    }
}
