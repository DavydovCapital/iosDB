using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CombatInput : MonoBehaviour
{
    public static CombatInput Instance { get; private set; }

    public Vector2 Move { get; set; }
    public Vector2 Look { get; private set; }
    public bool Fire { get; set; }
    public bool Ads { get; set; }
    public bool Reload { get; set; }
    public bool Jump { get; set; }

    public Vector2 TouchLook;
    public bool TouchFire;
    public bool TouchAds;
    public bool TouchReload;
    public Vector2 TouchMove;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        Vector2 move = TouchMove;
        Vector2 look = TouchLook;
        bool fire = TouchFire;
        bool ads = TouchAds;
        bool reload = TouchReload;
        bool jump = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb != null)
        {
            Vector2 k = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) k.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) k.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) k.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) k.x -= 1f;
            if (k.sqrMagnitude > 0.01f) move = k;
            if (kb.rKey.wasPressedThisFrame) reload = true;
            if (kb.spaceKey.wasPressedThisFrame) jump = true;
            ads = ads || kb.leftShiftKey.isPressed;
        }
        if (mouse != null)
        {
            look += mouse.delta.ReadValue() * 0.12f;
            bool overUi = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
            if (mouse.leftButton.isPressed && !overUi) fire = true;
            if (mouse.rightButton.isPressed) ads = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        Vector2 legacy = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (legacy.sqrMagnitude > 0.01f) move = legacy;
        look += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 2.6f;
        if (Input.GetKeyDown(KeyCode.R)) reload = true;
        if (Input.GetButtonDown("Jump")) jump = true;
        if (Input.GetMouseButton(1)) ads = true;
        bool ui = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButton(0) && !ui) fire = true;
#endif

        if (move.sqrMagnitude > 1f) move.Normalize();
        Move = move;
        Look = look;
        Fire = fire;
        Ads = ads;
        Reload = reload;
        Jump = jump;
        TouchLook = Vector2.zero;
    }
}
