using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Nautilus.Options;

namespace SubnauticaRandomiser.Configuration
{
    /// <summary>
    /// A wrapper around the BepInEx ConfigEntry which provides extra fields for more fine-grained control over
    /// ModOption behaviour in the in-game options menu.
    /// </summary>
    internal class ConfigEntryWrapper<T> : ConfigEntryWrapperBase
    {
        public Dictionary<T, HashSet<string>> ControllingValues;
        public readonly ConfigEntry<T> Entry;
        public T Value => Entry.Value;

        public ConfigEntryWrapper(ConfigEntry<T> entry)
        {
            Entry = entry;
        }
        
        public ConfigEntryWrapper(ConfigFile configFile, string section, string key, T defaultValue, string description)
        {
            Entry = configFile.Bind(
                section: section,
                key: key,
                defaultValue: defaultValue,
                description: description
            );
        }

        public ConfigEntryWrapper(ConfigFile configFile, string section, string key, T defaultValue, string description, AcceptableValueBase acceptableValues)
        {
            Entry = configFile.Bind(
                section: section,
                key: key,
                defaultValue: defaultValue,
                configDescription: new ConfigDescription(description, acceptableValues)
            );
        }

        /// <summary>
        /// Prepare the entry with custom label and description for display in the mod options menu.
        /// </summary>
        public ConfigEntryWrapper<T> WithDescription(string label, string tooltip)
        {
            OptionLabel = label;
            OptionTooltip = tooltip;

            return this;
        }

        public override string GetSection() => Entry.Definition.Section;
        public override string GetKey() => Entry.Definition.Key;

        public override string GetId()
        {
            return Entry.Definition.Section + "_" + Entry.Definition.Key;
        }

        public override string GetLabel()
        {
            var label = OptionLabel ?? Entry.Definition.Key;
            if (NumControllingParents > 0)
                label = string.Concat(Enumerable.Repeat("   ", NumControllingParents - 1)) + $" - {label}";
            return label;
        }

        public override string GetTooltip()
        {
            return OptionTooltip ?? Entry.Description?.Description;
        }

        /// <summary>
        /// Set all other options' gameobjects to active/inactive state based on the value of this entry.
        /// </summary>
        public override void UpdateControlledOptions(IEnumerable<OptionItem> options)
        {
            // Don't do anything if this option doesn't control any others.
            if (!IsControllingParent)
                return;
            
            // Collect a list of all child options which themselves also control children.
            List<ConfigEntryWrapperBase> controllers = new List<ConfigEntryWrapperBase>();
            
            var optionItems = options as OptionItem[] ?? options.ToArray();
            foreach (var option in optionItems)
            {
                if (!ControlledOptionIds.Contains(option.Id))
                    continue;
                
                bool active = ControllingValues.GetOrDefault(Value, null)?.Contains(option.Id) ?? false;
                option.OptionGameObject.SetActive(active);
                // Add the option to the list of child controllers if it is one.
                if (active)
                {
                    var optionWrapper = Initialiser._Config.GetEntryById(option.Id);
                    if (optionWrapper.IsControllingParent)
                        controllers.Add(optionWrapper);
                }
            }
            
            // Trigger updates for each child controller.
            controllers.ForEach(wrapper => wrapper.UpdateControlledOptions(optionItems));
        }

        /// <summary>
        /// Prepare the entry as a controller for displaying or hiding other options in the mod options menu.
        /// This enables showing options with preconditions, such as hiding all options of a module when that module
        /// is not active.
        ///
        /// Options controlled in this way will only display when the value of this entry matches their precondition.
        /// It is possible to enable an option for multiple values. Useful e.g. for ChoiceOptions.
        /// </summary>
        /// <param name="value">The value this entry must be set to to enable all the other options.</param>
        /// <param name="enabledOptions">All options shown when the value is set correctly.</param>
        public ConfigEntryWrapper<T> WithConditionalOptions(T value, params ConfigEntryWrapperBase[] enabledOptions)
        {
            ControlledOptionIds ??= new List<string>();
            ControllingValues ??= new Dictionary<T, HashSet<string>>();
            if (!ControllingValues.ContainsKey(value))
                ControllingValues.Add(value, new HashSet<string>());

            foreach (var option in enabledOptions)
            {
                ControlledOptionIds.Add(option.GetId());
                ControllingValues[value].Add(option.GetId());
            }

            return this;
        }

        /// <summary><inheritdoc cref="WithConditionalOptions(T,SubnauticaRandomiser.Configuration.ConfigEntryWrapperBase[])"/></summary>
        /// <param name="value"><inheritdoc cref="WithConditionalOptions(T,SubnauticaRandomiser.Configuration.ConfigEntryWrapperBase[])"/></param>
        /// <param name="section">This option will have control over this entire section.</param>
        public ConfigEntryWrapper<T> WithConditionalOptions(T value, string section)
        {
            return WithConditionalOptions(value,
                Initialiser._Config.GetSectionEntries(section)
                    .Where(entry => !entry.GetId().Equals(this.GetId()))
                    .ToArray());
        }
    }

