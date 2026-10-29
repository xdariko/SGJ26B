using UnityEditor;
using UnityEngine;

namespace GridSkillTree.Editor
{
    [CustomEditor(typeof(SkillTreeData))]
    public class SkillTreeDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10f);

            SkillTreeData treeData = (SkillTreeData)target;

            if (GUILayout.Button("Validate Skill Tree"))
            {
                SkillTreeValidationResult result = SkillTreeValidator.Validate(treeData);

                if (result.IsValid)
                    Debug.Log(result.GetReport(), treeData);
                else
                    Debug.LogError(result.GetReport(), treeData);
            }
        }
    }
}
