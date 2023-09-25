namespace SoupArena.Models.Battle.Offer
{
    public record class BattleOffer
    {
        public OfferPlayer FirstPlayer { get; init; }
        public OfferPlayer SecondPlayer { get; init; }

        public event EventHandler<BattleManager> BattleStarted = delegate { };

        public BattleOffer(OfferPlayer FirstPlayer, OfferPlayer SecondPlayer)
        {
            this.FirstPlayer = FirstPlayer;
            this.SecondPlayer = SecondPlayer;

            FirstPlayer.InvokeBattleFounded(FirstPlayer, this);
            SecondPlayer.InvokeBattleFounded(SecondPlayer, this);
        }

        public void ReadyChanged()
        {
            if (FirstPlayer.IsReady && SecondPlayer.IsReady)
            {
                var BattleManager = new BattleManager(FirstPlayer.SessionPlayer, SecondPlayer.SessionPlayer);
                BattleStarted.Invoke(this, BattleManager);
            }
        }
    }
}
