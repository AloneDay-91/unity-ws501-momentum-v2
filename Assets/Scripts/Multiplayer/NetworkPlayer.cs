#if WEB_BUILD
using System;
using UnityEngine;

public class NetworkPlayer : MonoBehaviour
{
    [Tooltip("Higher = snappier; lower = smoother. Tweak after testing.")]
    public float interpolationSpeed = 45f;

    [Tooltip("If we are further than this from the target, snap instantly instead of lerping. Prevents long visible teleports when a packet is delayed and the remote player is now far away.")]
    public float snapDistance = 4f;

    private PlayerState state;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool bound = false;
    private bool _hasServerPosition = false;
    private Action _removeOnChange;
    private bool _wasAlive = true;
    private int _playerNumber;
    private int _lastActionSeq = 0;

    // Animator driving (replaces the disabled PlayerAnimator component on the remote clone).
    // PlayerAnimator reads from PlayerInput/PlayerMovement which we disable here, so it would
    // stick the remote in the idle state. We mirror those reads from the network PlayerState.
    private Animator animator;
    private Transform modelTransform;
    private static readonly Quaternion FacingRight = Quaternion.Euler(0, 90, 0);
    private static readonly Quaternion FacingLeft = Quaternion.Euler(0, -90, 0);

    public void Bind(PlayerState playerState)
    {
        state = playerState;
        _playerNumber = (int)state.playerNumber;
        _wasAlive = state.isAlive;
        _lastActionSeq = (int)state.actionSeq;

        // Server PlayerState defaults pos to (0,0,0) and only writes it while status="playing".
        // During waiting/countdown we'd teleport the remote clone to world origin and only see
        // it lerp to the real spawn at the last moment — the prefab is already at the right
        // start position, so keep it until the server pushes a real position.
        if (HasServerPosition(state))
        {
            targetPosition = new Vector3(state.posX, state.posY, state.posZ);
            targetRotation = Quaternion.Euler(0, state.rotY, 0);
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            _hasServerPosition = true;
        }
        else
        {
            targetPosition = transform.position;
            targetRotation = transform.rotation;
        }

        // Disable local-input / local-physics on this GameObject — driven by the network
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        var input = GetComponent<PlayerInput>();
        if (input != null) input.enabled = false;

        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        var parkour = GetComponent<ParkourController>();
        if (parkour != null) parkour.enabled = false;

        // PlayerAnimator reads from the components we just disabled; take over manually.
        var pa = GetComponent<PlayerAnimator>();
        if (pa != null) pa.enabled = false;

        // Cache animator + model transform (model is the animator's GameObject by convention here)
        animator = GetComponentInChildren<Animator>(includeInactive: true);
        if (animator != null) modelTransform = animator.transform;

        // Subscribe to instance changes via Colyseus 0.17 callbacks API
        if (NetworkManager.Instance != null && NetworkManager.Instance.Callbacks != null)
        {
            _removeOnChange = NetworkManager.Instance.Callbacks.OnChange(state, OnStateChanged);
        }
        bound = true;
    }

    private void OnStateChanged()
    {
        // First valid server position: snap, then lerp from there. Avoids the visible
        // "(0,0,0) → spawn" travel that made the remote player appear at the last second.
        if (HasServerPosition(state))
        {
            targetPosition = new Vector3(state.posX, state.posY, state.posZ);
            targetRotation = Quaternion.Euler(0, state.rotY, 0);
            if (!_hasServerPosition)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                _hasServerPosition = true;
            }
        }

        // Remote death detection: when isAlive flips, mirror locally so the elimination
        // overlay + ScoreManager react the same way they would for the local player.
        if (_wasAlive && !state.isAlive)
        {
            _wasAlive = false;
            if (GameManager.Instance != null)
            {
                int score = ScoreManager.Instance != null ? ScoreManager.Instance.GetPlayerScore(_playerNumber) : (int)state.score;
                GameManager.Instance.OnPlayerEliminated(_playerNumber, score);
            }
        }

        // One-shot animation triggers (jump/slide/vault). Fire the matching animator
        // trigger every time actionSeq advances.
        int seq = (int)state.actionSeq;
        if (seq != _lastActionSeq)
        {
            _lastActionSeq = seq;
            if (animator != null)
            {
                int actionId = (int)state.actionId;
                switch (actionId)
                {
                    case 1: animator.SetTrigger("doJump"); break;
                    case 2: animator.SetTrigger("doManualSlide"); break;
                    case 3: animator.SetTrigger("doVault"); break;
                }
            }
        }
    }

    void Update()
    {
        if (!bound || state == null) return;

        // Skip movement interpolation until the server has actually placed us — otherwise
        // we lerp toward the prefab's own position which is fine, but we'd also start
        // animating with stale animator params before the match is live.
        if (_hasServerPosition)
        {
            // If the remote is way off (lost packets, big jump, respawn), snap instead of
            // lerping for half a second across the map — that's the "teleport" the user sees.
            float dist = Vector3.Distance(transform.position, targetPosition);
            if (dist > snapDistance)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, interpolationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);
            }
        }

        // Drive animator params from network state, matching what PlayerAnimator does locally
        if (animator != null)
        {
            animator.SetFloat("moveSpeed", Mathf.Abs(state.horizontalInput));
            animator.SetBool("isGrounded", state.isGrounded);
            animator.SetBool("isSliding", state.isSliding);
            animator.SetBool("isLandingHard", state.isLandingHard);
        }

        // Mirror PlayerAnimator.HandleFlipping using network horizontalInput
        if (modelTransform != null)
        {
            if (state.horizontalInput > 0.1f) modelTransform.localRotation = FacingRight;
            else if (state.horizontalInput < -0.1f) modelTransform.localRotation = FacingLeft;
        }
    }

    void OnDestroy()
    {
        _removeOnChange?.Invoke();
        _removeOnChange = null;
    }

    private static bool HasServerPosition(PlayerState s)
    {
        return Mathf.Abs(s.posX) > 0.0001f || Mathf.Abs(s.posY) > 0.0001f || Mathf.Abs(s.posZ) > 0.0001f;
    }
}
#endif
