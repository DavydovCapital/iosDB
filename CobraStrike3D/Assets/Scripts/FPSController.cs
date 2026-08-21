using UnityEngine;

public class FPSController : MonoBehaviour
{
    public float moveSpeed = 6.5f;
    public float aimSpeed = 3.5f;
    public float lookSensitivity = 0.12f;
    public float gravity = -20f;
    public float jumpForce = 7.5f;
    public Transform cameraRoot;

    private CharacterController cc;
    private float pitch;
    private float verticalVelocity;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Vector2 look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        transform.Rotate(Vector3.up, look.x * lookSensitivity);
        pitch = Mathf.Clamp(pitch - look.y * lookSensitivity, -75f, 75f);
        cameraRoot.localEulerAngles = new Vector3(pitch, 0, 0);

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool aiming = Input.GetMouseButton(1);
        float speed = aiming ? aimSpeed : moveSpeed;
        Vector3 move = (transform.right * h + transform.forward * v).normalized * speed;

        if (cc.isGrounded)
        {
            verticalVelocity = -1f;
            if (Input.GetButtonDown("Jump")) verticalVelocity = jumpForce;
        }
        else verticalVelocity += gravity * Time.deltaTime;

        cc.Move((move + Vector3.up * verticalVelocity) * Time.deltaTime);
    }
}
