using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// Look-around only (PIC 2). The reader can turn the head 360 degrees (left/right) and
/// look up/down with the mouse, but can never move. Attach to the Main Camera.
///
///   Mouse        : look around (the cursor is locked to the Game view)
///   Esc / L      : release the cursor / lock it again (no mouse click is ever used)
///   H / 1-4      : jump the HEAD ROTATION to a preset view: H home, 1 bar chart (12 o'clock),
///                  2 world map (floor), 3 event timeline (2 o'clock), 4 altitude scatter (10 o'clock).
///                  Only the rotation changes, the position never does. Handy for screenshots.
[DisallowMultipleComponent]
public class LookAroundOnly : MonoBehaviour
{
    [Header("Mouse look")]
    public float sensitivity = 0.12f;          // degrees per mouse count
    public float minPitch = -85f;              // look up limit   (negative = up)
    public float maxPitch = 85f;               // look down limit (positive = down)
    public bool invertY = false;
    public bool lockCursor = true;

    [Header("Preset views (yaw, pitch in degrees; pitch > 0 looks down)")]
    public bool enablePresets = true;
    public Vector2 homeView = new Vector2(0f, 10f);         // H : 12 o'clock, bars above + map below
    public Vector2 barsView = new Vector2(0f, -8f);         // 1 : V1 bar chart (eye height, 3 m)
    public Vector2 mapView = new Vector2(0f, 40f);          // 2 : V2 world map on the floor (2 m)
    public Vector2 timelineView = new Vector2(60f, 18f);    // 3 : V3 event timeline (2 o'clock, waist)
    public Vector2 scatterView = new Vector2(-60f, 18f);    // 4 : V4 altitude scatter (10 o'clock, waist)

    float yaw, pitch;
    Vector3 homePosition;

    void Awake() { homePosition = transform.position; }

    void OnEnable()
    {
        SetView(homeView);
        if (lockCursor) SetLock(true);
    }

    void OnDisable() { if (lockCursor) SetLock(false); }

    void Update()
    {
        Vector2 delta = Vector2.zero;
        bool locked = !lockCursor || Cursor.lockState == CursorLockMode.Locked;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var ms = Mouse.current;
        if (kb != null)
        {
            if (kb.escapeKey.wasPressedThisFrame) SetLock(false);
            if (kb.lKey.wasPressedThisFrame && lockCursor) SetLock(true);
            if (enablePresets)
            {
                if (kb.hKey.wasPressedThisFrame) SetView(homeView);
                if (kb.digit1Key.wasPressedThisFrame) SetView(barsView);
                if (kb.digit2Key.wasPressedThisFrame) SetView(mapView);
                if (kb.digit3Key.wasPressedThisFrame) SetView(timelineView);
                if (kb.digit4Key.wasPressedThisFrame) SetView(scatterView);
            }
        }
        if (ms != null && locked) delta = ms.delta.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape)) SetLock(false);
        if (enablePresets)
        {
            if (Input.GetKeyDown(KeyCode.H)) SetView(homeView);
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetView(barsView);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetView(mapView);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetView(timelineView);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetView(scatterView);
        }
        if (Input.GetKeyDown(KeyCode.L) && lockCursor) SetLock(true);
        if (locked) delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 8f;
#endif

        yaw += delta.x * sensitivity;                                   // free 360 degrees
        pitch += (invertY ? 1f : -1f) * delta.y * sensitivity;          // mouse up = look up
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        yaw = Mathf.Repeat(yaw, 360f);
        Apply();
    }

    // The reader never moves, whatever other scripts try to do.
    void LateUpdate() { transform.position = homePosition; }

    void SetView(Vector2 v) { yaw = v.x; pitch = Mathf.Clamp(v.y, minPitch, maxPitch); Apply(); }
    void Apply() { transform.rotation = Quaternion.Euler(pitch, yaw, 0f); }

    static void SetLock(bool on)
    {
        Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !on;
    }
}
