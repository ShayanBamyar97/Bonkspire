using UnityEngine;
using Core = TowerOfChaos.GameCore;

namespace TowerOfChaos.Game
{
    /// <summary>
    /// Phase-0 vertical slice. Creates itself on Play (no scene setup needed), loads Resources/catalog.json,
    /// builds a whole match world with the Core library and lets one hero run it.
    /// Everything that decides outcomes (room generation, traps, damage, death, fog, crown) lives in
    /// TowerOfChaos.GameCore; this class only feeds input and draws.
    /// </summary>
    public sealed class TowerBootstrap : MonoBehaviour
    {
        public int TowerHeight = 6;        // v1.0 = 6 floors (SPEC 29); 10 after Update 1
        public int Seed = 0;               // 0 = new random world every run
        public bool ShowGoldenPath;        // toggle with G (debug)

        public Core.TowerRun Run { get; private set; }
        public Core.MatchWorld World { get; private set; }
        public int DiceRoll { get; private set; }
        public string Error { get; private set; }
        public bool MonsterAlive { get; private set; }
        public Vector2 JoyOrigin { get; private set; }
        public Vector2 JoyCurrent { get; private set; }
        public bool JoyActive { get; private set; }
        public long CurrentSeed { get; private set; }

        private Core.Catalog _catalog;
        private RoomView _view;
        private int _viewFloor = -1, _viewRoom = -1;
        private GameObject _hero;
        private Camera _cam;
        private Material _baseMat;
        private double _monsterHp;
        private float _attackCooldown;
        private bool _snapCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (Object.FindFirstObjectByType<TowerBootstrap>() != null) return;
            var go = new GameObject("TowerBootstrap");
            go.AddComponent<TowerBootstrap>();
            go.AddComponent<TowerHud>();
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            var ta = Resources.Load<TextAsset>("catalog");
            if (ta == null) { Error = "Resources/catalog.json is missing. Run: python Core/tools/sync_to_unity.py"; return; }
            _catalog = Core.CatalogReader.Parse(ta.text);

            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _baseMat = tmp.GetComponent<Renderer>().sharedMaterial;      // default material of whatever pipeline is active
            Object.Destroy(tmp);

