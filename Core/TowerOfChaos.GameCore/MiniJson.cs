using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TowerOfChaos.GameCore
{
    /// <summary>
    /// Tiny dependency-free JSON reader (object -> Dictionary, array -> List, number -> double).
    /// Needed because netstandard2.1 / Unity have no shared JSON library; used for catalog.json on client AND server.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string text)
        {
            var p = new Parser(text);
            p.SkipWs();
            object v = p.Value();
            p.SkipWs();
            if (!p.End) throw new FormatException("Trailing characters at " + p.Pos);
            return v;
        }

        private sealed class Parser
        {
            private readonly string _s; private int _i;
            public Parser(string s) { _s = s; _i = (s.Length > 0 && s[0] == '﻿') ? 1 : 0; }
            public bool End { get { return _i >= _s.Length; } }
            public int Pos { get { return _i; } }

            public void SkipWs() { while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++; }

            private char Peek() { if (_i >= _s.Length) throw new FormatException("Unexpected end"); return _s[_i]; }

            public object Value()
            {
                char c = Peek();
                if (c == '{') return Obj();
                if (c == '[') return Arr();
                if (c == '"') return Str();
                if (c == 't') { Expect("true"); return true; }
                if (c == 'f') { Expect("false"); return false; }
                if (c == 'n') { Expect("null"); return null; }
                return Num();
            }

            private void Expect(string w)
            {
                if (string.CompareOrdinal(_s, _i, w, 0, w.Length) != 0) throw new FormatException("Expected " + w + " at " + _i);
                _i += w.Length;
            }

            private Dictionary<string, object> Obj()
            {
                var d = new Dictionary<string, object>();
                _i++; SkipWs();
                if (Peek() == '}') { _i++; return d; }
                while (true)
                {
                    SkipWs(); string k = Str(); SkipWs();
                    if (Peek() != ':') throw new FormatException("Expected ':' at " + _i);
                    _i++; SkipWs();
                    d[k] = Value(); SkipWs();
                    char c = Peek(); _i++;
                    if (c == '}') return d;
                    if (c != ',') throw new FormatException("Expected ',' or '}' at " + (_i - 1));
                }
            }

            private List<object> Arr()
            {
                var l = new List<object>();
                _i++; SkipWs();
                if (Peek() == ']') { _i++; return l; }
                while (true)
                {
                    SkipWs(); l.Add(Value()); SkipWs();
                    char c = Peek(); _i++;
                    if (c == ']') return l;
                    if (c != ',') throw new FormatException("Expected ',' or ']' at " + (_i - 1));
                }
            }

            private string Str()
            {
                if (Peek() != '"') throw new FormatException("Expected string at " + _i);
                _i++;
                var sb = new StringBuilder();
                while (true)
                {
                    char c = Peek(); _i++;
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    char e = Peek(); _i++;
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            _i += 4; break;
                        default: throw new FormatException("Bad escape \\" + e);
                    }
                }
            }

            private double Num()
            {
                int st = _i;
                while (_i < _s.Length && "+-0123456789.eE".IndexOf(_s[_i]) >= 0) _i++;
                if (st == _i) throw new FormatException("Unexpected character '" + _s[_i] + "' at " + _i);
                return double.Parse(_s.Substring(st, _i - st), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>Reads Data/catalog.json (generated from SPEC.md) into a <see cref="Catalog"/>.</summary>
    public static class CatalogReader
    {
        public static Catalog Parse(string json)
        {
            var root = (Dictionary<string, object>)MiniJson.Parse(json);
            var c = new Catalog();
            foreach (Dictionary<string, object> t in (List<object>)root["traps"])
                c.Traps.Add(new TrapDef
                {
                    Id = S(t, "id"), NameEn = S(t, "nameEn"), NameFa = S(t, "nameFa"), Release = S(t, "release"),
                    HomeFloor = I(t, "homeFloor"),
                    Tier = (CostTier)Enum.Parse(typeof(CostTier), S(t, "tier")),
                    Policy = (PathPolicy)Enum.Parse(typeof(PathPolicy), S(t, "policy")),
                    Shape = (TrapShape)Enum.Parse(typeof(TrapShape), S(t, "shape")),
                    SafeWindow = (double)t["safeWindow"], Interactive = (bool)t["interactive"]
                });
            foreach (Dictionary<string, object> r in (List<object>)root["rewards"])
                c.Rewards.Add(new RewardDef { Id = S(r, "id"), NameEn = S(r, "nameEn"), NameFa = S(r, "nameFa"), Release = S(r, "release"), Rarity = (Rarity)Enum.Parse(typeof(Rarity), S(r, "rarity")) });
            foreach (Dictionary<string, object> m in (List<object>)root["monsters"])
                c.Monsters.Add(new MonsterDef { Id = S(m, "id"), NameEn = S(m, "nameEn"), NameFa = S(m, "nameFa"), Release = S(m, "release"), Floor = I(m, "floor"), Boss = (bool)m["boss"] });
            foreach (Dictionary<string, object> m in (List<object>)root["mutators"])
            {
                var fl = new List<int>(); foreach (object o in (List<object>)m["floors"]) fl.Add((int)(double)o);
                var cf = new List<string>(); foreach (object o in (List<object>)m["conflicts"]) cf.Add((string)o);
                c.Mutators.Add(new MutatorDef { Id = S(m, "id"), NameEn = S(m, "nameEn"), NameFa = S(m, "nameFa"), Release = S(m, "release"), Floors = fl.ToArray(), Conflicts = cf.ToArray(), GauntletSafe = (bool)m["gauntletSafe"] });
            }
            return c;
        }

        private static string S(Dictionary<string, object> d, string k) { return (string)d[k]; }
        private static int I(Dictionary<string, object> d, string k) { return (int)(double)d[k]; }
    }
}
