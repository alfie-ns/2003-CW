using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Main class that handles the roulette game mechanics and UI
public class Roulette : MonoBehaviour
{
    [Header("UI Elements")]
    public Text resultText; // Displays the result of the spin and winnings
    public Text countdownText; // Shows countdown until next spin
    public Text currentBetText; // Displays all current bets
    
    [Header("Betting")]
    public Button[] chipButtons; // Array of buttons representing different chip denominations (1,2,5,10,25,50,100)
    public Transform rouletteBoardTransform; // Reference to the game board where betting spots are placed
    public float spinTime = 60f; // Duration of betting round in seconds

    private int selectedChipValue; // Tracks the currently selected chip value for betting
    private float timeUntilSpin; // Countdown timer
    private Dictionary<string, int> currentBets = new Dictionary<string, int>(); // Stores all active bets as betType:amount pairs (e.g. "Red":100)
    private bool bettingOpen = true; // Controls if players can place bets

    void Start()
    {
        timeUntilSpin = spinTime;
        SetupChipButtons();
    }

    void SetupChipButtons()
    {
        int[] chipValues = { 1, 2, 5, 10, 25, 50, 100 };
        
        for (int i = 0; i < chipButtons.Length; i++)
        {
            int value = chipValues[i];
            chipButtons[i].onClick.AddListener(() => SelectChipValue(value));
        }
    }

    void Update()
    {
        if (bettingOpen)
        {
            UpdateTimer();
        }
    }

    void UpdateTimer()
    {
        timeUntilSpin -= Time.deltaTime;
        countdownText.text = $"Time until spin: {Mathf.CeilToInt(timeUntilSpin)}s";

        if (timeUntilSpin <= 0)
        {
            bettingOpen = false;
            Spin();
        }
    }

    public void SelectChipValue(int value)
    {
        if (!bettingOpen) return;
        selectedChipValue = value;
    }

    public void PlaceBet(string betSpot)
    {
        if (!bettingOpen || selectedChipValue <= 0) return;

        // Add bet to current bets
        if (currentBets.ContainsKey(betSpot))
            currentBets[betSpot] += selectedChipValue;
        else
            currentBets[betSpot] = selectedChipValue;

        UpdateBetDisplay();
    }

    void UpdateBetDisplay()
    {
        string betDisplay = "Current Bets:\n";
        foreach (var bet in currentBets)
        {
            betDisplay += $"{bet.Key}: ${bet.Value}\n";
        }
        currentBetText.text = betDisplay;
    }

    private void Spin()
    {
        // Simulate spinning the roulette wheel
        int result = Random.Range(0, 37);
        resultText.text = "Result: " + result.ToString();

        // Process all current bets
        int totalWinnings = 0;
        foreach (var bet in currentBets)
        {
            if (CheckWin(result, bet.Key))
            {
                totalWinnings += CalculateWinnings(bet.Key, bet.Value);
            }
        }

        // Display results
        resultText.text += $"\nTotal Winnings: ${totalWinnings}";

        // Create a description of the bets
        string betsDescription = "";
        foreach (var bet in currentBets)
        {
            betsDescription += $"{bet.Key}: ${bet.Value}, ";
        }

        string prompt = $"Roulette wheel landed on {result}. " +
                        $"Player's bets were: {betsDescription} " +
                        $"Total winnings: ${totalWinnings}. " +
                        $"Give a brief croupier comment about this roulette spin.";

        // Send to AI
        ApiManager.Instance.SendGameUpdate(prompt);
        
        // Reset for next round
        ResetGame();
    }

    // Processes bet results and calculates winnings based on bet type
    private int CalculateWinnings(string betType, int betAmount)
    {
        // Multipliers based on standard roulette rules:
        // Single number (35:1)
        // Red/Black/Odd/Even/1-18/19-36 (2:1)
        // Dozens (3:1)
        switch (betType)
        {
            case "Single Number": return betAmount * 35;
            case "Red":
            case "Black":
            case "Odd":
            case "Even":
            case "1-18":
            case "19-36": return betAmount * 2;
            case "1st 12":
            case "2nd 12":
            case "3rd 12": return betAmount * 3;
            default: return 0;
        }
    }

    private void ResetGame()
    {
        timeUntilSpin = spinTime;
        currentBets.Clear();
        selectedChipValue = 0;
        bettingOpen = true;
        UpdateBetDisplay();
    }

    // Validates if a bet wins based on the spin result
    private bool CheckWin(int result, string betType)
    {
        // Handles all roulette bet types:
        // - Colors (Red/Black)
        // - Odd/Even
        // - Number ranges (1-18, 19-36)
        // - Dozens (1st 12, 2nd 12, 3rd 12)
        // - Single numbers
        switch (betType)
        {
            case "Red": return IsRed(result);
            case "Black": return IsBlack(result);
            case "Odd": return result % 2 != 0 && result != 0;
            case "Even": return result % 2 == 0 && result != 0;
            case "1-18": return result >= 1 && result <= 18;
            case "19-36": return result >= 19 && result <= 36;
            case "1st 12": return result >= 1 && result <= 12;
            case "2nd 12": return result >= 13 && result <= 24;
            case "3rd 12": return result >= 25 && result <= 36;
            default:
                // Check if it's a single number bet
                if (int.TryParse(betType, out int betNumber))
                    return result == betNumber;
                return false;
        }
    }

    // Standard roulette wheel red numbers
    private bool IsRed(int number)
    {
        int[] redNumbers = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
        return System.Array.IndexOf(redNumbers, number) >= 0;
    }

    // Standard roulette wheel black numbers
    private bool IsBlack(int number)
    {
        int[] blackNumbers = { 2, 4, 6, 8, 10, 11, 13, 15, 17, 20, 22, 24, 26, 28, 29, 31, 33, 35 };
        return System.Array.IndexOf(blackNumbers, number) >= 0;
    }
}
