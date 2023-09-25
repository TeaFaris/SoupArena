using Discord;
using SoupArena.Models.Battle.Entities;
using SoupArena.Models.Player;
using SoupArena.Models.SmartEnums;

namespace SoupArena.Models.Battle
{
    public sealed class BattlePlayer
    {
        public required SessionPlayer SessionPlayer { get; init; }

        public BattleClass Class { get; init; }
        public Equipment Equipment { get; init; }
        public required List<BattleConsumable> Consumables { get; init; } = new();
        public readonly List<BattleBuff> Buffs = new();
        public required BattleCell Cell { get; init; }
        public int? PositionOnField => SessionPlayer.CurrentBattle?.Field.GetPosition(this);
        public IUserMessage? Message { get; set; }

        public uint MaxHealth { get; set; }
        public uint Health { get; private set; }
        public uint Defence { get; set; }
        public uint DamageBoost { get; set; }
        public bool Surrendering { get; set; }

        public event EventHandler<BattlePlayer> OnDeath = async (_, _) => await Task.CompletedTask;

        public BattlePlayer(BattleClass Class, Equipment Equipment)
        {
            this.Class = Class;
            this.Equipment = Equipment;

            MaxHealth = Class.Class.Health;
            Health = MaxHealth;
            Defence = Equipment.Defence;
            DamageBoost = 0;
        }

        public void AddBuff(BattleBuff Buff)
        {
            var CurrentBuff = Buffs.Find(x => x.Type == Buff.Type && x.IsDebuff == Buff.IsDebuff);
            if (CurrentBuff is null)
            {
                Buffs.Add(Buff);
                Buff.OnSpellInvoke(null, this);
                return;
            }

            CurrentBuff.MovesLeft = Math.Max(CurrentBuff.MovesLeft, Buff.MovesLeft);
        }

        public void GiveDamage(ref uint Damage, BattlePlayer DamageSource)
        {
            Damage += DamageSource.DamageBoost;
            uint CurrentDefence = Defence > Damage ? Damage : Defence;

            Damage -= CurrentDefence;

            if (Health <= Damage)
            {
                Health = 0;

                OnDeath.Invoke(this, DamageSource);
            }
            else
            {
                Health -= Damage;
            }
        }
        public void Heal(ref uint Heal, BattlePlayer HealSource)
        {
            if (Health + Heal > MaxHealth)
                Health = MaxHealth;
            else
                Health += Heal;
        }

        public override string ToString() => $":heart: {Health}";
    }
}