            _cam = Camera.main;
            if (_cam == null)
            {
                var cg = new GameObject("Main Camera"); cg.tag = "MainCamera";
                _cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>();
            }
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lg = new GameObject("Sun"); var l = lg.AddComponent<Light>();
                l.type = LightType.Directional; lg.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            }
            NewRun();
        }

        private void NewRun()
        {
            CurrentSeed = Seed != 0 ? Seed : (System.DateTime.UtcNow.Ticks & 0x7FFFFFFF);
            World = Core.MatchWorld.Build(new Core.TowerConfig(TowerHeight), _catalog, CurrentSeed);
            DiceRoll = Core.MatchFlow.RollStartRoom(Core.DeterministicRng.For(CurrentSeed, 0, 0, 5));   // start dice (SPEC 11.1)
            Run = new Core.TowerRun(World, _catalog, DiceRoll);
            _viewFloor = -1; _viewRoom = -1;
            if (_hero == null)
            {
                _hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                var col = _hero.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
                _hero.name = "Hero";
                _hero.GetComponent<Renderer>().sharedMaterial = new Material(_baseMat) { color = new Color(0.2f, 0.55f, 1f) };
                var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var nc = nose.GetComponent<Collider>(); if (nc != null) Object.Destroy(nc);
                nose.transform.SetParent(_hero.transform, false);
                nose.transform.localPosition = new Vector3(0f, 0.4f, 0.55f); nose.transform.localScale = new Vector3(0.4f, 0.25f, 0.5f);
                nose.GetComponent<Renderer>().sharedMaterial = new Material(_baseMat) { color = Color.white };
            }
        }

        private void Update()
        {
            if (Run == null) return;
            float ix, iz; bool attack, restart;
            ReadInput(out ix, out iz, out attack, out restart);
            if (Run.Won || Run.Eliminated) { if (restart) NewRun(); return; }

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            Run.Step(dt, ix, iz);
            SyncView();

            _attackCooldown -= dt;
            if (attack && MonsterAlive && _attackCooldown <= 0f)
            {
                var hp = new Vector3((float)Run.Hero.X, 0f, (float)Run.Hero.Z);
                var mp = _view.MonsterPosition;
                if ((new Vector3(mp.x, 0f, mp.z) - hp).magnitude < 3.5f)
                {
                    _attackCooldown = 0.6f; _monsterHp -= 25.0;
                    if (_monsterHp <= 0) Run.KillMonster();
                }
            }
            MonsterAlive = Run.Current.MonsterLock && !Run.Current.IsCrown && !Run.RoomCleared;
            _view.Tick(Run.Elapsed, Run.DoorLocked, MonsterAlive);

            _hero.SetActive(!Run.IsDead);
            _hero.transform.position = new Vector3((float)Run.Hero.X, 1f, (float)Run.Hero.Z);
            var dir = new Vector3(ix, 0f, iz);
            if (dir.sqrMagnitude > 0.01f) _hero.transform.rotation = Quaternion.LookRotation(dir);
        }

        private void SyncView()
        {
            if (Run.Floor == _viewFloor && Run.Room == _viewRoom) return;
            if (_view != null) _view.Destroy();
            _view = new RoomView(Run.Current, _catalog, World.Seed, _baseMat, ShowGoldenPath);
            _viewFloor = Run.Floor; _viewRoom = Run.Room;
            _monsterHp = Run.Current.IsGauntlet ? 125.0 : 50.0;     // SPEC 5.2: normal 50 HP (2 hits), stairs guardian 125 HP (5 hits)
            _snapCamera = true;
        }

        private void LateUpdate()
        {
            if (Run == null || _cam == null || _view == null) return;
            var mode = Core.CameraRig.ModeFor(Run.Current, 1, false, false);
            var p = Core.CameraRig.Compute(Run.Hero.X, Run.Hero.Z, mode);
            var pos = new Vector3((float)p.PosX, (float)p.PosY, (float)p.PosZ);
            var tgt = new Vector3((float)p.TargetX, (float)p.TargetY, (float)p.TargetZ);
            var rot = Quaternion.LookRotation(tgt - pos);
            if (_snapCamera) { _cam.transform.position = pos; _cam.transform.rotation = rot; _cam.fieldOfView = (float)p.Fov; _snapCamera = false; return; }
            float k = 1f - Mathf.Exp(-12f * Time.deltaTime);
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, pos, k);
            _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation, rot, k);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, (float)p.Fov, k);
        }

        // Left half of the screen = floating joystick (touch or mouse), right half = attack. Keyboard: WASD/arrows, Space, R, G.
        private void ReadInput(out float ix, out float iz, out bool attack, out bool restart)
        {
            ix = 0f; iz = 0f; attack = false; restart = false;
#if ENABLE_LEGACY_INPUT_MANAGER
            ix = Input.GetAxisRaw("Horizontal"); iz = Input.GetAxisRaw("Vertical");
            Vector2 mp = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                if (mp.x < Screen.width * 0.5f) { JoyActive = true; JoyOrigin = mp; } else { attack = true; restart = true; }
            }
            if (Input.GetMouseButtonUp(0)) JoyActive = false;
            if (JoyActive && Input.GetMouseButton(0))
            {
                JoyCurrent = mp;
                float radius = Screen.dpi > 0f ? Screen.dpi * 0.35f : 110f;
                Vector2 d = Vector2.ClampMagnitude((mp - JoyOrigin) / radius, 1f);
                ix += d.x; iz += d.y;
            }
            if (Input.GetKeyDown(KeyCode.Space)) attack = true;
            if (Input.GetKeyDown(KeyCode.R)) restart = true;
            if (Input.GetKeyDown(KeyCode.G)) { ShowGoldenPath = !ShowGoldenPath; _viewFloor = -1; }
#else
            Error = "Enable Project Settings > Player > Active Input Handling = Input Manager (Old) or Both.";
#endif
        }
    }
}
