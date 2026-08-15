using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerMovementAudio : MonoBehaviour
{
    private enum FootstepMode
    {
        None,
        Walk,
        Run
    }

    [Header("Footsteps")]
    [SerializeField] private AudioCue walkLoopCue;
    [SerializeField] private AudioCue runLoopCue;
    [SerializeField, Min(0f)] private float minimumPlanarSpeed = 0.2f;

    [Header("Dash")]
    [SerializeField] private AudioCue dashCue;

    private CharacterController characterController;
    private PlayerController playerController;
    private AudioHandle footstepHandle = AudioHandle.Invalid;
    private AudioHandle dashHandle = AudioHandle.Invalid;
    private FootstepMode currentFootstepMode;
    private bool wasDashing;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        wasDashing = playerController != null && playerController.IsDashing;
    }

    private void Update()
    {
        if (characterController == null || playerController == null)
        {
            StopFootsteps();
            return;
        }

        bool isDashing = playerController.IsDashing;
        if (isDashing && !wasDashing && dashCue != null && AudioManager.Instance != null)
        {
            StopDash();
            dashHandle = AudioManager.Instance.PlayAtTracked(dashCue, transform.position);
        }
        else if (!isDashing && wasDashing)
        {
            StopDash();
        }

        wasDashing = isDashing;
        SetFootstepMode(DetermineFootstepMode(isDashing));
    }

    private void OnDisable()
    {
        StopFootsteps();
        StopDash();
    }

    private FootstepMode DetermineFootstepMode(bool isDashing)
    {
        Vector3 planarVelocity = characterController.velocity;
        planarVelocity.y = 0f;

        bool canPlayFootsteps = characterController.isGrounded &&
            !isDashing &&
            !playerController.isFlying &&
            planarVelocity.sqrMagnitude >= minimumPlanarSpeed * minimumPlanarSpeed;

        if (!canPlayFootsteps)
        {
            return FootstepMode.None;
        }

        return playerController.isRunning ? FootstepMode.Run : FootstepMode.Walk;
    }

    private void SetFootstepMode(FootstepMode desiredMode)
    {
        if (desiredMode == currentFootstepMode)
        {
            return;
        }

        StopFootsteps();
        AudioCue cue = desiredMode switch
        {
            FootstepMode.Walk => walkLoopCue,
            FootstepMode.Run => runLoopCue,
            _ => null
        };

        if (cue == null || AudioManager.Instance == null)
        {
            return;
        }

        footstepHandle = AudioManager.Instance.PlayLoop(cue, transform);
        if (footstepHandle.IsValid)
        {
            currentFootstepMode = desiredMode;
        }
    }

    private void StopFootsteps()
    {
        if (footstepHandle.IsValid && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoop(footstepHandle);
        }

        footstepHandle = AudioHandle.Invalid;
        currentFootstepMode = FootstepMode.None;
    }

    private void StopDash()
    {
        if (dashHandle.IsValid && AudioManager.Instance != null)
        {
            AudioManager.Instance.Stop(dashHandle);
        }

        dashHandle = AudioHandle.Invalid;
    }
}