    internal static class ConfigEntryWrapperExtensions
    {
        /// <summary>
        /// Converts a wrapper for a ConfigEntry to a ModOption.
        /// </summary>
        public static ModChoiceOption<T> ToModChoiceOption<T>(this ConfigEntryWrapper<T> wrapper)
            where T : IEquatable<T>
        {
            T[] options = null;
            if (wrapper.Entry.Description.AcceptableValues is AcceptableValueList<T> valueList)
                options = valueList.AcceptableValues;
            if (options == null)
                throw new ArgumentException("Could not get values from ConfigEntry");

            var modOption = ModChoiceOption<T>.Create(
                id: wrapper.GetId(),
                label: wrapper.GetLabel(),
                options: options,
                value: wrapper.Value,
                tooltip: wrapper.GetTooltip()
            );
            modOption.OnChanged += (_, e) => 
            {
                wrapper.Entry.Value = e.Value;
                wrapper.UpdateControlledOptions(ConfigModOptions.Instance.Options);
            };
            return modOption;
        }

        public static ModChoiceOption<TE> ToModChoiceOption<TE>(this ConfigEntryWrapper<TE> wrapper, IEnumerable<TE> values = null) where TE : Enum
        {
            TE[] options = values?.ToArray() ?? (TE[])Enum.GetValues(typeof(TE));
            if (options == null)
                throw new ArgumentException("Could not get values from ConfigEntry");
            
            var modOption = ModChoiceOption<TE>.Create(
                id: wrapper.GetId(),
                label: wrapper.GetLabel(),
                options: options,
                value: wrapper.Value,
                tooltip: wrapper.GetTooltip()
            );
            modOption.OnChanged += (_, e) => 
            {
                wrapper.Entry.Value = e.Value;
                wrapper.UpdateControlledOptions(ConfigModOptions.Instance.Options);
            };
            return modOption;
        }

        /// <summary>
        /// Converts a wrapper for a ConfigEntry to a ModOption.
        /// </summary>
        public static ModSliderOption ToModSliderOption(this ConfigEntryWrapper<int> wrapper, float minValue,
            float maxValue, float stepSize = 1f)
        {
            var modOption = ModSliderOption.Create(
                id: wrapper.GetId(),
                label: wrapper.GetLabel(),
                minValue: minValue,
                maxValue: maxValue,
                value: wrapper.Value,
                step: stepSize,
                tooltip: wrapper.GetTooltip()
            );
            modOption.OnChanged += (_, e) => wrapper.Entry.Value = (int)e.Value;
            return modOption;
        }

        /// <summary>
        /// Converts a wrapper for a ConfigEntry to a ModOption.
        /// </summary>
        public static ModSliderOption ToModSliderOption(this ConfigEntryWrapper<float> wrapper, float minValue,
            float maxValue, string valueFormat = "{0:F2}", float stepSize = 1f)
        {
            var modOption = ModSliderOption.Create(
                id: wrapper.GetId(),
                label: wrapper.GetLabel(),
                minValue: minValue,
                maxValue: maxValue,
                value: wrapper.Value,
                step: stepSize,
                valueFormat: valueFormat,
                tooltip: wrapper.GetTooltip()
            );
            modOption.OnChanged += (_, e) => wrapper.Entry.Value = e.Value;
            return modOption;
        }

        /// <summary>
        /// Converts a wrapper for a ConfigEntry to a ModOption.
        /// </summary>
        public static ModSliderOption ToModSliderOption(this ConfigEntryWrapper<double> wrapper, float minValue,
            float maxValue, string valueFormat = "{0:F2}", float stepSize = 1f)
        {
            var modOption = ModSliderOption.Create(
                id: wrapper.GetId(),
                label: wrapper.GetLabel(),
                minValue: minValue,
                maxValue: maxValue,
                value: (float)wrapper.Value,
                step: stepSize,
                valueFormat: valueFormat,
                tooltip: wrapper.GetTooltip()
            );
            modOption.OnChanged += (_, e) => wrapper.Entry.Value = e.Value;
            return modOption;
        }

        /// <summary>
        /// Converts a wrapper for a ConfigEntry to a ModOption.
        /// </summary>
        public static ModToggleOption ToModToggleOption(this ConfigEntryWrapper<bool> wrapper)
        {
            var modOption = ModToggleOption.Create(
                wrapper.GetId(),
                wrapper.GetLabel(),
                wrapper.Value,
                wrapper.GetTooltip()
            );
            modOption.OnChanged += (_, e) =>
            {
                wrapper.Entry.Value = e.Value;
                wrapper.UpdateControlledOptions(ConfigModOptions.Instance.Options);
            };
            return modOption;
        }
    }
}