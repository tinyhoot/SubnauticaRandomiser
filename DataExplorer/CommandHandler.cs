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
            DevConsole.RegisterConsoleCommand(this, "dumpBiomes");
            DevConsole.RegisterConsoleCommand(this, "dumpDataboxes");
            DevConsole.RegisterConsoleCommand(this, "dumpKnownTech");
            DevConsole.RegisterConsoleCommand(this, "dumpEncyclopedia");
            DevConsole.RegisterConsoleCommand(this, "dumpPrefabs");
            DevConsole.RegisterConsoleCommand(this, "prepareSlots");
            DevConsole.RegisterConsoleCommand(this, "dumpEntitySlots");
            DevConsole.RegisterConsoleCommand(this, "endEntitySlots");
        }

        private void OnConsoleCommand_dumpBiomes(NotificationCenter.Notification n)
        {
            ErrorMessage.AddMessage("Dumping biomes");
            DataDumper.LogBiomes();
        }

        private void OnConsoleCommand_dumpDataboxes(NotificationCenter.Notification n)
        {
            StartCoroutine(DataDumper.LogDataboxes());
        }

        private void OnConsoleCommand_dumpEncyclopedia(NotificationCenter.Notification n)
        {
            if (!PDAEncyclopedia.initialized)
            {
                ErrorMessage.AddMessage("PDA Encyclopedia is not yet initialised, please wait.");
                return;
            }
            ErrorMessage.AddMessage("Dumping PDA Encyclopedia");
            DataDumper.LogPDAEncyclopedia();
        }
        
        private void OnConsoleCommand_dumpKnownTech(NotificationCenter.Notification n)
        {
            ErrorMessage.AddMessage("Dumping known tech");
            DataDumper.LogKnownTech();
        }

        private void OnConsoleCommand_dumpPrefabs(NotificationCenter.Notification n)
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
            ErrorMessage.AddMessage("Dumping entity slots");
            StartCoroutine(EntitySlotDumper._main.ScrapeSlots());
        }

        private void OnConsoleCommand_endEntitySlots(NotificationCenter.Notification n)
        {
            EntitySlotDumper._main?.Teardown();
        }

        private void OnConsoleCommand_rando(NotificationCenter.Notification n)
        {
            DevConsole.SendConsoleCommand("oxygen");
            DevConsole.SendConsoleCommand("nodamage");
            DevConsole.SendConsoleCommand("item seaglide");
            DevConsole.SendConsoleCommand("item scanner");
        }
    }
}