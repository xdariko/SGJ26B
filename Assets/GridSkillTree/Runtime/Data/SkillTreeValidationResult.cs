using System.Collections.Generic;
using System.Text;

namespace GridSkillTree
{
    public class SkillTreeValidationResult
    {
        public readonly List<string> errors = new();
        public readonly List<string> warnings = new();

        public bool IsValid => errors.Count == 0;

        public void AddError(string message) => errors.Add(message);
        public void AddWarning(string message) => warnings.Add(message);

        public string GetReport()
        {
            StringBuilder builder = new();

            if (errors.Count == 0 && warnings.Count == 0)
            {
                builder.AppendLine("Skill tree validation passed.");
                return builder.ToString();
            }

            if (errors.Count > 0)
            {
                builder.AppendLine("Errors:");
                foreach (string error in errors)
                    builder.AppendLine("- " + error);
            }

            if (warnings.Count > 0)
            {
                builder.AppendLine("Warnings:");
                foreach (string warning in warnings)
                    builder.AppendLine("- " + warning);
            }

            return builder.ToString();
        }
    }
}
