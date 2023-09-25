using Discord.WebSocket;
using SoupArena.Discord;
using SoupArena.Models.Battle;
using SoupArena.Models.Battle.Offer;
using SoupArena.Models.SmartEnums;

namespace SoupArena.Models.Player
{
    public class SessionPlayer
    {
        public ulong DiscordID { get; init; }
        public SocketUser User { get; init; }
        public SessionPlayer(ulong DiscordID)
        {
            this.DiscordID = DiscordID;
            User = DiscordBot.Instance.Client!.GetUser(DiscordID);
        }

        #region Merchant
        public Consumable? ObservableConsumable { get; set; }
        #endregion

        #region Inventory
        public Consumable? ChoosedConsumable { get; set; }
        #endregion

        #region Battle
        public BattleOffer? SelectedBattleOffer { get; set; }
        public BattleManager? CurrentBattle { get; set; }

        public Dictionary<BattleOffer, DateTime> BattleOffers { get; set; } = new Dictionary<BattleOffer, DateTime>();
        #endregion
    }
}
