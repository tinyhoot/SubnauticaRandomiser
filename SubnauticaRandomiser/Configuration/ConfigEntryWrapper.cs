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

        /// <summary>
        /// Prepare the entry as a controller for displaying or hiding other options in the mod options menu.
        /// Only makes sense for ToggleOptions.
        /// </summary>
        /// <param name="options">All options affected by this one.</param>
        public ConfigEntryWrapper<T> WithControlOverOptions(params ConfigEntryWrapperBase[] options)
        {
            ToggleControllerIds = new List<string>();
            foreach (var option in options)
            {
                ToggleControllerIds.Add(option.GetId());
                option.HasControllingParent = true;
            }

            return this;
        }

        public override string GetId()
        {
            return Entry.Definition.Section + "_" + Entry.Definition.Key;
        }

        public override string GetLabel()
        {
            var label = OptionLabel ?? Entry.Definition.Key;
            if (HasControllingParent)
                label = $"- {label}";
            return label;
        }

        public override string GetTooltip()
        {
            return OptionTooltip ?? Entry.Description?.Description;
        }

        /// <summary>
        /// Set all other options' gameobjects to active/inactive state based on the value of this entry.
        /// </summary>
        public void UpdateControlledOptions(IReadOnlyCollection<OptionItem> options)
        {
            // Don't do anything if this option doesn't control any others.
            if (!(ToggleControllerIds?.Count > 0))
                return;
            
            // Ensure this only happens for entries with boolean values.
            if (!(Value is bool active))
                return;
            foreach (var option in options)
            {
                if (ToggleControllerIds.Contains(option.Id))
                    option.OptionGameObject.SetActive(active);
            }
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
            modOption.OnChanged += (_, e) => wrapper.Entry.Value = e.Value;
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
            modOption.OnChanged += (_, e) => wrapper.Entry.Value = e.Value;
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