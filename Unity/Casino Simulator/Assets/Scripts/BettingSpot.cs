using UnityEngine;
using UnityEngine.UI;

public class BettingSpot : MonoBehaviour
{
    public string betType; // Set this in inspector for each spot
    private Roulette roulette;

    void Start()
    {
        roulette = FindObjectOfType<Roulette>();
        GetComponent<Button>().onClick.AddListener(PlaceBet);
    }

    void PlaceBet()
    {
        roulette.PlaceBet(betType);
    }
}