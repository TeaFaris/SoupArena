using SoupArena.Models.Player;

namespace SoupArena.Models.Battle.Offer
{
    public class OfferPlayer : IDisposable
    {
        public required SessionPlayer SessionPlayer { get; init; }
        public bool IsReady { get; private set; }
        public event EventHandler<BattleOffer> BattleFound = delegate { };
        public event EventHandler<BattleOffer> Ready = delegate { };

        public void Dispose()
        {
            foreach (var Delegate in BattleFound.GetInvocationList())
                BattleFound -= Delegate as EventHandler<BattleOffer>;
            foreach (var Delegate in Ready.GetInvocationList())
                Ready -= Delegate as EventHandler<BattleOffer>;

            GC.SuppressFinalize(this);
        }

        public void InvokeBattleFounded(object? Sender, BattleOffer Battle)
        {
            BattleFound.Invoke(Sender, Battle);
        }
        public void SetReady()
        {
            IsReady = true;
            SessionPlayer.SelectedBattleOffer!.ReadyChanged();
            Ready.Invoke(this, SessionPlayer.SelectedBattleOffer);
        }
    }
}
