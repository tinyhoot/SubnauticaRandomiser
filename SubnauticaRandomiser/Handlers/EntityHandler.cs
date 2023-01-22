using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;
using SubnauticaRandomiser.Objects.Events;

namespace SubnauticaRandomiser.Handlers
{
    /// <summary>
    /// Handles anything related to entities, including whether they are considered accessible in logic.
    /// </summary>
    internal class EntityHandler
    {
        // Data structures are hard and I'm not convinced that a list is the right way to go here.
        private List<LogicEntity> _allEntities;
        private readonly HashSet<LogicEntity> _inLogic;
        private readonly ILogHandler _log;
        
        public List<LogicEntity> GetAll() => _allEntities.ShallowCopy();

        public EntityHandler(ILogHandler logger)
        {
            // Use a custom comparer to make that hash work properly for these otherwise mutable entities.
            _inLogic = new HashSet<LogicEntity>(new LogicEntityEqualityComparer());
            _log = logger;
        }

        public event EventHandler<EntityEventArgs> EntityEnteredLogic;

        /// <summary>
        /// Add the given TechType as prerequisite for all entities in the given category.
        /// </summary>
        /// <param name="category">The category to add prerequisites to.</param>
        /// <param name="techType">The new prerequisite to add.</param>
        public void AddCategoryPrerequisite(TechTypeCategory category, TechType techType)
        {
            foreach (var categoryEntity in _allEntities.FindAll(e => e.Category.Equals(category)))
            {
                categoryEntity.AddPrerequisite(techType);
            }
        }
        
        /// <summary>
        /// Add the given TechTypes as prerequisites for all entities in the given category.
        /// </summary>
        /// <param name="category">The category to add prerequisites to.</param>
        /// <param name="techTypes">The new prerequisites to add.</param>
        public void AddCategoryPrerequisite(TechTypeCategory category, ICollection<TechType> techTypes)
        {
            foreach (var categoryEntity in _allEntities.FindAll(e => e.Category.Equals(category)))
            {
                foreach (var techType in techTypes)
                {
                    categoryEntity.AddPrerequisite(techType);
                }
            }
        }

        /// <summary>
        /// Add the builder tool as a prerequisite to all base pieces. Adding it here because this is essential
        /// and must be done every time, no matter the loaded modules.
        /// </summary>
        private void AddBaseBuilderPrerequisite()
        {
            AddCategoryPrerequisite(TechTypeCategory.BaseBasePieces, TechType.Builder);
            AddCategoryPrerequisite(TechTypeCategory.BaseGenerators, TechType.Builder);
            AddCategoryPrerequisite(TechTypeCategory.BaseExternalModules, TechType.Builder);
            AddCategoryPrerequisite(TechTypeCategory.BaseInternalModules, TechType.Builder);
            AddCategoryPrerequisite(TechTypeCategory.BaseInternalPieces, TechType.Builder);
        }

        /// <summary>
        /// Mark a single entity as accessible in logic.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        public bool AddToLogic(LogicEntity entity)
        {
            if (entity is null || IsInLogic(entity))
                return false;
            
            _log.Debug($"Added entity to logic: {entity}");
            entity.InLogic = true;
            _inLogic.Add(entity);
            EntityEnteredLogic?.Invoke(this, new EntityEventArgs(entity));
            return true;
        }

        /// <summary>
        /// Mark multiple entities as accessible in logic.
        /// </summary>
        /// <returns>True if any new entities were added into logic, false otherwise.</returns>
        public bool AddToLogic(List<LogicEntity> additions)
        {
            bool anySuccess = false;
            foreach (LogicEntity entity in additions)
            {
                anySuccess |= AddToLogic(entity);
            }

            return anySuccess;
        }
        
        /// <summary>
        /// Add all entities matching the category up to a maximum depth into logic.
        /// </summary>
        /// <param name="category">The category to consider.</param>
        /// <param name="maxDepth">The maximum depth at which the entity is allowed to be available.</param>
        /// <returns>True if any new entities were added into logic, false otherwise.</returns>
        public bool AddToLogic(TechTypeCategory category, int maxDepth)
        {
            return AddToLogic(new[] { category }, maxDepth);
        }
        
        /// <summary>
        /// Add all entities matching one of the categories up to a maximum depth into logic.
        /// </summary>
        /// <param name="categories">The categories to consider.</param>
        /// <param name="maxDepth">The maximum depth at which the entity is allowed to be available.</param>
        /// <returns>True if any new entities were added into logic, false otherwise.</returns>
        public bool AddToLogic(TechTypeCategory[] categories, int maxDepth)
        {
            var additions = _allEntities.FindAll(x => categories.Contains(x.Category) && x.AccessibleDepth <= maxDepth);
            return AddToLogic(additions);
        }
        
        /// <summary>
        /// Add all entities that match the conditions and whose sole prerequisite is the given TechType into logic.
        /// </summary>
        /// <param name="category">The category to consider.</param>
        /// <param name="maxDepth">The maximum depth at which the entity is allowed to be available.</param>
        /// <param name="prerequisite">Only include entities which have exactly this entity as their one and only
        /// prerequisite.</param>
        /// <param name="invert">If true, invert the behaviour of the prerequisite to consider exclusively entities
        /// which do <i>not</i> require that TechType.</param>
        /// <returns>True if any new entities were added into logic, false otherwise.</returns>
        public bool AddToLogic(TechTypeCategory category, int maxDepth, TechType prerequisite, bool invert = false)
        {
            return AddToLogic(new[] { category }, maxDepth, prerequisite, invert);
        }
        
