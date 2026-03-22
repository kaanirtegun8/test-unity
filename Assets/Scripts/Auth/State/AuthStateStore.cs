using System;

public class AuthStateStore
{
    public bool IsAuthenticated { get; private set; }
    public AuthUser CurrentUser { get; private set; }

    public void SetAuthenticatedUser(AuthUser user)
    {
        if (user == null)
        {
            ClearAuth();
            return;
        }

        CurrentUser = user;
        IsAuthenticated = true;
    }

    public void ClearAuth()
    {
        CurrentUser = null;
        IsAuthenticated = false;
    }

    public void SyncCurrentUser(Action<AuthUser> syncAction)
    {
        if (syncAction == null)
        {
            return;
        }

        syncAction(CurrentUser);
    }
}
