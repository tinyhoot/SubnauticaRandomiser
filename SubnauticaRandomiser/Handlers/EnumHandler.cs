using System;

namespace SubnauticaRandomiser.Handlers
{
    /// <summary>
    /// Handle anything related to dealing with Enums.
    /// </summary>
    internal class EnumHandler
    {
        /// <summary>
        /// Try parsing a string to the given type of Enum.
        /// </summary>
        /// <param name="value">The string to parse.</param>
        /// <returns>The parsed Enum if successful, or the Enum's default value if not.</returns>
        public static TEnum Parse<TEnum>(string value) where TEnum : struct
        {
            if (!Enum.TryParse(value, true, out TEnum result))
            {
                return default;
            }

            return result;
        }
    }
}