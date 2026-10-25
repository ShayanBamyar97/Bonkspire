using System.Collections.Generic;
using UnityEngine;
using Core = TowerOfChaos.GameCore;

namespace TowerOfChaos.Game
{
    /// <summary>
    /// Builds one room out of primitives from a Core RoomLayout (Phase-0 grey-box; real art replaces the primitives later).
    /// All rules/geometry come from Core - this class only draws.
    /// Room space: x = columns, z = rows (row 0 = entrance, +z = exit), 1 tile = 2 m.
    /// </summary>
    public sealed class RoomView
    {
        private sealed class Timed
        {
            public int Index; public Core.TrapDef Def; public GameObject Go; public Renderer Rend; public bool Row; public float Width;
        }

        private readonly Core.RoomLayout _l;
        private readonly Core.Catalog _cat;
        private readonly long _seed;
        private readonly Material _baseMat;
        private readonly GameObject _root;
        private readonly List<Timed> _timed = new List<Timed>();
        private GameObject _door, _monster;
        private Renderer _doorRend;

        public Vector3 MonsterPosition { get; private set; }

        public RoomView(Core.RoomLayout layout, Core.Catalog catalog, long matchSeed, Material baseMat, bool showGoldenPath)
        {
            _l = layout; _cat = catalog; _seed = matchSeed; _baseMat = baseMat;
            _root = new GameObject("Room F" + layout.Floor + " R" + layout.Room);
            Build(showGoldenPath);
        }

        public void Destroy() { if (_root != null) Object.Destroy(_root); }

        private Material Mat(Color c) { var m = new Material(_baseMat); m.color = c; return m; }

        private GameObject Box(string name, Vector3 center, Vector3 size, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var col = go.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = center; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat(c);
            return go;
        }

        private static Vector3 Center(int cx, int cz, float y)
        {
            return new Vector3((float)Core.RoomSpace.CenterX(cx), y, (float)Core.RoomSpace.CenterZ(cz));
        }

