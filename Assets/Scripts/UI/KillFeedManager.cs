using UnityEngine;
using UnityEngine.UIElements;

public class KillfeedManager : MonoBehaviour
{
    private const int MAX_ENTRIES = 5;
    private const int ENTRY_LIFESPAN_MS = 5000;
    public static KillfeedManager Instance;

    [Header("Kill Icons")] public Texture2D iconHitscan;
    public Texture2D iconProjectile;
    public Texture2D iconKillbox;
    public Texture2D iconOther;
    private VisualElement _container;

    private UIDocument _doc;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        _container = _doc.rootVisualElement.Q<VisualElement>("killfeed-container");
    }

    public void AddKillfeedEntry(int killerId, int killerTeam, int victimId, int victimTeam, int reason)
    {
        if (_container == null) return;
        
        var myTeam = -1;
        if (PlayerVisualisationManager.LocalPlayer.HasValue)
            myTeam = PlayerVisualisationManager.LocalPlayer.Value.TeamIndex;

        var allyColor = new Color(0.15f, 0.4f, 0.8f);
        var enemyColor = new Color(0.7f, 0.15f, 0.15f);

        var row = new VisualElement();
        row.AddToClassList("killfeed-row");

        if (killerId != -1 && killerId != victimId && reason != 2)
        {
            var killerText = new Label($"Player {killerId}");
            killerText.AddToClassList("killfeed-text");
            killerText.style.color = killerTeam == myTeam ? new StyleColor(allyColor) : new StyleColor(enemyColor);
            row.Add(killerText);
        }

        var icon = new VisualElement();
        icon.AddToClassList("killfeed-icon");
        icon.style.backgroundImage = reason switch
        {
            0 => iconHitscan,
            1 => iconProjectile,
            2 => iconKillbox,
            _ => iconOther
        };
        row.Add(icon);

        var victimText = new Label($"Player {victimId}");
        victimText.AddToClassList("killfeed-text");
        victimText.style.color = victimTeam == myTeam ? new StyleColor(allyColor) : new StyleColor(enemyColor);
        row.Add(victimText);

        _container.Add(row);

        row.schedule.Execute(() =>
        {
            row.style.opacity = 0f;
            row.schedule.Execute(() =>
            {
                if (_container.Contains(row)) _container.Remove(row);
            }).StartingIn(250);
        }).StartingIn(ENTRY_LIFESPAN_MS);

        if (_container.childCount > MAX_ENTRIES) _container.RemoveAt(0);
    }
}