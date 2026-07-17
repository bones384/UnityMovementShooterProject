using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplayer;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections;
public static class MatchmakingState
{
    // A simple static class to hold our data between scenes
    public static bool IsHost;
    public static ISession CurrentSession; // <-- NEW
}

public class MainMenuManager : MonoBehaviour
{
    private UIDocument _doc;
    
    // UI Elements
    private VisualElement _loginContainer;
    private VisualElement _playContainer;
    private VisualElement _matchmakingOverlay;
    
    private TextField _inputUsername;
    private Button _btnLogin;
    private Label _lblWelcome;
    private Button _btnPlay;
    private Button _btnExit;
    private Button _btnCancel;
    private Label _lblStatus;

    private CancellationTokenSource _matchmakingCts;

    private async void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;

        // Bind Elements
        _loginContainer = root.Q<VisualElement>("login-container");
        _playContainer = root.Q<VisualElement>("play-container");
        _matchmakingOverlay = root.Q<VisualElement>("matchmaking-overlay");
        
        _inputUsername = root.Q<TextField>("input-username");
        _btnLogin = root.Q<Button>("btn-login");
        _lblWelcome = root.Q<Label>("lbl-welcome");
        _btnPlay = root.Q<Button>("btn-play");
        _btnExit = root.Q<Button>("btn-exit");
        _btnCancel = root.Q<Button>("btn-cancel");
        _lblStatus = root.Q<Label>("lbl-status");

        // Button Events
        _btnLogin.clicked += OnLoginClicked;
        _btnPlay.clicked += OnPlayClicked;
        _btnExit.clicked += OnExitClicked;
        _btnCancel.clicked += OnCancelClicked;

        // Force UI state to wait
        _loginContainer.style.display = DisplayStyle.None;
        _playContainer.style.display = DisplayStyle.None;
        
        await InitializeServices();
    }

    private async Task InitializeServices()
    {
        try
        {
            // Only initialize if we haven't already (crucial for returning from gameplay)
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var options = new InitializationOptions();
#if !UNITY_EDITOR
                options.SetProfile("BuildPlayer"); 
#endif
                await UnityServices.InitializeAsync(options);
            }

            // Check if we are already signed in from a previous visit to this menu
            if (AuthenticationService.Instance.IsSignedIn)
            {
                ShowPlayScreen(AuthenticationService.Instance.PlayerName);
            }
            else
            {
                ShowLoginScreen();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
            _lblWelcome.text = "Service Init Failed";
            _playContainer.style.display = DisplayStyle.Flex;
        }
    }

    private void ShowLoginScreen()
    {
        _loginContainer.style.display = DisplayStyle.Flex;
        _playContainer.style.display = DisplayStyle.None;
        _btnLogin.SetEnabled(true);
        _btnLogin.text = "CONNECT";
    }

    private void ShowPlayScreen(string playerName)
    {
        _loginContainer.style.display = DisplayStyle.None;
        _playContainer.style.display = DisplayStyle.Flex;
        
        // Use a fallback if name is null for some reason
        string displayName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
        
        // Strip the weird # hash that Unity sometimes appends to anonymous names
        int hashIndex = displayName.IndexOf('#');
        if (hashIndex > 0) displayName = displayName.Substring(0, hashIndex);

        _lblWelcome.text = $"Welcome, {displayName}!";
    }

    private async void OnLoginClicked()
    {
        string desiredName = _inputUsername.value.Trim();
        if (string.IsNullOrEmpty(desiredName)) return;

        _btnLogin.SetEnabled(false);
        _btnLogin.text = "CONNECTING...";

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            await AuthenticationService.Instance.UpdatePlayerNameAsync(desiredName);
            
            Debug.Log($"Logged in as {AuthenticationService.Instance.PlayerName}");
            ShowPlayScreen(desiredName);
        }
        catch (Exception e)
        {
            Debug.LogError($"Login failed: {e.Message}");
            _btnLogin.SetEnabled(true);
            _btnLogin.text = "CONNECT";
        }
    }

    private async void OnPlayClicked()
    {
        _matchmakingOverlay.style.display = DisplayStyle.Flex;
        _btnCancel.SetEnabled(true);

        _matchmakingCts = new CancellationTokenSource();
        await StartMatchmaking(_matchmakingCts.Token);
        _matchmakingOverlay.style.display = DisplayStyle.None;
        _btnCancel.SetEnabled(false);
    }

    private async Task StartMatchmaking(CancellationToken ct)
    {
        try
        {
            _lblStatus.text = "Searching for open games...";

            var queryOptions = new QuerySessionsOptions();
            queryOptions.FilterOptions.Add(new FilterOption(FilterField.AvailableSlots, "1",
                FilterOperation.GreaterOrEqual));

            var results = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);
            ct.ThrowIfCancellationRequested();

            ISession session;

            if (results.Sessions.Count > 0)
            {
                _lblStatus.text = "Found a game! Connecting...";
                session = await MultiplayerService.Instance.JoinSessionByIdAsync(results.Sessions[0].Id);
            }
            else
            {
                _lblStatus.text = "No open games found. Creating a new one...";
                var sessionOptions = new SessionOptions { MaxPlayers = 4 }.WithRelayNetwork();
                session = await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
            }

            _lblStatus.text = "Successfully Connected! Starting Game...";
            _btnCancel.SetEnabled(false);

            MatchmakingState.IsHost = session.IsHost;
            MatchmakingState.CurrentSession = session; // <-- NEW
            
            await Task.Delay(1000);
            SceneManager.LoadScene("MainTestScene");
        }
        catch (SessionException e)
        {
            _lblStatus.text = "Error: " + e.Message;
            Debug.LogError(e);
        }
        catch (TaskCanceledException)
        {
            // Cancelled by user
        }
        catch (Exception e)
        {
            _lblStatus.text = "Error: " + e.Message;
            Debug.LogError(e);
        }
      
    }

    private void OnCancelClicked()
    {
        _lblStatus.text = "Canceling...";
        _btnCancel.SetEnabled(false);
        
        if (_matchmakingCts != null)
        {
            _matchmakingCts.Cancel();
            _matchmakingCts.Dispose();
            _matchmakingCts = null;
        }

        _matchmakingOverlay.style.display = DisplayStyle.None;
    }

    private void OnExitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}