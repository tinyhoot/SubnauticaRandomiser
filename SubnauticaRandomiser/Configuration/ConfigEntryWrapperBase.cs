using System.Collections.Generic;
using BepInEx.Configuration;
using Nautilus.Options;

namespace SubnauticaRandomiser.Configuration
{
    /// <summary>
    /// Really only necessary because sometimes it is good to have arrays of these wrappers.
    /// </summary>
    public abstract class ConfigEntryWrapperBase
    {
        public string OptionLabel;
        public string OptionTooltip;
        public List<string> ControlledOptionIds;
        public bool HasControllingParent = false;
        public bool IsControllingParent => ControlledOptionIds?.Count > 0;
        
        public abstract string GetSection();
        public abstract string GetKey();
        public abstract string GetId();
        public abstract string GetLabel();
        public abstract string GetTooltip();
        public abstract void UpdateControlledOptions(IEnumerable<OptionItem> options);
    }
}