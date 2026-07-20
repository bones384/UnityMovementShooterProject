using System;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

public class PauseMenuManager : MonoBehaviour
{
    private Button _btnQuit;
    private Button _btnResume;
    private UIDocument _doc;

    private bool _isPaused;
    private VisualElement _pauseOverlay;
    private bool toggled;

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !toggled)
        {
            toggled = true;
            if (_isPaused) ResumeGame();
            else PauseGame();
        }
        else
        {
            toggled = false;
        }
    }

    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;

        _pauseOverlay = root.Q<VisualElement>("pause-overlay");
        _btnResume = root.Q<Button>("btn-resume");
        _btnQuit = root.Q<Button>("btn-quit");

        _btnResume.clicked += ResumeGame;
        _btnQuit.clicked += QuitToMainMenu;
        
        _pauseOverlay.style.display = DisplayStyle.None;
    }

    private void PauseGame()
    {
        _isPaused = true;
        _pauseOverlay.style.display = DisplayStyle.Flex;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ResumeGame()
    {
        _isPaused = false;
        _pauseOverlay.style.display = DisplayStyle.None;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private async void QuitToMainMenu()
    {
        Debug.Log("Quitting Session...");
        
        _btnQuit.SetEnabled(false);
        _btnResume.SetEnabled(false);
        
        if (MatchmakingState.CurrentSession != null)
        {
            try
            {
                if (MatchmakingState.IsHost)
                    await MatchmakingState.CurrentSession.AsHost().DeleteAsync();
                else
                    await MatchmakingState.CurrentSession.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Session cleanup threw a warning, proceeding anyway: {e.Message}");
            }

            MatchmakingState.CurrentSession = null;
        }
        
        if (ClientServerBootstrap.ClientWorld != null) ClientServerBootstrap.ClientWorld.Dispose();

        if (ClientServerBootstrap.ServerWorld != null) ClientServerBootstrap.ServerWorld.Dispose();

        MatchmakingState.IsHost = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("MainMenuScene");
    }
}