#if WEB_BUILD
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class LocalPlayerSync : MonoBehaviour
{
    [Tooltip("Send rate in Hz; should match server TICK_RATE_HZ (60). Higher = smoother remote view but more bandwidth.")]
    public float sendRateHz = 60f;

    private Rigidbody rb;
    private PlayerInput input;
    private PlayerMovement movement;
    private ParkourController parkour;
    private float sendInterval;
    private float timeSinceLastSend;

    // One-shot animation trigger queue. Filled by PlayerAnimator/ParkourController when the
    // local player jumps/slides/vaults; flushed into the next outgoing input message.
    private int _actionSeq;
    private int _pendingActionId;

    public const int ACTION_JUMP = 1;
    public const int ACTION_MANUAL_SLIDE = 2;
    public const int ACTION_VAULT = 3;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<PlayerInput>();
        movement = GetComponent<PlayerMovement>();
        parkour = GetComponent<ParkourController>();
        sendInterval = 1f / Mathf.Max(1f, sendRateHz);
    }

    public void QueueAction(int actionId)
    {
        _pendingActionId = actionId;
        _actionSeq++;
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
            isSliding = movement != null && movement.IsInSlopeZone,
            horizontalInput = input.HorizontalInput,
            isManuallySliding = parkour != null && parkour.isManuallySliding,
            isLandingHard = movement != null && movement.isLandingHard,
            actionSeq = _actionSeq,
            actionId = _pendingActionId,
        };
        NetworkManager.Instance.SendInput(payload);
    }
}
#endif
