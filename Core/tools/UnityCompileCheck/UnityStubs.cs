// Minimal stand-in for the UnityEngine API subset used by UnityClient/Assets/_Project/Scripts.
// PURPOSE: catch typos and misuse of the Core API without a Unity install. It does NOT prove the real
// Unity signatures - always open the project in Unity 6 once to confirm.
using System;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void Destroy(Object o) { }
        public static T FindFirstObjectByType<T>() where T : Object { return null; }
    }
    public class Component : Object
    {
        public Transform transform; public GameObject gameObject;
        public T GetComponent<T>() where T : class { return null; }
    }
    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { }
    public sealed class GameObject : Object
    {
        public Transform transform; public string tag;
        public GameObject() { } public GameObject(string n) { name = n; }
        public static GameObject CreatePrimitive(PrimitiveType t) { return null; }
        public T AddComponent<T>() where T : Component { return null; }
        public T GetComponent<T>() where T : class { return null; }
        public void SetActive(bool b) { }
    }
    public sealed class Transform : Component
    {
        public Vector3 position, localPosition, localScale; public Quaternion rotation;
        public void SetParent(Transform p, bool worldPositionStays) { }
    }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube }
    public struct Vector2
    {
        public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator /(Vector2 a, float d) { return new Vector2(a.x / d, a.y / d); }
        public static Vector2 ClampMagnitude(Vector2 v, float m) { return v; }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
    }
    public struct Vector3
    {
        public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 one { get { return new Vector3(1, 1, 1); } }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y + z * z); } }
        public float sqrMagnitude { get { return x * x + y * y + z * z; } }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator *(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { return a; }
    }
    public struct Quaternion
    {
        public static Quaternion Euler(float x, float y, float z) { return default(Quaternion); }
        public static Quaternion LookRotation(Vector3 v) { return default(Quaternion); }
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) { return a; }
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1); } }
        public static Color green { get { return new Color(0, 1, 0); } }
        public static Color Lerp(Color a, Color b, float t) { return a; }
    }
    public class Material : Object { public Material(Material m) { } public Color color; }
    public class Renderer : Component { public Material sharedMaterial, material; }
    public class Collider : Component { }
    public enum CameraClearFlags { SolidColor }
    public class Camera : Behaviour { public static Camera main; public CameraClearFlags clearFlags; public Color backgroundColor; public float fieldOfView; }
    public class AudioListener : Behaviour { }
    public enum LightType { Directional }
    public class Light : Behaviour { public LightType type; }
    public class TextAsset : Object { public string text; }
    public static class Resources { public static T Load<T>(string path) where T : Object { return null; } }
    public enum ScreenOrientation { LandscapeLeft }
    public static class Screen { public static int width, height; public static float dpi; public static ScreenOrientation orientation; }
    public static class Application { public static int targetFrameRate; }
    public static class Time { public static float deltaTime; }
    public static class Mathf
    {
        public static float Min(float a, float b) { return Math.Min(a, b); }
        public static int Max(int a, int b) { return Math.Max(a, b); }
        public static float Clamp01(float v) { return v; }
        public static float Exp(float v) { return (float)Math.Exp(v); }
        public static float Lerp(float a, float b, float t) { return a; }
        public static int CeilToInt(float v) { return (int)Math.Ceiling(v); }
    }
    public enum KeyCode { Space, R, G }
    public static class Input
    {
        public static Vector3 mousePosition;
        public static float GetAxisRaw(string a) { return 0; }
        public static bool GetMouseButtonDown(int b) { return false; }
        public static bool GetMouseButtonUp(int b) { return false; }
        public static bool GetMouseButton(int b) { return false; }
        public static bool GetKeyDown(KeyCode k) { return false; }
    }
    public struct Rect { public float x, y, width, height; public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; } }
    public enum TextAnchor { MiddleCenter }
    public class GUIStyleState { public Color textColor; }
    public class GUIStyle { public int fontSize; public TextAnchor alignment; public GUIStyleState normal = new GUIStyleState(); public GUIStyle(GUIStyle s) { } }
    public class GUISkin { public GUIStyle label; }
    public class Texture2D : Object { public static Texture2D whiteTexture; }
    public static class GUI { public static GUISkin skin; public static Color color; public static void Label(Rect r, string t, GUIStyle s) { } public static void DrawTexture(Rect r, Texture2D t) { } }
    public enum RuntimeInitializeLoadType { AfterSceneLoad }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
}
