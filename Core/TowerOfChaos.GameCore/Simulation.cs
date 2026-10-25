using System;
using System.Collections.Generic;
using System.Linq;

namespace TowerOfChaos.GameCore
{
    /// <summary>Room-local metric space: x = column axis, z = row axis (row 0 = entrance, +z = towards the exit). 1 tile = 2 m.</summary>
    public static class RoomSpace
    {
        public const double Tile = 2.0;
        public static double Width(RoomLayout L) { return L.Cols * Tile; }
        public static double Depth(RoomLayout L) { return L.Rows * Tile; }
        public static Cell CellAt(double x, double z) { return new Cell((int)Math.Floor(x / Tile), (int)Math.Floor(z / Tile)); }
        public static double CenterX(int cx) { return (cx + 0.5) * Tile; }
        public static double CenterZ(int cz) { return (cz + 0.5) * Tile; }
    }

    public sealed class HeroState
    {
        public const double MaxHp = 100.0;
        public double X, Z, Hp = MaxHp, Shield, Speed = 6.0;
    }

    /// <summary>Deterministic movement + collision for one hero inside one room (SPEC 2.2: capsule radius 0.5 m, 6 m/s).</summary>
    public static class HeroSim
    {
        public const double Radius = 0.5;

        public static void Step(RoomLayout L, HeroState h, double ix, double iz, double dt, bool doorLocked)
        {
            double len = Math.Sqrt(ix * ix + iz * iz);
            if (len > 1.0) { ix /= len; iz /= len; }
            double dx = ix * h.Speed * dt, dz = iz * h.Speed * dt;
            double w = RoomSpace.Width(L), d = RoomSpace.Depth(L);

            double nx = Math.Min(Math.Max(h.X + dx, Radius), w - Radius);
            if (!Collides(L, nx, h.Z, doorLocked)) h.X = nx;
            double nz = Math.Min(Math.Max(h.Z + dz, Radius), d - Radius);
            if (!Collides(L, h.X, nz, doorLocked)) h.Z = nz;
        }

        public static bool Collides(RoomLayout L, double x, double z, bool doorLocked)
        {
            int x0 = (int)Math.Floor((x - Radius) / RoomSpace.Tile), x1 = (int)Math.Floor((x + Radius) / RoomSpace.Tile);
            int z0 = (int)Math.Floor((z - Radius) / RoomSpace.Tile), z1 = (int)Math.Floor((z + Radius) / RoomSpace.Tile);
            for (int cx = Math.Max(0, x0); cx <= Math.Min(L.Cols - 1, x1); cx++)
                for (int cz = Math.Max(0, z0); cz <= Math.Min(L.Rows - 1, z1); cz++)
                {
                    bool blocked = L.Tiles[cx, cz] == TileKind.Hazard || (doorLocked && cx == L.Goal.X && cz == L.Goal.Y);
                    if (!blocked) continue;
                    double minX = cx * RoomSpace.Tile, maxX = minX + RoomSpace.Tile, minZ = cz * RoomSpace.Tile, maxZ = minZ + RoomSpace.Tile;
                    double px = Math.Min(Math.Max(x, minX), maxX), pz = Math.Min(Math.Max(z, minZ), maxZ);
                    double ddx = x - px, ddz = z - pz;
                    if (ddx * ddx + ddz * ddz < Radius * Radius) return true;
                }
            return false;
        }
    }

    public enum LaneState { Open, Warning, Blocked }

    /// <summary>
    /// Deterministic timed-trap clock (SPEC 14.1: traps are derived from matchSeed + time, never streamed per frame).
    /// Cycle = SafeWindow (open, last 0.8 s of it is the telegraph) + 1.0 s blocked.
    /// </summary>
    public static class TrapClock
    {
        public const double BlockedSeconds = 1.0;
        public const double WarnSeconds = 0.8;

        public static double Cycle(TrapDef d) { return d.SafeWindow + BlockedSeconds; }

        public static double Offset(long matchSeed, RoomLayout L, int placementIndex, TrapDef d)
        {
            return DeterministicRng.For(matchSeed, L.Floor, L.Room, 100 + placementIndex).NextDouble() * Cycle(d);
        }

        private static double Phase(long matchSeed, RoomLayout L, int idx, TrapDef d, double t)
        {
            double c = Cycle(d);
            double p = (t + Offset(matchSeed, L, idx, d)) % c;
            return p < 0 ? p + c : p;
        }

