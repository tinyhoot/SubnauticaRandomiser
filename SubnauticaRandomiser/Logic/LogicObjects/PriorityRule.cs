using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using SubnauticaRandomiser.Serialization;
using SubnauticaRandomiser.Serialization.Converters;
using UnityEngine;

namespace SubnauticaRandomiser.Logic.LogicObjects
{
    /// <summary>
    /// Represents a rule that entity randomisation must follow. Useful to guarantee some entities are randomised
    /// early, e.g. for QoL access to some items.
    /// </summary>
    [Serializable]
    internal class PriorityRule
    {
        [JsonIgnore]
        private const string Path = "priorityRules.json";
        
        /// <summary>
        /// All entities affected by this rule.
        /// </summary>
        [JsonConverter(typeof(StringEntityConverter))]
        public List<LogicEntity> Entities;
        
        /// <summary>
        /// If true, apply this rule to all entities. If false, apply it to at least one.
        /// </summary>
        public bool RequireAllEntities;
        
        /// <summary>
        /// Ensure the entities are randomised no later than this sphere tier.
        /// </summary>
        public int ForceBySphere = -1;
        
        /// <summary>
        /// Ensure the entities are randomised no further away than this many transitions from the starting
        /// <see cref="Region"/>.
        /// </summary>
        public int ForceByRegionDistanceFromStart = -1;

        public static IEnumerator LoadFromDiskAsync(EntityManager manager, TaskResult<List<PriorityRule>> rules)
        {
            var task = SerdeUtils.ReadFileContents(Path);
            yield return new WaitUntil(() => task.IsCompleted);
            yield return SerdeUtils.DeserializeObjectsAsync(task.Result, rules, new StringEntityConverter(manager));

            foreach (var rule in rules.value)
            {
                manager.ReplaceReferencesInPlace(rule.Entities);
            }
        }
    }
}