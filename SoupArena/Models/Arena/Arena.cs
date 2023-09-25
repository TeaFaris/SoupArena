using C3.SmartEnums;
using SoupArena.Models.Battle.Offer;
using SoupArena.Models.Player;

namespace SoupArena.Models.Arena
{
    public record class Arena : ISmartEnum<byte>
    {
        public required byte ID { get; init; }
        public required string Name { get; init; }

        private readonly object LockObject = new();
        private readonly List<OfferPlayer> PlayersSearchingMatch = new List<OfferPlayer>();

        public void AddPlayerToQueue(OfferPlayer Player)
        {
            lock(LockObject)
            {
                PlayersSearchingMatch.Add(Player);

                if(PlayersSearchingMatch.Count > 1)
                {
                    var FirstPlayer = PlayersSearchingMatch[0];
                    var SecondPlayer = PlayersSearchingMatch[1];

                    if(FirstPlayer.SessionPlayer.DiscordID == SecondPlayer.SessionPlayer.DiscordID)
                        return;

                    PlayersSearchingMatch.Remove(FirstPlayer);
                    PlayersSearchingMatch.Remove(SecondPlayer);

                    var BattleOffer = new BattleOffer(FirstPlayer, SecondPlayer);
                    FirstPlayer.SessionPlayer.SelectedBattleOffer = BattleOffer;
                    SecondPlayer.SessionPlayer.SelectedBattleOffer = BattleOffer;
                }
            }
        }
        public OfferPlayer GetPlayerFromQueue(SessionPlayer Player) => PlayersSearchingMatch.Find(x => x.SessionPlayer == Player)!;
        public void RemovePlayerFromQueue(OfferPlayer OfferPlayer)
        {
            lock (LockObject)
            {
                PlayersSearchingMatch.Remove(OfferPlayer);
            }
        }

        public required Merchant Merchant { get; init; }

        public readonly static Arena Basic = new()
        {
            ID = 0,
            Name = "База",
            Merchant = Merchant.Basic
        };
    }
}