namespace SubnauticaRandomiser.Logic.LogicObjects.Transitions
{
    internal class ItemLock : TransitionLock
    {
        public TechType RequiredItem;
        private LogicEntity _cachedEntity;
        
        public override bool CheckUnlocked(EntityManager manager)
        {
            _cachedEntity ??= manager.Find<LogicInventoryItem>(RequiredItem);

            return manager.IsAccessible(_cachedEntity);
        }
    }
}