using System;
using System.Collections.Generic;
using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    [CreateAssetMenu(
        fileName = DefaultAssetName,
        menuName = "DanieloZ/World Interaction/Object Groups")]
    public sealed class WorldInteraction_Config_ObjectGroups : ScriptableObject
    {
        public const string DefaultAssetName = "WorldInteraction_Config_ObjectGroups";
        public const string DefaultResourcesPath = "Data/" + DefaultAssetName;

        [SerializeField] private List<string> groupIds = new();

        public IReadOnlyList<string> GroupIds => groupIds;

        public static WorldInteraction_Config_ObjectGroups LoadDefault()
        {
            return Resources.Load<WorldInteraction_Config_ObjectGroups>(DefaultResourcesPath);
        }

        public static IEnumerable<string> GetConfiguredGroupIds()
        {
            var settings = LoadDefault();
            if (settings == null || settings.groupIds == null)
            {
                yield break;
            }

            for (var i = 0; i < settings.groupIds.Count; i++)
            {
                var groupId = NormalizeGroupId(settings.groupIds[i]);
                if (!string.IsNullOrEmpty(groupId))
                {
                    yield return groupId;
                }
            }
        }

        public static bool MatchesAcceptedGroup(string acceptedGroup, WorldInteraction_Slot_Item item)
        {
            var normalizedAcceptedGroup = NormalizeGroupId(acceptedGroup);
            if (string.IsNullOrEmpty(normalizedAcceptedGroup))
            {
                return true;
            }

            return item != null
                && string.Equals(
                    normalizedAcceptedGroup,
                    NormalizeGroupId(item.ObjectGroup),
                    StringComparison.Ordinal);
        }

        public static string NormalizeGroupId(string groupId)
        {
            return string.IsNullOrWhiteSpace(groupId) ? string.Empty : groupId.Trim();
        }

        public void ResetToDefaults()
        {
            groupIds ??= new List<string>();
            groupIds.Clear();
            groupIds.Add("Disk");
        }

        private void OnValidate()
        {
            Validate();
        }

        private void Validate()
        {
            groupIds ??= new List<string>();
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < groupIds.Count; i++)
            {
                var groupId = NormalizeGroupId(groupIds[i]);
                if (string.IsNullOrEmpty(groupId) || !uniqueIds.Add(groupId))
                {
                    groupIds.RemoveAt(i);
                    i--;
                    continue;
                }

                groupIds[i] = groupId;
            }
        }
    }
}
