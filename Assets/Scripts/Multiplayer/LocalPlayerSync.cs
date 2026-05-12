#if WEB_BUILD
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class LocalPlayerSync : MonoBehaviour
{
    [Tooltip("Send rate in Hz; should match server TICK_RATE_HZ (20).")]
    public float sendRateHz = 20f;

    private Rigidbody rb;
    private PlayerInput input;
    private PlayerMovement movement;
    private float sendInterval;
    private float timeSinceLastSend;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<PlayerInput>();
        movement = GetComponent<PlayerMovement>();
        sendInterval = 1f / Mathf.Max(1f, sendRateHz);
    }

    void Update()
    {
        timeSinceLastSend += Time.deltaTime;
        if (timeSinceLastSend < sendInterval) return;
        timeSinceLastSend = 0f;

        if (NetworkManager.Instance == null || NetworkManager.Instance.Room == null) return;

        var payload = new PlayerInputPayload
        {
            posX = transform.position.x,
            posY = transform.position.y,
            posZ = transform.position.z,
            velX = rb.velocity.x,
            velY = rb.velocity.y,
            velZ = rb.velocity.z,
            rotY = transform.rotation.eulerAngles.y,
            isGrounded = movement != null && movement.IsPhysicallyGrounded,
            isSliding = false, // TODO: wire up when ParkourController is integrated (M2.7+)
            horizontalInput = input.HorizontalInput,
        };
        NetworkManager.Instance.SendInput(payload);
    }
}
#endif