        private void Build(bool showPath)
        {
            float w = (float)Core.RoomSpace.Width(_l), d = (float)Core.RoomSpace.Depth(_l);
            var onPath = new HashSet<long>();
            foreach (var c in _l.GoldenPath) onPath.Add(c.X * 1000L + c.Y);

            // floor tiles (checkerboard + state colours)
            for (int x = 0; x < _l.Cols; x++)
                for (int z = 0; z < _l.Rows; z++)
                {
                    Color col = ((x + z) & 1) == 0 ? new Color(0.30f, 0.30f, 0.34f) : new Color(0.26f, 0.26f, 0.30f);
                    float h = 0.1f;
                    switch (_l.Tiles[x, z])
                    {
                        case Core.TileKind.EntranceSafe: col = new Color(0.25f, 0.40f, 0.55f); break;
                        case Core.TileKind.Hazard: col = new Color(0.55f, 0.08f, 0.10f); h = 0.9f; break;
                        case Core.TileKind.Trap: col = new Color(0.75f, 0.45f, 0.10f); break;
                        case Core.TileKind.CrossingAid: col = new Color(0.10f, 0.65f, 0.75f); h = 0.14f; break;
                        case Core.TileKind.Portal: col = new Color(0.60f, 0.25f, 0.85f); break;
                        case Core.TileKind.Altar: col = new Color(0.95f, 0.78f, 0.15f); h = 0.3f; break;
                    }
                    if (showPath && onPath.Contains(x * 1000L + z) && _l.Tiles[x, z] != Core.TileKind.CrossingAid) col = Color.Lerp(col, new Color(0.2f, 0.9f, 0.3f), 0.55f);
                    Box("tile", Center(x, z, h * 0.5f - 0.1f), new Vector3(1.94f, h, 1.94f), col);
                }

            // perimeter walls
            Color wall = new Color(0.18f, 0.16f, 0.20f);
            Box("wall-W", new Vector3(-0.25f, 1.5f, d * 0.5f), new Vector3(0.5f, 3f, d + 1f), wall);
            Box("wall-E", new Vector3(w + 0.25f, 1.5f, d * 0.5f), new Vector3(0.5f, 3f, d + 1f), wall);
            Box("wall-S", new Vector3(w * 0.5f, 1.5f, -0.25f), new Vector3(w + 1f, 3f, 0.5f), wall);
            Box("wall-N", new Vector3(w * 0.5f, 1.5f, d + 0.25f), new Vector3(w + 1f, 3f, 0.5f), wall);

            // exit door marker on the goal tile (red = locked, green = open); not for the crown room
            if (!_l.IsCrown)
            {
                _door = Box("door", Center(_l.Goal.X, _l.Goal.Y, 0.5f), new Vector3(1.7f, 1.0f, 1.7f), Color.green);
                _doorRend = _door.GetComponent<Renderer>();
            }

            // traps
            for (int i = 0; i < _l.Placements.Count; i++)
            {
                var p = _l.Placements[i];
                if (p.TrapId == "AID_SWITCH") { Box("switch", Center(p.X, p.Y, 0.2f), new Vector3(1.0f, 0.3f, 1.0f), new Color(0.2f, 0.9f, 0.9f)); continue; }
                var def = _cat.Trap(p.TrapId);
                if (def.Policy == Core.PathPolicy.TimedBlocks)
                {
                    bool row = p.Shape == Core.TrapShape.FullRow;
                    Vector3 pos = row ? new Vector3(w * 0.5f, 0.2f, (float)Core.RoomSpace.CenterZ(p.Y)) : Center(p.X, p.Y, 0.2f);
                    Vector3 size = row ? new Vector3(w - 0.1f, 0.4f, 1.7f) : new Vector3(1.7f, 0.4f, 1.7f);
                    var go = Box(p.TrapId, pos, size, Color.green);
                    _timed.Add(new Timed { Index = i, Def = def, Go = go, Rend = go.GetComponent<Renderer>(), Row = row, Width = size.x });
                }
                else if (def.Policy == Core.PathPolicy.NeverBlocks)
                {
                    Color c = def.Tier == Core.CostTier.S ? new Color(0.7f, 0.7f, 0.2f) : def.Tier == Core.CostTier.C ? new Color(0.9f, 0.1f, 0.1f) : new Color(0.95f, 0.5f, 0.1f);
                    Box(p.TrapId, Center(p.X, p.Y, 0.3f), new Vector3(1.0f, 0.5f, 1.0f), c);
                }
            }

            // reward orb, portal, monster, altar
            if (_l.RewardTile.HasValue)
            {
                var o = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(o.GetComponent<Collider>());
                o.transform.SetParent(_root.transform, false); o.transform.localScale = Vector3.one * 0.8f;
                o.transform.localPosition = Center(_l.RewardTile.Value.X, _l.RewardTile.Value.Y, 1.0f);
                o.GetComponent<Renderer>().sharedMaterial = Mat(new Color(1f, 0.9f, 0.1f));
                o.name = "reward " + _l.RewardId;
            }
            if (_l.PortalTile.HasValue)
            {
                var pt = _l.PortalTile.Value;
                var o = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(o.GetComponent<Collider>());
                o.transform.SetParent(_root.transform, false); o.transform.localScale = new Vector3(1.4f, 1.6f, 1.4f);
                o.transform.localPosition = Center(pt.X, pt.Y, 1.6f); o.name = "portal";
                o.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.65f, 0.3f, 0.95f));
            }
            if (_l.MonsterSpawn.HasValue && !_l.IsCrown)
            {
                var ms = _l.MonsterSpawn.Value;
                _monster = GameObject.CreatePrimitive(PrimitiveType.Capsule); Object.Destroy(_monster.GetComponent<Collider>());
                _monster.transform.SetParent(_root.transform, false);
                float s = _l.IsGauntlet ? 1.8f : 1.1f;
                _monster.transform.localScale = new Vector3(s, s, s);
                MonsterPosition = Center(ms.X, ms.Y, s);
                _monster.transform.localPosition = MonsterPosition; _monster.name = "monster";
                _monster.GetComponent<Renderer>().sharedMaterial = Mat(_l.IsGauntlet ? new Color(0.8f, 0.2f, 0.9f) : new Color(0.9f, 0.15f, 0.15f));
            }
        }

        /// <summary>Per-frame update: timed lane colours from the deterministic Core clock, door colour, monster visibility.</summary>
        public void Tick(double elapsed, bool doorLocked, bool monsterAlive)
        {
            foreach (var t in _timed)
            {
                var s = Core.TrapClock.StateAt(_seed, _l, t.Index, t.Def, elapsed);
                Color c = s == Core.LaneState.Open ? new Color(0.2f, 0.8f, 0.3f) : s == Core.LaneState.Warning ? new Color(1f, 0.85f, 0.1f) : new Color(0.95f, 0.1f, 0.1f);
                t.Rend.material.color = c;
                var sc = t.Go.transform.localScale;
                sc.y = s == Core.LaneState.Blocked ? 1.6f : 0.4f;
                t.Go.transform.localScale = sc;
                var pos = t.Go.transform.localPosition; pos.y = sc.y * 0.5f; t.Go.transform.localPosition = pos;
            }
            if (_doorRend != null) _doorRend.material.color = doorLocked ? new Color(0.9f, 0.1f, 0.1f) : new Color(0.1f, 0.9f, 0.2f);
            if (_monster != null) _monster.SetActive(monsterAlive);
        }
    }
}
