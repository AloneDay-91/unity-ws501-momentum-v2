#if WEB_BUILD
using System;
using UnityEngine;

public class NetworkPlayer : MonoBehaviour
{
    [Tooltip("Higher = snappier; lower = smoother. Tweak after testing.")]
    public float interpolationSpeed = 15f;

    private PlayerState state;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool bound = false;
    private Action _removeOnChange;

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

        var parkour = GetComponent<ParkourController>();
        if (parkour != null) parkour.enabled = false;

        // Subscribe to instance changes via Colyseus 0.17 callbacks API
        if (NetworkManager.Instance != null && NetworkManager.Instance.Callbacks != null)
        {
            _removeOnChange = NetworkManager.Instance.Callbacks.OnChange(state, OnStateChanged);
        }
        bound = true;
    }

    private void OnStateChanged()
    {
        targetPosition = new Vector3(state.posX, state.posY, state.posZ);
        targetRotation = Quaternion.Euler(0, state.rotY, 0);
    }

    void Update()
    {
        if (!bound || state == null) return;

        transform.position = Vector3.Lerp(transform.position, targetPosition, interpolationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);
    }

    void OnDestroy()
    {
        _removeOnChange?.Invoke();
        _removeOnChange = null;
    }
}
#endif
