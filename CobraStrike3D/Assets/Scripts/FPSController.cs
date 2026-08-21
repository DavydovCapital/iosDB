using UnityEngine;

public class FPSController : MonoBehaviour
{
    public float moveSpeed = 8.4f;
    public float aimSpeed = 6.2f;
    public float lookSensitivity = 1f;
    public float gravity = -28f;
    public float jumpForce = 8f;
    public Transform cameraRoot;

    CharacterController cc;
    float pitch;
    float verticalVelocity;
    float bob;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (Application.isMobilePlatform) Cursor.lockState = CursorLockMode.None;
        else Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = Application.isMobilePlatform;
    }

    void Update()
    {
        var input = CombatInput.Instance;
        if (!input || !cc) return;
        if (HUD.Instance && HUD.Paused) return;

        Vector2 look = input.Look * lookSensitivity;
        transform.Rotate(Vector3.up, look.x, Space.World);
        pitch = Mathf.Clamp(pitch - look.y, -78f, 78f);
        if (cameraRoot) cameraRoot.localEulerAngles = new Vector3(pitch, 0f, 0f);

        Vector2 m = input.Move;
        bool ads = input.Ads;
        float speed = ads ? aimSpeed : moveSpeed;
        Vector3 wish = transform.right * m.x + transform.forward * m.y;
        if (wish.sqrMagnitude > 1f) wish.Normalize();
        wish *= speed;

        if (cc.isGrounded)
        {
            verticalVelocity = -2f;
            if (input.Jump) verticalVelocity = jumpForce;
        }
        else verticalVelocity += gravity * Time.deltaTime;

        cc.Move((wish + Vector3.up * verticalVelocity) * Time.deltaTime);

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, -36f, 36f);
        p.z = Mathf.Clamp(p.z, -72f, 18f);
        if (p != transform.position) transform.position = p;

        bob += wish.magnitude * Time.deltaTime;
        if (cameraRoot && wish.sqrMagnitude > 0.2f && !ads)
            cameraRoot.localPosition = new Vector3(Mathf.Sin(bob * 9f) * 0.03f, 1.62f + Mathf.Abs(Mathf.Sin(bob * 18f)) * 0.025f, 0f);
        else if (cameraRoot)
            cameraRoot.localPosition = Vector3.Lerp(cameraRoot.localPosition, new Vector3(0f, 1.62f, 0f), 12f * Time.deltaTime);
    }
}
