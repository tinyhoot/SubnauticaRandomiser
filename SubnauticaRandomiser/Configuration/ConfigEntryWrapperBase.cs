using System.Collections.Generic;

namespace SubnauticaRandomiser.Configuration
{
    /// <summary>
    /// Really only necessary because sometimes it is good to have arrays of these wrappers.
    /// </summary>
    public abstract class ConfigEntryWrapperBase
    {
        public string OptionLabel;
        public string OptionTooltip;
        public List<string> ToggleControllerIds;
        public bool HasControllingParent = false;

        public abstract string GetId();
        public abstract string GetLabel();
        public abstract string GetTooltip();
    }
}