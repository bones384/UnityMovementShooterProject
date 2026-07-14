using UnityEngine;
using UnityEngine.UIElements;
using Entities.Netcode.GameMechanics;
using Unity.Mathematics;

[RequireComponent(typeof(UIDocument))]
public class KothHUDManager : MonoBehaviour
{
    private UIDocument _doc;
    
    private Label _teamATimer;
    private Label _teamBTimer;
    private VisualElement _pointSquare;
    private VisualElement _pointFill;
    private Label _pointStatusText;
    private Label _overtimeLabel;
    private VisualElement _victoryOverlay;
    private VisualElement _victoryBox;
    private bool _hasPlayedGameOverSound = false;
    private Label _victoryText;
    private readonly Color _colorGray = new Color(0.4f, 0.4f, 0.4f);
    private readonly Color _colorBlue = new Color(0.2f, 0.5f, 0.9f);
    private readonly Color _colorRed = new Color(0.8f, 0.2f, 0.2f);
    private readonly Color _colorBlueLight = new Color(0.4f, 0.7f, 1.0f);
    private readonly Color _colorRedLight = new Color(1.0f, 0.4f, 0.4f);

    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;

        _teamATimer = root.Q<Label>("team-a-timer");
        _teamBTimer = root.Q<Label>("team-b-timer");
        _pointSquare = root.Q<VisualElement>("point-square");
        _pointFill = root.Q<VisualElement>("point-fill");
        _pointStatusText = root.Q<Label>("point-status-text");
        _overtimeLabel = root.Q<Label>("overtime-label");
        _victoryOverlay = root.Q<VisualElement>("victory-overlay");
        _victoryBox = root.Q<VisualElement>("victory-box");
        _victoryText = root.Q<Label>("victory-text");
    }

    private void Update()
    {
        // Wait until both the KOTH state and the LocalPlayer are fully loaded
        if (!KothBridge.IsActive || PlayerVisualisationManager.LocalPlayer == null)
        {
            _doc.rootVisualElement.style.display = DisplayStyle.None;
            return;
        }

        _doc.rootVisualElement.style.display = DisplayStyle.Flex;
        var state = KothBridge.State;
        int myTeam = PlayerVisualisationManager.LocalPlayer.Value.TeamIndex;

        // Route the timers so Left is always Ally, Right is always Enemy
        float myTimer = myTeam == 0 ? state.TeamATimer : state.TeamBTimer;
        float enemyTimer = myTeam == 0 ? state.TeamBTimer : state.TeamATimer;

        // 1. Timers
        _teamATimer.text = FormatTime(myTimer);     // Left Side
        _teamBTimer.text = FormatTime(enemyTimer);  // Right Side

        ToggleClass(_teamATimer, "timer-zero", myTimer <= 0);
        ToggleClass(_teamBTimer, "timer-zero", enemyTimer <= 0);

        // 2. Base Square Color (Current Owner)
        if (state.CurrentOwner == -1) _pointSquare.style.backgroundColor = _colorGray;
        else if (state.CurrentOwner == myTeam) _pointSquare.style.backgroundColor = _colorBlue;
        else _pointSquare.style.backgroundColor = _colorRed;

        // 3. Capture Fill & Direction
        float capPercent = state.TimeToCapture > 0 ? (state.CaptureProgress / state.TimeToCapture) * 100f : 0f;
        _pointFill.style.width = new Length(capPercent, LengthUnit.Percent);

        if (state.CapturingTeam == myTeam)
        {
            _pointFill.style.backgroundColor = _colorBlueLight;
            _pointFill.style.left = 0;
            _pointFill.style.right = new StyleLength(StyleKeyword.Auto);
        }
        else if (state.CapturingTeam != -1)
        {
            _pointFill.style.backgroundColor = _colorRedLight;
            _pointFill.style.right = 0;
            _pointFill.style.left = new StyleLength(StyleKeyword.Auto);
        }

        // 4. Status Text
        if (state.IsContested)
        {
            _pointStatusText.text = "!";
            _pointStatusText.style.color = Color.yellow;
        }
        else if (state.CaptureProgress > 0 && state.CappingPlayerCount > 0)
        {
            _pointStatusText.text = $"x{state.CappingPlayerCount}";
            _pointStatusText.style.color = Color.white;
        }
        else
        {
            _pointStatusText.text = ""; 
        }

        // 5. Overtime
        _overtimeLabel.style.display = state.IsOvertime ? DisplayStyle.Flex : DisplayStyle.None;
        
        if (KothBridge.IsGameOver)
        {
            _victoryOverlay.style.display = DisplayStyle.Flex;
            
            bool iWon = KothBridge.WinningTeam == myTeam;

            if (iWon)
            {
                _victoryText.text = "YOU WIN!";
                ToggleClass(_victoryBox, "victory-red", false);
                ToggleClass(_victoryBox, "victory-blue", true);
                // --- NEW: VICTORY SOUND ---
                if (!_hasPlayedGameOverSound)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SFX.Victory);
                    _hasPlayedGameOverSound = true;
                }
            }
            else
            {
                _victoryText.text = "YOU LOSE!";
                ToggleClass(_victoryBox, "victory-blue", false);
                ToggleClass(_victoryBox, "victory-red", true);
                // --- NEW: DEFEAT SOUND ---
                if (!_hasPlayedGameOverSound)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SFX.Loss);
                    _hasPlayedGameOverSound = true;
                }
            }
        }
        else
        {
            // As soon as the server deletes the GameOverStateComponent (game restarts), this hides automatically
            _victoryOverlay.style.display = DisplayStyle.None;
            _hasPlayedGameOverSound = false;
        }
    }
    private string FormatTime(float seconds)
    {
        int s = math.max(0, Mathf.CeilToInt(seconds));
        int mins = s / 60;
        int secs = s % 60;
        return $"{mins}:{secs:D2}";
    }

    private void ToggleClass(VisualElement element, string className, bool enable)
    {
        if (enable && !element.ClassListContains(className))
            element.AddToClassList(className);
        else if (!enable && element.ClassListContains(className))
            element.RemoveFromClassList(className);
    }

    // --- DEBUG GUI ---
    private void OnGUI()
    {
        return;
        if (!KothBridge.IsActive) return;

        var state = KothBridge.State;

        // Draw a debug box starting at X: 10, Y: 200
        GUILayout.BeginArea(new Rect(10, 200, 300, 400));
        
        // Optional styling to make it readable against the game world
        GUI.contentColor = Color.green; 
        
        GUILayout.Label("<b>--- KOTH STATE DEBUG ---</b>");
        GUILayout.Label($"Radius: {state.Radius}");
        GUILayout.Label($"Height: {state.Height}");
        GUILayout.Label($"TimeToWin: {state.TimeToWin:F1}");
        GUILayout.Label($"TimeToCapture: {state.TimeToCapture:F1}");
        GUILayout.Label($"TeamATimer: {state.TeamATimer:F2}");
        GUILayout.Label($"TeamBTimer: {state.TeamBTimer:F2}");
        GUILayout.Label($"CurrentOwner: {state.CurrentOwner}");
        GUILayout.Label($"CapturingTeam: {state.CapturingTeam}");
        GUILayout.Label($"CaptureProgress: {state.CaptureProgress:F2}");
        GUILayout.Label($"IsOvertime: {state.IsOvertime}");
        GUILayout.Label($"IsContested: {state.IsContested}");
        GUILayout.Label($"CappingPlayerCount: {state.CappingPlayerCount}");
        
        GUILayout.EndArea();
    }
}