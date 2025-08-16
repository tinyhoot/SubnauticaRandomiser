using DataExplorer.EntitySlots;
using UnityEngine;

namespace DataExplorer
{
    /// <summary>
    /// Listen to and execute custom console commands.
    /// </summary>
    internal class CommandHandler : MonoBehaviour
    {
        /// <summary>
        /// Register the console commands added by this mod.
        /// </summary>
        public void RegisterCommands()
        {
            DevConsole.RegisterConsoleCommand("dump", OnConsoleCommand_dump);
            DevConsole.RegisterConsoleCommand("prepSlots", OnConsoleCommand_prepareSlots);
            DevConsole.RegisterConsoleCommand("scrapeSlots", OnConsoleCommand_dumpEntitySlots);
            DevConsole.RegisterConsoleCommand("endScrapeSlots", OnConsoleCommand_endEntitySlots);
        }

        private void OnConsoleCommand_dump(NotificationCenter.Notification n)
        {
            if (n.data.Count < 2)
            {
                ErrorMessage.AddMessage("Options: biomes, databoxes, ency, knownTech, prefabs");
                return;
            }

            switch (n.data[1])
            {
                case "biomes":
                    DumpBiomes();
                    break;
                case "databoxes":
                case "dbox":
                    DumpDataboxes();
                    break;
                case "ency":
                    DumpEncyclopedia();
                    break;
                case "knowntech":
                    DumpKnownTech();
                    break;
                case "prefabs":
                    DumpPrefabs();
                    break;
                default:
                    ErrorMessage.AddMessage("Bad option!");
                    break;
            }
        }

        private void DumpBiomes()
        {
            ErrorMessage.AddMessage("Dumping biomes");
            DataDumper.LogBiomes();
        }

        private void DumpDataboxes()
        {
            StartCoroutine(DataDumper.LogDataboxes());
        }

        private void DumpEncyclopedia()
        {
            if (!PDAEncyclopedia.initialized)
            {
                ErrorMessage.AddMessage("PDA Encyclopedia is not yet initialised, please wait.");
                return;
            }
            ErrorMessage.AddMessage("Dumping PDA Encyclopedia");
            DataDumper.LogPDAEncyclopedia();
        }
        
        private void DumpKnownTech()
        {
            ErrorMessage.AddMessage("Dumping known tech");
            DataDumper.LogKnownTech();
        }

        private void DumpPrefabs()
        {
            ErrorMessage.AddMessage("Dumping prefabs");
            DataDumper.LogPrefabs();
        }

        private void OnConsoleCommand_prepareSlots(NotificationCenter.Notification n)
        {
            ErrorMessage.AddMessage("Preparing DB for entity slot dumping");
            EntitySlotDumper dumper = new EntitySlotDumper();
        }

        private void OnConsoleCommand_dumpEntitySlots(NotificationCenter.Notification n)
        {
            if (EntitySlotDumper._main is null)
            {
                ErrorMessage.AddMessage("DB is not ready! Use 'prepSlots' first");
                return;
            }
            
            ErrorMessage.AddMessage("Dumping entity slots");
            StartCoroutine(EntitySlotDumper._main.ScrapeSlots());
        }

        private void OnConsoleCommand_endEntitySlots(NotificationCenter.Notification n)
        {
            EntitySlotDumper._main?.Teardown();
        }
    }
}