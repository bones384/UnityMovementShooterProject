using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PlayerHUDManager : MonoBehaviour
{
    private const int MAG_SIZE = 30; // Match your EntitiesReferences default
    private const float MAX_HEALTH = 100f; // Replace if added to ECS state
    private Label _ammoText;
    private VisualElement _crosshair;
    private VisualElement _deathOverlay;
    private Label _deathTimerText;
    private AbilityUI _fourthUI;

    // UI Elements
    private VisualElement _healthBarFill;
    private Label _healthText;
    private VisualElement _hitmarker;
    private AbilityUI _primaryUI;
    private AbilityUI _secondaryUI;
    private AbilityUI _thirdUI;
    private UIDocument _uiDocument;

    private void Update()
    {
        // If the LocalPlayer is not yet initialized or dead, optionally hide UI
        if (PlayerVisualisationManager.LocalPlayer == null)
        {
            _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
            return;
        }

        _uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;

        var state = PlayerVisualisationManager.LocalPlayer.Value;
        _ammoText.text = $"{state.CurrentAmmo} / {MAG_SIZE}";
        if (state.CurrentAmmo == 0)
            _ammoText.style.color = new StyleColor(new Color(1f, 0.3f, 0.3f));
        else
            _ammoText.style.color = new StyleColor(Color.white);
        // Toggle Death Screen

        if (state.HitMarkerTimer > 0)
        {
            _hitmarker.style.display = DisplayStyle.Flex;
            if (state.WasLastHitFatal)
                _hitmarker.AddToClassList("hitmaker-fetal");
            else _hitmarker.RemoveFromClassList("hitmaker-fetal");
        }
        else
        {
            _hitmarker.style.display = DisplayStyle.None;
        }

        if (state.IsDead)
        {
            _deathOverlay.style.display = DisplayStyle.Flex;
            _crosshair.style.display = DisplayStyle.None;
            _deathTimerText.text = $"Respawning in {Mathf.CeilToInt(state.RespawnTimer)}...";
        }
        else
        {
            _deathOverlay.style.display = DisplayStyle.None;
            _crosshair.style.display = DisplayStyle.Flex;
        }

        UpdateHealth(state.Health);

        UpdateAbilitySlot(_primaryUI, state.PrimaryCooldownTimer);
        UpdateAbilitySlot(_secondaryUI, state.SecondaryCooldownTimer);
        UpdateAbilitySlot(_thirdUI, state.ThirdCooldownTimer);
        UpdateAbilitySlot(_fourthUI, state.FourthCooldownTimer);
    }

    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        var root = _uiDocument.rootVisualElement;

        // Query Health UI
        _healthBarFill = root.Q<VisualElement>("health-bar-fill");
        _healthText = root.Q<Label>("health-text");

        // Query Ability UI
        _primaryUI = GetAbilityUI(root, "primary");
        _secondaryUI = GetAbilityUI(root, "secondary");
        _thirdUI = GetAbilityUI(root, "third");
        _fourthUI = GetAbilityUI(root, "fourth");
        _ammoText = root.Q<Label>("ammo-text");
        _crosshair = root.Q<VisualElement>("crosshair");
        _deathOverlay = root.Q<VisualElement>("death-overlay");
        _deathTimerText = root.Q<Label>("death-timer-text");
        _hitmarker = root.Q<VisualElement>("hitmarker");
    }

    private AbilityUI GetAbilityUI(VisualElement root, string id)
    {
        return new AbilityUI
        {
            Overlay = root.Q<VisualElement>($"overlay-{id}"),
            Timer = root.Q<Label>($"timer-{id}")
        };
    }

    private void UpdateHealth(float currentHealth)
    {
        // Clamp health between 0 and MaxHealth for the bar calculation
        var safeHealth = Mathf.Clamp(currentHealth, 0f, MAX_HEALTH);
        var healthPercent = safeHealth / MAX_HEALTH * 100f;

        // Scale the width of the fill bar
        _healthBarFill.style.width = new Length(healthPercent, LengthUnit.Percent);

        // Update the numeric label
        // Using Mathf.CeilToInt so 0.1hp shows as 1, avoiding "0 / 100" while still alive
        _healthText.text = $"{Mathf.CeilToInt(safeHealth)} / {MAX_HEALTH}";
    }

    private void UpdateAbilitySlot(AbilityUI ui, float currentCooldown)
    {
        if (currentCooldown > 0f)
        {
            // Activate gray overlay and update text
            ui.Overlay.style.display = DisplayStyle.Flex;
            ui.Timer.text = currentCooldown.ToString("F1"); // Formats to 1 decimal place (e.g. "2.4")
        }
        else
        {
            // Hide gray overlay completely when ready
            ui.Overlay.style.display = DisplayStyle.None;
        }
    }

    // Ability Wrappers to keep the Update loop clean
    private struct AbilityUI
    {
        public VisualElement Overlay;
        public Label Timer;
    }
}