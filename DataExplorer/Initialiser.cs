using BepInEx;
using BepInEx.Logging;

namespace DataExplorer
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.snmodding.nautilus", "1.0.0.33")]
    internal class Initialiser : BaseUnityPlugin
    {
        public const string GUID = "com.github.tinyhoot.DataExplorer";
        public const string NAME = "Data Explorer";
        public const string VERSION = "0.0.1";

        internal static ManualLogSource _Log;

        private void Awake()
        {
            _Log = Logger;
            _Log.LogInfo($"{NAME} v{VERSION} ready.");

            CommandHandler cmd = gameObject.AddComponent<CommandHandler>();
            cmd.RegisterCommands();
        }
    }
}