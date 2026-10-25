using UnityEngine;
using Core = TowerOfChaos.GameCore;

namespace TowerOfChaos.Game
{
    /// <summary>IMGUI HUD for the Phase-0 slice (no UI prefabs needed). Real UI (SPEC 16.3 layout, RTL Persian) is a separate epic.</summary>
    public sealed class TowerHud : MonoBehaviour
    {
        private TowerBootstrap _b;
        private GUIStyle _label, _big;

        private static string Clock(double s)
        {
            if (s < 0) s = 0;
            int m = (int)(s / 60.0), sec = (int)(s % 60.0);
            return m.ToString("00") + ":" + sec.ToString("00");
        }

        private void Bar(Rect r, float frac, Color c)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f); GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = c; GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(frac), r.height - 4), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void OnGUI()
        {
            if (_b == null) _b = Object.FindFirstObjectByType<TowerBootstrap>();
            if (_b == null) return;
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 38) };
                _label.normal.textColor = Color.white;
                _big = new GUIStyle(_label) { fontSize = Mathf.Max(28, Screen.height / 14), alignment = TextAnchor.MiddleCenter };
            }
            float sw = Screen.width, sh = Screen.height, u = sh / 40f;

            if (_b.Error != null) { GUI.Label(new Rect(u, u, sw - 2 * u, sh), _b.Error, _label); return; }
            var run = _b.Run; if (run == null) return;
            int h = run.World.Config.Height;

            // HP / shield
            Bar(new Rect(u, u, sw * 0.25f, u * 1.3f), (float)(run.Hero.Hp / Core.HeroState.MaxHp), new Color(0.85f, 0.15f, 0.15f));
            if (run.Hero.Shield > 0) Bar(new Rect(u, u * 2.5f, sw * 0.25f, u * 0.8f), (float)(run.Hero.Shield / 50.0), new Color(0.3f, 0.6f, 1f));
            GUI.Label(new Rect(u * 1.2f, u * 0.9f, sw * 0.25f, u * 2f), "HP " + Mathf.CeilToInt((float)run.Hero.Hp), _label);

            // location + time + fog
            string where = "Floor " + run.Floor + "/" + h + "   Room " + run.Room + "/10   Progress " + Core.TowerConfig.Progress(run.Floor, run.Room) + "/" + run.World.Config.MaxProgress;
            GUI.Label(new Rect(sw * 0.3f, u * 0.5f, sw * 0.5f, u * 2f), where, _label);
            double fogAt = Core.TowerRules.FogReachesFloorAt(h, run.Floor);
            string fog = run.Elapsed < fogAt ? "Fog reaches this floor in " + Clock(fogAt - run.Elapsed) : "FOG! LEAVE THIS FLOOR";
            GUI.Label(new Rect(sw * 0.3f, u * 2.4f, sw * 0.5f, u * 2f), "Time " + Clock(run.Elapsed) + "   " + fog + "   Hard cap " + Clock(Core.TowerRules.HardCapSeconds(h)), _label);
            GUI.Label(new Rect(sw * 0.8f, u * 0.5f, sw * 0.2f, u * 2f), "Dice " + _b.DiceRoll + "   seed " + _b.CurrentSeed, _label);

            // hints
            string hint = "";
            if (run.Current.IsCrown) hint = "Hold the golden altar for 8 s (any damage resets it)  " + run.CrownProgress.ToString("0.0") + " / 8.0";
            else if (run.DoorLocked) hint = "Door locked - kill the monster (Space / tap right half) when within 3.5 m";
            else if (run.Current.IsGauntlet) hint = "Guardian dead - take the stairs (green tile)";
            GUI.Label(new Rect(sw * 0.2f, sh - u * 3f, sw * 0.6f, u * 2f), hint, _label);
            GUI.Label(new Rect(u, sh - u * 2f, sw * 0.6f, u * 2f), "Move: WASD / drag left half   Attack: Space / tap right half   G = show golden path   R = restart   Deaths " + run.Deaths + "  Trap hits " + run.TrapHits, _label);

            // floating joystick
            if (_b.JoyActive)
            {
                Vector2 o = _b.JoyOrigin, c = _b.JoyCurrent;
                GUI.color = new Color(1f, 1f, 1f, 0.25f); GUI.DrawTexture(new Rect(o.x - u * 3, sh - o.y - u * 3, u * 6, u * 6), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, 0.6f); GUI.DrawTexture(new Rect(c.x - u, sh - c.y - u, u * 2, u * 2), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            if (run.IsDead) GUI.Label(new Rect(0, 0, sw, sh), "YOU DIED - respawn in " + run.RespawnTimer.ToString("0.0") + " s (room 1 of this floor)", _big);
            if (run.Eliminated) GUI.Label(new Rect(0, 0, sw, sh), "ELIMINATED BY THE FOG\nMax progress " + run.MaxProgress + "   (R / tap to restart)", _big);
            if (run.Won) GUI.Label(new Rect(0, 0, sw, sh), "CROWN TAKEN!  Time " + Clock(run.Elapsed) + "   Deaths " + run.Deaths + "\n(R / tap to restart)", _big);
        }
    }
}
