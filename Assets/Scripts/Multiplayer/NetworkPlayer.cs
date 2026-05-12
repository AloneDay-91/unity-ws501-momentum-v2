#if WEB_BUILD
using UnityEngine;
using System.Collections.Generic;
using Colyseus.Schema;

public class NetworkPlayer : MonoBehaviour
{
    [Tooltip("Higher = snappier; lower = smoother. Tweak after testing.")]
    public float interpolationSpeed = 15f;

    private PlayerState state;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool bound = false;

    public void Bind(PlayerState playerState)
    {
        state = playerState;

        targetPosition = new Vector3(state.posX, state.posY, state.posZ);
        targetRotation = Quaternion.Euler(0, state.rotY, 0);
        transform.position = targetPosition;
        transform.rotation = targetRotation;

        // Disable local-input / local-physics on this GameObject — driven by the network
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        var input = GetComponent<PlayerInput>();
        if (input != null) input.enabled = false;

        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        // ParkourController is in the same assembly — use typed lookup
        var parkour = GetComponent<ParkourController>();
        if (parkour != null) parkour.enabled = false;

        // Subscribe to server state changes
        state.OnChange += HandleStateChange;
        bound = true;
    }

    private void HandleStateChange(List<DataChange> changes)
    {
        targetPosition = new Vector3(state.posX, state.posY, state.posZ);
        targetRotation = Quaternion.Euler(0, state.rotY, 0);
    }

    void Update()
    {
        if (!bound || state == null) return;

        transform.position = Vector3.Lerp(transform.position, targetPosition, interpolationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);

        // Animation flag forwarding via PlayerAnimator is deferred:
        // PlayerAnimator reads from PlayerInput/PlayerMovement directly, so we would need
        // explicit public setters there (e.g., SetGrounded(bool), SetHorizontalInput(float))
        // before driving it from network state. Add those setters when ready.
    }

    void OnDestroy()
    {
        if (state != null && bound)
        {
            state.OnChange -= HandleStateChange;
        }
    }
}
#endif
