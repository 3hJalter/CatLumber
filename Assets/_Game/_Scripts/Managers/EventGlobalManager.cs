using _Game.DesignPattern;
using Sigtrap.Relays;

namespace _Game._Scripts.Managers
{
    public class EventGlobalManager : Singleton<EventGlobalManager>
    {
        // Global events
        public Relay<int> OnMoneyGoldChanged;
        public Relay<int> OnMoneyGemChanged;
        public Relay<int> OnTicketChanged;
        public Relay OnStartLoadScene;
        public Relay OnFinishLoadScene;
        public Relay OnUpdateSetting;
        public Relay OnGameInit;
        public Relay OnPurchaseNoAds;
        
        // In-game events
        // Unit Button
        // ReSharper disable once Unity.RedundantHideInInspectorAttribute
        public Relay<bool> OnButtonUnitEnter;

        private void Awake()
        {
            OnMoneyGoldChanged = new Relay<int>();
            OnMoneyGemChanged = new Relay<int>();
            OnTicketChanged = new Relay<int>();
            OnStartLoadScene = new Relay();
            OnFinishLoadScene = new Relay();
            OnUpdateSetting = new Relay();
            OnGameInit = new Relay();
            OnPurchaseNoAds = new Relay();
            OnButtonUnitEnter = new Relay<bool>();
        }
    }
}
