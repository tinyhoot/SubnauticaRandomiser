using BepInEx.Configuration;

namespace SubnauticaRandomiser.Configuration
{
    internal class SaveFile
    {
        private ConfigFile _file;

        private ConfigEntry<string> _base64save;
        private ConfigEntry<int> _saveVersion;

        public string Base64Save => _base64save.Value;
        public int SaveVersion => _saveVersion.Value;

        public SaveFile(string path, int saveVersion)
        {
            _file = new ConfigFile(path, false);
            _base64save = _file.Bind("Save", "Base64Save", "",
                "This is your saved randomisation state. Delete this to delete your save.");
            _saveVersion = _file.Bind("Save", "SaveVersion", saveVersion,
                "This helps the mod keep track of whether you updated into a save incompatibility. "
                + "Do not touch this.");
        }

        public void Save(string base64, int saveVersion)
        {
            _base64save.Value = base64;
            _saveVersion.Value = saveVersion;
            _file.Save();
        }
    }
}