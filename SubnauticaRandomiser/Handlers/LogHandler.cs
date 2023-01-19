using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using ILogHandler = SubnauticaRandomiser.Interfaces.ILogHandler;
using Logger = BepInEx.Logging.Logger;

namespace SubnauticaRandomiser.Handlers
{
    /// A class for handling all the logging that the main program might ever want to do.
    /// Also includes main menu messages for relaying information to the user directly.
    [Serializable]
    internal class LogHandler : ILogHandler
    {
        private static readonly ManualLogSource _log = Logger.CreateLogSource(Initialiser.NAME);
        private readonly List<string> _ingameMessages;
        private bool _isReady;

        public LogHandler()
        {
            _ingameMessages = new List<string>();
            _isReady = false;
            // Get notified as soon as the game has loaded and is ready to display in-game messages.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void Debug(string message)
        {
            _log.LogDebug(message);
        }
        
        public void Info(string message)
        {
            _log.LogInfo(message);
        }

        public void Warn(string message)
        {
            _log.LogWarning(message);
        }

        public void Error(string message)
        {
            _log.LogError(message);
        }

        public void Fatal(string message)
        {
            _log.LogFatal(message);
        }

        /// Send an in-game message to the player.
        public void InGameMessage(string message, bool error = false)
        {
            _log.LogMessage("Main Menu Message: " + message);
            if (!_isReady)
                _ingameMessages.Add(message);
            else
                ErrorMessage.AddMessage(message);
        }

        private IEnumerator DisplayDelayedMessages()
        {
            yield return new WaitForSeconds(3f);
            _isReady = true;

            foreach (string message in _ingameMessages)
            {
                ErrorMessage.AddMessage(message);
            }
            _ingameMessages.Clear();
        }

        /// <summary>
        /// Get notified and proceed as soon as the main menu scene has loaded.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "XMenu")
                return;

            // Remove this event listener.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            // Increase the vanilla fadeout time for messages.
            ErrorMessage.main.timeDelay = 10f;
            ErrorMessage.main.StartCoroutine(DisplayDelayedMessages());
        }
    }
}
