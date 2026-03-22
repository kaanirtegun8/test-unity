using UnityEngine;

public class AuthDebugBootstrap : MonoBehaviour
{
    [SerializeField] private bool signInOnStart = true;
    [SerializeField] private string fakeUserId = "local_debug_user";
    [SerializeField] private string fakeDisplayName = "Debug User";
    [SerializeField] private bool fakeAnonymous = true;

    private readonly AuthStateStore authStateStore = new AuthStateStore();

    private void Start()
    {
        if (!signInOnStart)
        {
            return;
        }

        BootstrapFakeAuth();
    }

    [ContextMenu("Bootstrap Fake Auth")]
    public void BootstrapFakeAuth()
    {
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
