using System;
using System.Collections.Generic;

namespace GridSkillTree
{
    [Serializable]
    public class SkillNodeProgress
    {
        public string nodeId;
        public int level;
    }

    [Serializable]
    public class SkillTreeProgress
    {
        public List<SkillNodeProgress> nodeProgress = new();

        public int GetLevel(string nodeId)
        {
            SkillNodeProgress progress = nodeProgress.Find(item => item.nodeId == nodeId);
            return progress == null ? 0 : progress.level;
        }

        public void SetLevel(string nodeId, int level)
        {
            SkillNodeProgress progress = nodeProgress.Find(item => item.nodeId == nodeId);

            if (progress == null)
            {
                nodeProgress.Add(new SkillNodeProgress
                {
                    nodeId = nodeId,
                    level = level
                });
                return;
            }

            progress.level = level;
        }
    }
}
