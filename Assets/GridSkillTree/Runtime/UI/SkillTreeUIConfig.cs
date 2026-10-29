using UnityEngine;

namespace GridSkillTree
{
    [CreateAssetMenu(menuName = "Grid Skill Tree/UI Config")]
    public class SkillTreeUIConfig : ScriptableObject
    {
        [Header("Prefabs")]
        public SkillNodeButton nodeButtonPrefab;
        public SkillTreeConnectionLine connectionLinePrefab;

        [Header("Connections")]
        public float connectionThickness = 6f;

        [Header("Layout")]
        public Vector2 cellSize = new(120f, 120f);
        public Vector2 spacing = new(30f, 30f);

        [Header("State Colors")]
        public Color lockedColor = new Color32(42, 42, 55, 255);
        public Color availableColor = new Color32(105, 76, 180, 255);
        public Color maxedColor = new Color32(192, 154, 74, 255);
    }
}
