using System.Collections.Generic;
using Entities.Netcode;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class PlayerVisualisation : MonoBehaviour
{
    public bool isLocalPlayer;
    public GameObject body;
    public GameObject visor;
    public GameObject balls;

    [Header("Overhead UI")] public UIDocument overheadUI;

    public LayerMask environmentLayer; // Set this to your level/environment layer in the Inspector!

    [Header("Speed FX")] public ParticleSystem windParticles;

    public CinemachineCamera virtualCamera; // Link your vcam here!

    [Tooltip("Velocity required to start showing wind and increasing FOV")]
    public float minSpeedThreshold = 8f;

    [Tooltip("Velocity where FX are at absolute maximum")]
    public float maxSpeedThreshold = 25f;

    public float spawnDistanceFront = 15f;

    [Header("FOV")] public float baseFOV = 60f;

    public float maxFOV = 90f;
    public float fovLerpSpeed = 5f;
    private float _footstepTimer;
    private VisualElement _healthFill;
    private Label _healthLabel;
    private float _lastHealth = 100f;
    private Camera _mainCamera;
    private Label _nameLabel;

    private VisualElement _overheadRoot;
    public PlayerStateComponent PlayerState;

    private void Start()
    {
        _mainCamera = Camera.main;
        if (virtualCamera == null) virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        if (isLocalPlayer)
        {
            foreach (Transform childTransform in transform)
                if (childTransform.gameObject.TryGetComponent<Renderer>(out var r))
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            if (overheadUI != null) overheadUI.gameObject.SetActive(false);
            return;
        }

        if (overheadUI != null)
        {
            _overheadRoot = overheadUI.rootVisualElement;
            _nameLabel = _overheadRoot.Q<Label>("player-name");
            _healthFill = _overheadRoot.Q<VisualElement>("health-fill");
            _healthLabel = _overheadRoot.Q<Label>("health-text-overhead");
        }
    }

    private void Update()
    {
        // --- 1. LOCAL TAKE DAMAGE SOUND ---
        if (isLocalPlayer)
        {
            if (PlayerState.Health < _lastHealth && !PlayerState.IsDead) AudioManager.Instance.Play2D(SFX.TakeDamage);
            _lastHealth = PlayerState.Health;
        }

        // --- 2. FOOTSTEPS (For everyone) ---
        // Only play if they are moving fast enough (ignoring Y falling velocity)
        var horizontalVel = new float2(PlayerState.Velocity.x, PlayerState.Velocity.z);
        var speed = math.length(horizontalVel);

        if (speed > 1f && !PlayerState.IsDead && (PlayerState.IsGrounded || PlayerState.IsWallRunning))
        {
            _footstepTimer -= Time.deltaTime * speed; // Frequency scales with velocity
            if (_footstepTimer <= 0)
            {
                AudioManager.Instance.Play3D(SFX.Footstep, transform.position, Random.Range(0.9f, 2.1f));
                _footstepTimer = 3f; // Base distance threshold before next step
            }
        }
        else
        {
            _footstepTimer = 0f; // Reset when stopped
        }

        if (!isLocalPlayer)
        {
            UpdateRemotePlayerVisuals();
            return;
        }

        PlayerVisualisationManager.LocalPlayer = PlayerState;

        // Ensure we don't see our own body casting shadows when dead
        var bodyRenderer = body.GetComponent<Renderer>();
        var visorRenderer = visor.GetComponent<Renderer>();
        var ballsRenderer = balls.GetComponent<Renderer>();
        bodyRenderer.enabled = !PlayerState.IsDead;
        visorRenderer.enabled = !PlayerState.IsDead;
        ballsRenderer.enabled = !PlayerState.IsDead;

        UpdateSpeedEffects();
    }

    private void OnGUI()
    {
    }

    private void UpdateSpeedEffects()
    {
        if (windParticles == null || virtualCamera == null) return;

        // 1. Calculate 3D Speed (Include Y for falling!)
        var speed = math.length(PlayerState.Velocity);

        // Zero out the effects if dead
        if (PlayerState.IsDead) speed = 0f;

        // 2. Normalize speed between our thresholds (0.0 to 1.0)
        var speedFactor = Mathf.Clamp01((speed - minSpeedThreshold) / (maxSpeedThreshold - minSpeedThreshold));

        // 3. Update Cinemachine FOV
        var targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedFactor);
        virtualCamera.Lens.FieldOfView =
            Mathf.Lerp(virtualCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);

        // 4. Update Particle Spawner
        var emission = windParticles.emission;
        var main = windParticles.main;

        if (speedFactor > 0f)
        {
            emission.enabled = true;

            // Scale emission rate and particle speed based on how fast you are going
            emission.rateOverTime = Mathf.Lerp(0f, 50f, speedFactor);
            main.startSpeed = Mathf.Lerp(20f, 60f, speedFactor);

            // Calculate the exact 3D direction vector
            var moveDirection = new Vector3(PlayerState.Velocity.x, PlayerState.Velocity.y, PlayerState.Velocity.z) /
                                speed;

            // Place the spawner IN FRONT of the player, and rotate it to shoot BACK at the player
            windParticles.transform.position = transform.position + moveDirection * spawnDistanceFront;
            windParticles.transform.rotation = Quaternion.LookRotation(-moveDirection);
        }
        else
        {
            // Stop emitting, but let existing particles finish their lifespan
            emission.enabled = false;
        }
    }

    private void UpdateRemotePlayerVisuals()
    {
        var bodyRenderer = body.GetComponent<Renderer>();
        var visorRenderer = visor.GetComponent<Renderer>();
        var ballsRenderer = balls.GetComponent<Renderer>();
// --- 1. INSTANT INVISIBILITY ---
        if (PlayerState.IsDead)
        {
            bodyRenderer.enabled = false;
            visorRenderer.enabled = false;
            ballsRenderer.enabled = false;
            if (_overheadRoot != null) _overheadRoot.style.display = DisplayStyle.None;
            return;
        }

        bodyRenderer.enabled = true; // Turn back on when alive
        ballsRenderer.enabled = true;
        visorRenderer.enabled = true;
        var isEnemy = PlayerState.TeamIndex != PlayerVisualisationManager.LocalPlayer?.TeamIndex;

        var materials = new List<Material>
        {
            isEnemy
                ? PlayerVisualisationManager.Instance.enemyMaterial
                : PlayerVisualisationManager.Instance.friendMaterial
        };
        bodyRenderer.SetMaterials(materials);

        if (overheadUI == null || _overheadRoot == null || _mainCamera == null) return;

        var uiTransform = overheadUI.transform;
        uiTransform.LookAt(uiTransform.position + _mainCamera.transform.rotation * Vector3.forward,
            _mainCamera.transform.rotation * Vector3.up);

        var isVisible = !Physics.Linecast(_mainCamera.transform.position, uiTransform.position, environmentLayer);

        _overheadRoot.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;

        if (!isVisible) return;

        _nameLabel.text = $"ID: {PlayerState.NetworkId}";
        _nameLabel.style.color =
            isEnemy ? new StyleColor(Color.red) : new StyleColor(new Color(0.2f, 0.6f, 1f)); // Blue for ally
        _healthFill.style.backgroundColor = isEnemy ? new StyleColor(Color.red) : new StyleColor(Color.white);

        var hpPercent = Mathf.Clamp01(PlayerState.Health / 100f) * 100f;
        _healthFill.style.width = new Length(hpPercent, LengthUnit.Percent);
        _healthLabel.text = $"{PlayerState.Health}/100";
    }
}