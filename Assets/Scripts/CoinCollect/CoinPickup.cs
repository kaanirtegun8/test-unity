using UnityEngine;

[DisallowMultipleComponent]
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private string collectorName = "TestPlayer01";
    [SerializeField] private bool disableOnPickup = true;
    [SerializeField] private int scoreValue = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        if (!string.Equals(other.gameObject.name, collectorName, System.StringComparison.Ordinal))
        {
            return;
        }

        if (CoinCollectScoreManager.IsRoundFinished)
        {
            return;
        }

        CoinCollectScoreManager.AddScore(scoreValue);

        if (disableOnPickup)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        scoreValue = Mathf.Max(0, scoreValue);
    }
#endif
}
