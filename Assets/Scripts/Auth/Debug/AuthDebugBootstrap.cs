using UnityEngine;

public class AuthDebugBootstrap : MonoBehaviour
{
    [SerializeField] private bool enableDebugBootstrap = false;
    [SerializeField] private bool signInOnStart = false;
    [SerializeField] private string fakeUserId = "local_debug_user";
    [SerializeField] private string fakeDisplayName = "Debug User";
    [SerializeField] private bool fakeAnonymous = true;

    private readonly AuthStateStore authStateStore = AuthStateStore.Local;

    private void Start()
    {
        if (!enableDebugBootstrap || !signInOnStart)
        {
            return;
        }

        BootstrapFakeAuth();
    }

    [ContextMenu("Bootstrap Fake Auth")]
    public void BootstrapFakeAuth()
    {
        if (!enableDebugBootstrap)
        {
            Debug.LogWarning("AuthDebugBootstrap: Debug bootstrap is disabled. Enable 'Enable Debug Bootstrap' to use fake auth.", this);
            return;
        }

        string safeUserId = string.IsNullOrWhiteSpace(fakeUserId)
            ? "local_debug_user"
            : fakeUserId.Trim();
        string safeDisplayName = string.IsNullOrWhiteSpace(fakeDisplayName)
            ? "Debug User"
            : fakeDisplayName.Trim();

        AuthUser fakeUser = new AuthUser
        {
            userId = safeUserId,
            displayName = safeDisplayName,
            isAnonymous = fakeAnonymous
        };

        authStateStore.SetAuthenticatedUser(fakeUser);
        authStateStore.SyncCurrentUser(LobbyStateStore.Local.SyncLocalPlayerFromAuth);
    }
}
