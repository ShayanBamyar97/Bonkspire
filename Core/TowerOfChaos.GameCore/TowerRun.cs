using System;

namespace TowerOfChaos.GameCore
{
    /// <summary>
    /// One player's run through the tower: room flow, traps, death/respawn, fog, crown channel.
    /// Pure logic - the Unity client renders it, the dedicated server will run one per player.
    /// </summary>
    public sealed class TowerRun
    {
        public const double CrownChannelSeconds = 8.0;
        public const double CrownRadius = 2.5;

        public readonly MatchWorld World;
        public readonly Catalog Catalog;
        public readonly HeroState Hero = new HeroState();
        public int Floor, Room, MaxProgress, Deaths, TrapHits;
        public double Elapsed, MaxProgressAt, RespawnTimer, InvulnTimer, CrownProgress;
        public bool RoomCleared, Won, Eliminated;
        private readonly HitCooldown _cd = new HitCooldown();
        private readonly DeterministicRng _rng;

        public TowerRun(MatchWorld world, Catalog catalog, int startRoom, int startFloor = 1)
        {
            World = world; Catalog = catalog;
            _rng = DeterministicRng.For(world.Seed, 0, 0, 9001);
            EnterRoom(startFloor, startRoom);
        }

        public RoomLayout Current { get { return World.Room(Floor, Room); } }
        public bool DoorLocked { get { return Current.MonsterLock && !RoomCleared && !Current.IsCrown; } }
        public bool IsDead { get { return RespawnTimer > 0; } }

        public void KillMonster() { RoomCleared = true; }

        private void EnterRoom(int floor, int room)
        {
            Floor = floor; Room = room;
            var L = Current;
            RoomCleared = !L.MonsterLock;
            Hero.X = RoomSpace.CenterX(L.Start.X); Hero.Z = RoomSpace.CenterZ(L.Start.Y);
            _cd.Clear(); CrownProgress = 0;
            int pr = TowerConfig.Progress(floor, room);
            if (pr > MaxProgress) { MaxProgress = pr; MaxProgressAt = Elapsed; }
        }

        public void Step(double dt, double ix, double iz)
        {
            if (Won || Eliminated) return;
            Elapsed += dt;
            if (InvulnTimer > 0) InvulnTimer = Math.Max(0, InvulnTimer - dt);

            // fog is checked first: it keeps hurting even while the hero waits to respawn into a fogged floor
            if (TowerRules.IsFloorFogged(World.Config.Height, Floor, Elapsed))
            {
                Damage(TowerRules.FogDamagePerSecond * dt, true);
                if (Eliminated) return;
            }

            if (RespawnTimer > 0)
            {
                RespawnTimer -= dt;
                if (RespawnTimer <= 0) { EnterRoom(Floor, 1); Hero.Hp = HeroState.MaxHp; Hero.Shield = 0; InvulnTimer = 3.0; }
                return;
            }

            HeroSim.Step(Current, Hero, ix, iz, dt, DoorLocked);

            if (InvulnTimer <= 0)
                foreach (int dmg in TrapRuntime.Evaluate(World.Seed, Current, Catalog, Hero, Elapsed, _cd)) { TrapHits++; Damage(dmg, false); if (IsDead || Eliminated) return; }

            if (Current.IsCrown) UpdateCrown(dt); else TryAdvance();
        }

        public void Damage(double amount, bool fog)
        {
            if (amount <= 0 || Won || Eliminated) return;
            if (!fog && InvulnTimer > 0) return;
            CrownProgress = 0;                       // any damage cancels the crown channel (SPEC 11.4)
            double absorbed = Math.Min(Hero.Shield, amount);
            Hero.Shield -= absorbed; Hero.Hp -= amount - absorbed;
            if (Hero.Hp > 0) return;
            Hero.Hp = 0;
            if (fog) { Eliminated = true; return; }  // SPEC 17.4: dying in fog is permanent elimination
            Deaths++;
            RespawnTimer = MatchFlow.Respawn(Floor, Room, _rng).DelaySeconds;   // same floor, room 1, never falls a floor
        }

        private void TryAdvance()
        {
            var L = Current;
            var c = RoomSpace.CellAt(Hero.X, Hero.Z);
            if (c.X != L.Goal.X || c.Y != L.Goal.Y || DoorLocked) return;
            if (Room < TowerConfig.RoomsPerFloor) EnterRoom(Floor, Room + 1);
            else EnterRoom(Floor + 1, 1);            // stairs; the crown room never reaches here
        }

        private void UpdateCrown(double dt)
        {
            var L = Current;
            double ax = RoomSpace.CenterX(L.Goal.X), az = RoomSpace.CenterZ(L.Goal.Y);
            double dx = Hero.X - ax, dz = Hero.Z - az;
            if (dx * dx + dz * dz <= CrownRadius * CrownRadius) CrownProgress += dt; else CrownProgress = 0;
            if (CrownProgress >= CrownChannelSeconds) Won = true;
        }
    }
}