        public static LaneState StateAt(long matchSeed, RoomLayout L, int idx, TrapDef d, double t)
        {
            double p = Phase(matchSeed, L, idx, d, t);
            if (p >= d.SafeWindow) return LaneState.Blocked;
            return d.SafeWindow - p <= WarnSeconds ? LaneState.Warning : LaneState.Open;
        }

        /// <summary>Seconds the lane stays passable from now (0 when blocked).</summary>
        public static double OpenRemaining(long matchSeed, RoomLayout L, int idx, TrapDef d, double t)
        {
            double p = Phase(matchSeed, L, idx, d, t);
            return p >= d.SafeWindow ? 0.0 : d.SafeWindow - p;
        }
    }

    public sealed class HitCooldown
    {
        private readonly Dictionary<int, double> _last = new Dictionary<int, double>();
        public bool TryHit(int idx, double now, double cooldown)
        {
            double t;
            if (_last.TryGetValue(idx, out t) && now - t < cooldown) return false;
            _last[idx] = now; return true;
        }
        public void Clear() { _last.Clear(); }
    }

    public static class TrapRuntime
    {
        public const double HitCooldownSeconds = 1.0;

        /// <summary>PLACEHOLDER damage by cost tier until per-trap values are tuned (S0 / A10 / B20 / C35).</summary>
        public static int DamageFor(TrapDef d)
        {
            switch (d.Tier) { case CostTier.S: return 0; case CostTier.A: return 10; case CostTier.B: return 20; default: return 35; }
        }

        /// <summary>Damage events caused by the hero standing where he is at time t (center-tile test).</summary>
        public static List<int> Evaluate(long matchSeed, RoomLayout L, Catalog cat, HeroState h, double t, HitCooldown cd)
        {
            var hits = new List<int>();
            var cell = RoomSpace.CellAt(h.X, h.Z);
            for (int i = 0; i < L.Placements.Count; i++)
            {
                var p = L.Placements[i];
                if (p.TrapId == "AID_SWITCH") continue;
                var d = cat.Trap(p.TrapId);
                int dmg = DamageFor(d);
                if (dmg <= 0) continue;
                bool inside = p.Shape == TrapShape.FullRow ? cell.Y == p.Y : (cell.X == p.X && cell.Y == p.Y);
                if (!inside) continue;
                if (d.Policy == PathPolicy.TimedBlocks && TrapClock.StateAt(matchSeed, L, i, d, t) != LaneState.Blocked) continue;
                if (d.Policy == PathPolicy.BlocksWithBypass) continue;   // impassable tile, cannot be stood on
                if (cd.TryHit(i, t, HitCooldownSeconds)) hits.Add(dmg);
            }
            return hits;
        }
    }

    public enum CameraMode { Standard, Combat, XRay }

    public struct CameraPose
    {
        public double PosX, PosY, PosZ, TargetX, TargetY, TargetZ, PitchDeg, Fov;
    }

    /// <summary>SPEC 2.3 (offsets recomputed in v6.0 so that atan(Y/|Z|) = 54 / 58 / 62 deg). The camera looks along +z.</summary>
    public static class CameraRig
    {
        public const double LookAhead = 4.0;

        public static CameraPose Compute(double heroX, double heroZ, CameraMode mode)
        {
            double oy, oz, fov;
            switch (mode)
            {
                case CameraMode.Combat: oy = 14.4; oz = -9.0; fov = 48; break;
                case CameraMode.XRay: oy = 22.0; oz = -11.7; fov = 54; break;
                default: oy = 11.0; oz = -8.0; fov = 44; break;
            }
            double ahead = mode == CameraMode.Standard ? LookAhead : 0.0;
            return new CameraPose
            {
                TargetX = heroX, TargetY = 0, TargetZ = heroZ + ahead,
                PosX = heroX, PosY = oy, PosZ = heroZ + ahead + oz,
                PitchDeg = Math.Atan2(oy, -oz) * 180.0 / Math.PI, Fov = fov
            };
        }

        /// <summary>SPEC 2.3 activation rules.</summary>
        public static CameraMode ModeFor(RoomLayout L, int playersInRoom, bool nearPortal, bool xrayScan)
        {
            if (xrayScan) return CameraMode.XRay;
            if (playersInRoom >= 2 || L.IsGauntlet || nearPortal) return CameraMode.Combat;
            return CameraMode.Standard;
        }
    }
}
