using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SubnauticaRandomiser.Interfaces;
using SubnauticaRandomiser.Objects;
using SubnauticaRandomiser.Objects.Enums;

namespace SubnauticaRandomiser.Logic.Recipes
{
    internal class BaseTheme
    {
        private Materials _materials;
        private ILogHandler _log;
        private IRandomHandler _random;

        private LogicEntity _baseTheme;

        public BaseTheme(Materials materials, ILogHandler logger, IRandomHandler random)
        {
            _materials = materials;
            _log = logger;
            _random = random;
        }

        /// <summary>
        /// Choose a theming ingredient for the base from among a range of easily available options.
        /// </summary>
        /// <param name="depth">The maximum depth at which the material must be available.</param>
        /// <param name="useFish">If true, consider fish as valid options for base theming.</param>
        /// <returns>A random LogicEntity from the Raw Materials or (if enabled) Fish categories.</returns>
        public LogicEntity ChooseBaseTheme(int depth, bool useFish = false)
        {
            List<LogicEntity> options = new List<LogicEntity>();

            options.AddRange(_materials.GetAll().FindAll(x => x.Category.Equals(ETechTypeCategory.RawMaterials)
                                                              && x.AccessibleDepth < depth
                                                              && !x.HasPrerequisites
                                                              && x.MaxUsesPerGame == 0
                                                              && x.GetItemSize() == 1));

            if (useFish)
            {
                options.AddRange(_materials.GetAll().FindAll(x => x.Category.Equals(ETechTypeCategory.Fish)
                                                                  && x.AccessibleDepth < depth
                                                                  && !x.HasPrerequisites
                                                                  && x.MaxUsesPerGame == 0
                                                                  && x.GetItemSize() == 1));
            }

            _baseTheme = _random.Choice(options);
            _log.Debug($"[R] Chose {_baseTheme} as base theme.");
            return _baseTheme;
        }
        
        /// <summary>
        /// If the given entity is a base piece, return the base theming ingredient.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>A LogicEntity if the passed entity is a base piece, null otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this method is called before a base theme was chosen.
        /// </exception>
        [CanBeNull]
        public LogicEntity GetBaseTheme(LogicEntity entity)
        {
            if (_baseTheme is null)
                throw new InvalidOperationException("Base theme must be chosen before it can be retrieved!");
            if (entity.Category.Equals(ETechTypeCategory.BaseBasePieces))
                return _baseTheme;

            return null;
        }
    }
}