        /// <summary>
        /// Add all entities that match the conditions and whose sole prerequisite is the given TechType into logic.
        /// </summary>
        /// <param name="categories">The categories to consider.</param>
        /// <param name="maxDepth">The maximum depth at which the entity is allowed to be available.</param>
        /// <param name="prerequisite">Only include entities which have exactly this entity as their one and only
        /// prerequisite.</param>
        /// <param name="invert">If true, invert the behaviour of the prerequisite to consider exclusively entities
        /// which do <i>not</i> require that TechType.</param>
        /// <returns>True if any new entities were added into logic, false otherwise.</returns>
        public bool AddToLogic(TechTypeCategory[] categories, int maxDepth, TechType prerequisite, bool invert = false)
        {
            List<LogicEntity> additions;

            if (invert)
            {
                additions = _allEntities.FindAll(e => categories.Contains(e.Category)
                                                      && e.AccessibleDepth <= maxDepth
                                                      && (!e.HasPrerequisites
                                                          || !e.Prerequisites.Contains(prerequisite))
                );
            }
            else
            {
                additions = _allEntities.FindAll(e => categories.Contains(e.Category)
                                                      && e.AccessibleDepth <= maxDepth
                                                      && e.HasPrerequisites
                                                      && e.Prerequisites.Count == 1
                                                      && e.Prerequisites[0].Equals(prerequisite)
                );
            }

            return AddToLogic(additions);
        }

        /// <summary>
        /// Get the LogicEntity corresponding to the given TechType.
        /// </summary>
        /// <returns>The LogicEntity if found, null otherwise.</returns>
        [CanBeNull]
        public LogicEntity GetEntity(TechType techType)
        {
            LogicEntity entity = _allEntities.Find(e => e.TechType.Equals(techType));
            if (entity is null)
                _log.Warn($"Tried to get nonexistent entity for TechType {techType}!");
            return entity;
        }
        
        /// <summary>
        /// Get the LogicEntity corresponding to the given TechType.
        /// </summary>
        /// <returns>The LogicEntity if found, null otherwise.</returns>
        [CanBeNull]
        public LogicEntity GetEntity(TechType techType, EntityType entityType)
        {
            LogicEntity entity = _allEntities.Find(e => e.TechType.Equals(techType) && e.EntityType.Equals(entityType));
            if (entity is null)
                _log.Warn($"Tried to get nonexistent entity for TechType {techType}!");
            return entity;
        }

        /// <summary>
        /// Get all entities that are capable of being crafted.
        /// </summary>
        public List<LogicEntity> GetAllCraftables()
        {
            return _allEntities.FindAll(e => e.Category.IsCraftable());
        }

        /// <summary>
        /// Get all entities that are currently loaded by the Randomiser.
        /// </summary>
        public List<LogicEntity> GetAllEntities()
        {
            return _allEntities.ShallowCopy();
        }

        /// <summary>
        /// Get all entities that are considered fragments.
        /// </summary>
        public List<LogicEntity> GetAllFragments()
        {
            var fragments = _allEntities.FindAll(x =>
                x.Category.Equals(TechTypeCategory.Fragments));

            return fragments;
        }

        /// <summary>
        /// Get all entities that are considered raw materials without prerequisites and accessible by the given depth.
        /// </summary>
        /// <param name="maxDepth">The maximum depth at which the raw materials must be available.</param>
        public List<LogicEntity> GetAllRawMaterials(int maxDepth = 2000)
        {
            var rawMaterials = _allEntities.FindAll(x =>
                x.Category.Equals(TechTypeCategory.RawMaterials) && x.AccessibleDepth <= maxDepth
                                                                  && !x.HasPrerequisites);

            return rawMaterials;
        }

        /// <summary>
        /// Check whether the given entity is considered in logic, i.e. either randomised or otherwise accessible.
        /// </summary>
        public bool IsInLogic(LogicEntity entity)
        {
            return _inLogic.Contains(entity);
        }
        
        /// <summary>
        /// Check whether the given entity is considered in logic, i.e. either randomised or otherwise accessible.
        /// </summary>
        public bool IsInLogic(TechType techType)
        {
            return _inLogic.Any(e => e.TechType.Equals(techType));
        }

        /// <summary>
        /// Check whether the given entity is considered in logic, i.e. either randomised or otherwise accessible.
        /// Limits lookup to the given EntityType only.
        /// </summary>
        public bool IsInLogic(TechType techType, EntityType type)
        {
            return _inLogic.Where(e => e.EntityType.Equals(type)).Any(e => e.TechType.Equals(techType));
        }
        
        /// <summary>
        /// Load information on all entities from disk.
        /// </summary>
        public async Task ParseDataFileAsync(string fileName)
        {
            _allEntities = await CSVReader.ParseDataFileAsync(fileName, CSVReader.ParseRecipeLine);
            AddBaseBuilderPrerequisite();
        }
    }
}
