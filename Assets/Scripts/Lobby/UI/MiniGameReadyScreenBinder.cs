using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MiniGameReadyScreenBinder : MonoBehaviour
{
    private const int SlotCount = 4;

    private enum MatchStartReadinessState
    {
        None = 0,
        ReadyByEarlyConfirm = 1,
        ReadyByTimeout = 2
    }

    private const string EarlyConfirmReadyLog = "MiniGameReadyScreenBinder: Ready to start early (all players confirmed).";
    private const string TimeoutReadyLog = "MiniGameReadyScreenBinder: Ready to start by timeout (countdown reached 00:00).";

    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private int countdownStartSeconds = 30;
    [SerializeField] private TMP_Text[] playerNameTexts = new TMP_Text[SlotCount];
    [SerializeField] private TMP_Text[] playerStateTexts = new TMP_Text[SlotCount];
    [SerializeField] private Button[] playerConfirmButtons = new Button[SlotCount];
    [SerializeField] private string emptySlotNameText = "";
    [SerializeField] private string emptySlotStateText = "Empty";
    [SerializeField] private string lockedSlotNameText = "";
    [SerializeField] private string lockedSlotStateText = "Locked";
    [SerializeField] private string waitingText = "Waiting";
    [SerializeField] private string confirmedText = "Confirmed";
    [SerializeField] private string confirmActionText = "Confirm";
    [SerializeField] private Color waitingTextColor = new Color(0.78f, 0.87f, 0.95f, 0.95f);
    [SerializeField] private Color confirmedTextColor = new Color(0.62f, 0.9f, 0.67f, 1f);
    [SerializeField] private Color confirmActionColor = new Color(0.9f, 0.95f, 1f, 1f);

    private readonly UnityAction[] slotConfirmHandlers = new UnityAction[SlotCount];
    private Coroutine countdownCoroutine;
    private bool isCountdownCompleted;
    private int remainingSeconds;
    private MatchStartReadinessState matchStartReadinessState = MatchStartReadinessState.None;

    private LobbyStateStore SharedStore => LobbyStateStore.Local;

    private void OnEnable()
    {
        AutoAssignReferences();
        BindConfirmButtons();
        ResetPlayersToWaiting();
        ResetMatchStartReadinessState();
        RefreshFromCurrentRoom();
        StartCountdown();
    }

    private void OnDisable()
    {
        StopCountdown();
        UnbindConfirmButtons();
    }

    private void OnDestroy()
    {
        StopCountdown();
        UnbindConfirmButtons();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignReferences(false);
    }
#endif

    public void RefreshFromCurrentRoom()
    {
        ApplyPlayers(SharedStore.CurrentRoom);
    }

    private void StartCountdown()
    {
        StopCountdown();
        isCountdownCompleted = false;
        remainingSeconds = Mathf.Max(0, countdownStartSeconds);
        ApplyCountdownText(remainingSeconds);
        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    private void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }

    private System.Collections.IEnumerator CountdownCoroutine()
    {
        WaitForSeconds waitOneSecond = new WaitForSeconds(1f);

        while (remainingSeconds > 0)
        {
            yield return waitOneSecond;
            remainingSeconds = Mathf.Max(0, remainingSeconds - 1);
            ApplyCountdownText(remainingSeconds);
        }

        countdownCoroutine = null;
        if (!isCountdownCompleted)
        {
            isCountdownCompleted = true;
            SetMatchStartReadinessState(MatchStartReadinessState.ReadyByTimeout);
        }
    }

    private void ApplyCountdownText(int seconds)
    {
        if (countdownText == null)
        {
            return;
        }

        int safeSeconds = Mathf.Max(0, seconds);
        int minutesPart = safeSeconds / 60;
        int secondsPart = safeSeconds % 60;
        countdownText.text = $"{minutesPart:00}:{secondsPart:00}";
    }

    private void ApplyPlayers(RoomState room)
    {
        int maxPlayers = Mathf.Clamp(room != null ? room.maxPlayers : SlotCount, 1, SlotCount);
        List<PlayerState> orderedPlayers = GetOrderedPlayers(room, maxPlayers);
        string localPlayerId = SharedStore.LocalPlayer != null
            ? NormalizePlayerId(SharedStore.LocalPlayer.playerId)
            : string.Empty;

        for (int i = 0; i < SlotCount; i++)
        {
            if (i < orderedPlayers.Count)
            {
                PlayerState player = orderedPlayers[i];
                string displayName = ResolveDisplayName(player, i + 1);
                ApplyOccupiedSlot(i, displayName, player, localPlayerId);
            }
            else if (i < maxPlayers)
            {
                ApplyPassiveSlot(i, emptySlotNameText, emptySlotStateText, waitingTextColor, false);
            }
            else
            {
                ApplyPassiveSlot(i, lockedSlotNameText, lockedSlotStateText, waitingTextColor, false);
            }
        }

        EvaluateEarlyConfirmReadiness(room);
    }

    private void ApplyOccupiedSlot(int slotIndex, string displayName, PlayerState player, string localPlayerId)
    {
        bool isLocalPlayer = IsLocalPlayer(player, localPlayerId);
        bool isConfirmed = player != null && player.isConfirmed;
        string stateText = waitingText;
        Color stateColor = waitingTextColor;
        bool confirmInteractable = false;

        if (isConfirmed)
        {
            stateText = confirmedText;
            stateColor = confirmedTextColor;
        }
        else if (isLocalPlayer)
        {
            stateText = confirmActionText;
            stateColor = confirmActionColor;
            confirmInteractable = true;
        }

        ApplyPassiveSlot(slotIndex, displayName, stateText, stateColor, true);
        SetConfirmButtonState(slotIndex, confirmInteractable);
    }

    private void ApplyPassiveSlot(int slotIndex, string nameText, string stateText, Color stateColor, bool occupied)
    {
        TMP_Text nameLabel = GetPlayerNameText(slotIndex);
        TMP_Text stateLabel = GetPlayerStateText(slotIndex);

        if (nameLabel != null)
        {
            nameLabel.text = nameText ?? string.Empty;
            nameLabel.gameObject.SetActive(occupied || !string.IsNullOrWhiteSpace(nameLabel.text));
        }

        if (stateLabel != null)
        {
            stateLabel.text = stateText ?? string.Empty;
            stateLabel.color = stateColor;
        }

        SetConfirmButtonState(slotIndex, false);
    }

    private static List<PlayerState> GetOrderedPlayers(RoomState room, int maxPlayers)
    {
        List<PlayerState> orderedPlayers = new List<PlayerState>();
        if (room == null || room.players == null || room.players.Count == 0 || maxPlayers <= 0)
        {
            return orderedPlayers;
        }

        PlayerState hostPlayer = FindHostPlayer(room.players);
        if (hostPlayer != null)
        {
            orderedPlayers.Add(hostPlayer);
        }

        for (int i = 0; i < room.players.Count; i++)
        {
            PlayerState player = room.players[i];
            if (player == null)
            {
                continue;
            }

            if (hostPlayer != null && AreSamePlayer(player, hostPlayer))
            {
                continue;
            }

            orderedPlayers.Add(player);
            if (orderedPlayers.Count >= maxPlayers)
            {
                break;
            }
        }

        return orderedPlayers;
    }

    private static PlayerState FindHostPlayer(List<PlayerState> players)
    {
        for (int i = 0; i < players.Count; i++)
        {
            PlayerState player = players[i];
            if (player != null && player.isHost)
            {
                return player;
            }
        }

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null)
            {
                return players[i];
            }
        }

        return null;
    }

    private static bool AreSamePlayer(PlayerState a, PlayerState b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        string aId = a.playerId != null ? a.playerId.Trim() : string.Empty;
        string bId = b.playerId != null ? b.playerId.Trim() : string.Empty;

        if (!string.IsNullOrWhiteSpace(aId) && !string.IsNullOrWhiteSpace(bId))
        {
            return string.Equals(aId, bId, System.StringComparison.Ordinal);
        }

        return false;
    }

    private static string ResolveDisplayName(PlayerState player, int fallbackIndex)
    {
        if (player != null)
        {
            string safeName = player.displayName != null ? player.displayName.Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(safeName))
            {
                return safeName;
            }
        }

        return $"Player {Mathf.Max(1, fallbackIndex)}";
    }

    private void OnConfirmButtonClicked(int slotIndex)
    {
        RoomState room = SharedStore.CurrentRoom;
        if (room == null || room.players == null || slotIndex < 0)
        {
            return;
        }

        int maxPlayers = Mathf.Clamp(room.maxPlayers, 1, SlotCount);
        List<PlayerState> orderedPlayers = GetOrderedPlayers(room, maxPlayers);
        if (slotIndex >= orderedPlayers.Count)
        {
            return;
        }

        PlayerState targetPlayer = orderedPlayers[slotIndex];
        if (targetPlayer == null || targetPlayer.isConfirmed)
        {
            return;
        }

        string localPlayerId = SharedStore.LocalPlayer != null
            ? NormalizePlayerId(SharedStore.LocalPlayer.playerId)
            : string.Empty;
        if (string.IsNullOrWhiteSpace(localPlayerId) || !IsLocalPlayer(targetPlayer, localPlayerId))
        {
            return;
        }

        targetPlayer.isConfirmed = true;
        RefreshFromCurrentRoom();
    }

    private TMP_Text GetPlayerNameText(int slotIndex)
    {
        if (playerNameTexts == null || slotIndex < 0 || slotIndex >= playerNameTexts.Length)
        {
            return null;
        }

        return playerNameTexts[slotIndex];
    }

    private TMP_Text GetPlayerStateText(int slotIndex)
    {
        if (playerStateTexts == null || slotIndex < 0 || slotIndex >= playerStateTexts.Length)
        {
            return null;
        }

        return playerStateTexts[slotIndex];
    }

    private Button GetPlayerConfirmButton(int slotIndex)
    {
        if (playerConfirmButtons == null || slotIndex < 0 || slotIndex >= playerConfirmButtons.Length)
        {
            return null;
        }

        return playerConfirmButtons[slotIndex];
    }

    private void BindConfirmButtons()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            Button button = EnsurePlayerConfirmButton(i);
            if (button == null)
            {
                continue;
            }

            if (slotConfirmHandlers[i] == null)
            {
                int capturedIndex = i;
                slotConfirmHandlers[i] = () => OnConfirmButtonClicked(capturedIndex);
            }

            button.onClick.RemoveListener(slotConfirmHandlers[i]);
            button.onClick.AddListener(slotConfirmHandlers[i]);
        }
    }

    private void UnbindConfirmButtons()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            Button button = GetPlayerConfirmButton(i);
            UnityAction handler = slotConfirmHandlers[i];

            if (button != null && handler != null)
            {
                button.onClick.RemoveListener(handler);
            }
        }
    }

    private Button EnsurePlayerConfirmButton(int slotIndex)
    {
        Button existingButton = GetPlayerConfirmButton(slotIndex);
        if (existingButton != null)
        {
            return existingButton;
        }

        TMP_Text stateLabel = GetPlayerStateText(slotIndex);
        if (stateLabel == null)
        {
            return null;
        }

        stateLabel.raycastTarget = true;

        Button createdButton = stateLabel.GetComponent<Button>();
        if (createdButton == null)
        {
            createdButton = stateLabel.gameObject.AddComponent<Button>();
        }

        if (createdButton.targetGraphic == null)
        {
            createdButton.targetGraphic = stateLabel;
        }

        ColorBlock colors = createdButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = Color.white;
        createdButton.colors = colors;
        createdButton.transition = Selectable.Transition.ColorTint;

        playerConfirmButtons[slotIndex] = createdButton;
        return createdButton;
    }

    private void SetConfirmButtonState(int slotIndex, bool interactable)
    {
        Button button = EnsurePlayerConfirmButton(slotIndex);
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private void ResetPlayersToWaiting()
    {
        RoomState room = SharedStore.CurrentRoom;
        if (room == null || room.players == null)
        {
            return;
        }

        for (int i = 0; i < room.players.Count; i++)
        {
            PlayerState player = room.players[i];
            if (player == null)
            {
                continue;
            }

            player.isConfirmed = false;
        }
    }

    private void ResetMatchStartReadinessState()
    {
        matchStartReadinessState = MatchStartReadinessState.None;
        isCountdownCompleted = false;
    }

    private void EvaluateEarlyConfirmReadiness(RoomState room)
    {
        if (matchStartReadinessState != MatchStartReadinessState.None || room == null || room.players == null)
        {
            return;
        }

        bool hasAnyPlayer = false;
        for (int i = 0; i < room.players.Count; i++)
        {
            PlayerState player = room.players[i];
            if (player == null)
            {
                continue;
            }

            hasAnyPlayer = true;
            if (!player.isConfirmed)
            {
                return;
            }
        }

        if (!hasAnyPlayer)
        {
            return;
        }

        SetMatchStartReadinessState(MatchStartReadinessState.ReadyByEarlyConfirm);
        StopCountdown();
    }

    private void SetMatchStartReadinessState(MatchStartReadinessState nextState)
    {
        if (nextState == MatchStartReadinessState.None || matchStartReadinessState != MatchStartReadinessState.None)
        {
            return;
        }

        matchStartReadinessState = nextState;

        if (nextState == MatchStartReadinessState.ReadyByEarlyConfirm)
        {
            Debug.Log(EarlyConfirmReadyLog);
            return;
        }

        if (nextState == MatchStartReadinessState.ReadyByTimeout)
        {
            Debug.Log(TimeoutReadyLog);
        }
    }

    private static string NormalizePlayerId(string playerId)
    {
        return playerId != null ? playerId.Trim() : string.Empty;
    }

    private static bool IsLocalPlayer(PlayerState player, string localPlayerId)
    {
        if (player == null || string.IsNullOrWhiteSpace(localPlayerId))
        {
            return false;
        }

        string playerId = NormalizePlayerId(player.playerId);
        return !string.IsNullOrWhiteSpace(playerId) &&
               string.Equals(playerId, localPlayerId, StringComparison.Ordinal);
    }

    private void AutoAssignReferences(bool includeInactive = true)
    {
        if (countdownText == null)
        {
            countdownText = transform.Find("HeaderArea/CountdownText")?.GetComponent<TMP_Text>();
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (GetPlayerNameText(i) == null)
            {
                string namePath = $"ContentArea/RightPlayersArea/PlayersList/PlayerRow0{i + 1}/PlayerNameText0{i + 1}";
                TMP_Text resolved = transform.Find(namePath)?.GetComponent<TMP_Text>();
                if (resolved != null)
                {
                    playerNameTexts[i] = resolved;
                }
            }

            if (GetPlayerStateText(i) == null)
            {
                string statePath = $"ContentArea/RightPlayersArea/PlayersList/PlayerRow0{i + 1}/ReadyStateText0{i + 1}";
                TMP_Text resolved = transform.Find(statePath)?.GetComponent<TMP_Text>();
                if (resolved != null)
                {
                    playerStateTexts[i] = resolved;
                }
            }

            if (GetPlayerConfirmButton(i) == null)
            {
                TMP_Text stateLabel = GetPlayerStateText(i);
                if (stateLabel != null)
                {
                    playerConfirmButtons[i] = stateLabel.GetComponent<Button>();
                }
            }
        }

        if (!includeInactive || Application.isPlaying)
        {
            return;
        }

        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
        if (countdownText == null)
        {
            countdownText = FindTextByName(allTexts, "CountdownText");
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (playerNameTexts[i] == null)
            {
                playerNameTexts[i] = FindTextByName(allTexts, $"PlayerNameText0{i + 1}");
            }

            if (playerStateTexts[i] == null)
            {
                playerStateTexts[i] = FindTextByName(allTexts, $"ReadyStateText0{i + 1}");
            }

            if (playerConfirmButtons[i] == null && playerStateTexts[i] != null)
            {
                playerConfirmButtons[i] = playerStateTexts[i].GetComponent<Button>();
            }
        }
    }

    private static TMP_Text FindTextByName(TMP_Text[] allTexts, string targetName)
    {
        if (allTexts == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        for (int i = 0; i < allTexts.Length; i++)
        {
            TMP_Text text = allTexts[i];
            if (text != null && text.name == targetName)
            {
                return text;
            }
        }

        return null;
    }
}
