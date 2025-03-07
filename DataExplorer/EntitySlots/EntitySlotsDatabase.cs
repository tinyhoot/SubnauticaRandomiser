using SQLite;

namespace DataExplorer.EntitySlots
{
    public class EntitySlotsDatabase
    {
        private SQLiteConnection _connection;

        public EntitySlotsDatabase(string path)
        {
            _connection = SetupDatabase(path);
        }

        private SQLiteConnection SetupDatabase(string path)
        {
            SQLiteConnection connection = new SQLiteConnection(path);
            SQLiteCommand cmd = connection.CreateCommand(
                @"CREATE TABLE IF NOT EXISTS EntitySlots (
                    Batch TEXT NOT NULL,
                    Cell TEXT NOT NULL,
                    WorldPos TEXT NOT NULL,
                    Biome TEXT NOT NULL,
                    SlotType TEXT NOT NULL,
                    IsPlaceholder INTEGER NOT NULL
                )");
            cmd.ExecuteNonQuery();

            connection.EnableWriteAheadLogging();
            return connection;
        }

        public void Insert(string batch, string cell, string worldPos, string biome, string slotType, bool placeholder)
        {
            var cmd = _connection.CreateCommand(
                @"INSERT INTO EntitySlots (Batch, Cell, WorldPos, Biome, SlotType, IsPlaceholder)
                          VALUES ($batch, $cell, $worldPos, $biome, $slotType, $placeholder)");
            cmd.Bind("$batch", batch);
            cmd.Bind("$cell", cell);
            cmd.Bind("$worldPos", worldPos);
            cmd.Bind("$biome", biome);
            cmd.Bind("$slotType", slotType);
            cmd.Bind("$placeholder", placeholder);
            cmd.ExecuteNonQuery();
        }

        public void StartTransaction()
        {
            _connection.BeginTransaction();
        }

        public void CommitTransaciton()
        {
            _connection.Commit();
        }

        public void Teardown()
        {
            _connection.Close();
        }
    }
}