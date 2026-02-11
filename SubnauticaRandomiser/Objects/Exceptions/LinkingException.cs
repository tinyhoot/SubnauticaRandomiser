using System;

namespace SubnauticaRandomiser.Objects.Exceptions
{
    /// <summary>
    /// Used for errors that occur during entity linking, before randomising can begin.
    /// </summary>
    public class LinkingException : Exception
    {
        public LinkingException(string message) : base(message)
        {
        }
    }
}