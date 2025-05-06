using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

// Main class that handles the roulette game mechanics and UI
public class Roulette : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI resultText; // Displays the result of the spin and winnings
    public TextMeshProUGUI currentBetText; // Displays all current bets
    public TextMeshProUGUI balanceText; // Displays the player's current balance
    
    [Header("Betting Controls")]
    public Button[] chipButtons; // Array of buttons representing different chip denominations (1,2,5,10,25,50,100)
    public Button dealButton; // Button to start the roulette spin
    public Button clearBetButton; // Button to clear all bets
    public Button[] betOptionButtons; // Array of buttons for betting options (Red, Black, Odd, Even, etc.)
    
    [Header("Game Settings")]
    public float spinAnimationTime = 3f; // Duration of the spin animation in seconds
    
    private int selectedChipValue; // Tracks the currently selected chip value for betting
    private Dictionary<string, int> currentBets = new Dictionary<string, int>(); // Stores all active bets as betType:amount pairs (e.g. "Red":100)
    private bool bettingOpen = true; // Controls if players can place bets
    private bool isSpinning = false; // Tracks if wheel is currently spinning
    private int lastResult = -1; // Stores the last spin result (-1 means no result yet)

    [Header("Game Interaction")]
    public float interactionRange = 2f;
    public GameObject rouletteUIParent;
    public GameObject playerObject;
    public GameObject Crosshair;
    public Transform playerStandPoint;
    public GameObject interactionPrompt;
    
    [Header("Balance Management")]
    public MonoBehaviour balanceManagerObject; // Reference to the object with IBalanceManager implementation
    private IBalanceManager balanceManager;

    private bool isPlayingRoulette = false;
    private Vector3 savedPlayerPosition;
    private Quaternion savedPlayerRotation;
    private CharacterController playerController;
    private FirstPersonController firstPersonController;
    private FirstPersonLook firstPersonLook;

    void Awake()
    {
        // Disable Roulette UI at start
        if (rouletteUIParent != null)
            rouletteUIParent.SetActive(false);
        
        // Disable interaction prompt at start
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
    
    void Start()
    {
        SetupChipButtons();
        SetupGameControls();
        SetupBetButtons();
        UpdateBetDisplay();
        resultText.text = "Place your bets!";
        
        // Get the balance manager
        balanceManager = balanceManagerObject as IBalanceManager;
        if (balanceManager == null)
        {
            Debug.LogError("Balance Manager not found or doesn't implement IBalanceManager!");
        }
        else
        {
            UpdateBalanceDisplay();
        }
    }

    private void UpdateBalanceDisplay()
    {
        if (balanceManager == null || balanceText == null) return;
        
        int currentBalance = balanceManager.GetBalance();
        balanceText.text = $"${currentBalance}";
    }

    void SetupChipButtons()
    {
        int[] chipValues = { 1, 5, 10, 25, 100 };
        
        for (int i = 0; i < chipButtons.Length; i++)
        {
            int value = chipValues[i];
            chipButtons[i].onClick.AddListener(() => SelectChipValue(value));
        }
    }

    void SetupGameControls()
    {
        dealButton.onClick.AddListener(StartDeal);
        clearBetButton.onClick.AddListener(ClearBets);
    }

    void SetupBetButtons()
    {
        // Setup each bet option button to call PlaceBet with its corresponding bet type
        foreach (Button button in betOptionButtons)
        {
            string betType = button.GetComponentInChildren<TextMeshProUGUI>().text;
            button.onClick.AddListener(() => PlaceBet(betType));
        }
    }

    void Update()
    {
        // Original update code for button states
        clearBetButton.interactable = bettingOpen && currentBets.Count > 0;
        dealButton.interactable = bettingOpen && currentBets.Count > 0;
        
        foreach (Button button in betOptionButtons)
        {
            button.interactable = bettingOpen && selectedChipValue > 0;
        }
        
        // New interaction code
        if (!isPlayingRoulette)
        {
            bool shouldShowPrompt = false;

            // First check if player is in range
            float distance = Vector3.Distance(playerObject.transform.position, transform.position);
            
            if (distance <= interactionRange)
            {
                // Then check if player is looking at the roulette table
                Camera playerCamera = playerObject.GetComponentInChildren<Camera>();
                if (playerCamera != null)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionRange))
                    {
                        // Check if the object hit is this roulette table
                        if (hit.transform == transform || hit.transform.IsChildOf(transform))
                        {
                            shouldShowPrompt = true;

                            if (Input.GetKeyDown(KeyCode.E))
                            {
                                StartRoulette();
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("No camera found on player object. Add fallback behavior here.");
                    
                    // Fallback if no camera is found - just check if player is facing the table
                    Vector3 directionToTable = transform.position - playerObject.transform.position;
                    directionToTable.y = 0; // Ignore height difference
                    
                    // Get forward direction of player
                    Vector3 playerForward = playerObject.transform.forward;
                    playerForward.y = 0; // Ignore height difference
                    
                    // Check if player is roughly facing the table (dot product > 0.5 means < 60 degree angle)
                    if (Vector3.Dot(playerForward.normalized, directionToTable.normalized) > 0.5f)
                    {
                        shouldShowPrompt = true;
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            StartRoulette();
                        }
                    }
                }
            }
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(shouldShowPrompt);
            }
        }
        else if (isPlayingRoulette && Input.GetKeyDown(KeyCode.E))
        {
            ExitRoulette();
        }
        if(isPlayingRoulette)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }
    }

    void StartRoulette()
    {
        isPlayingRoulette = true;
        
        Crosshair.SetActive(false); // Hide crosshair when playing Roulette

        // Get and disable player components first
        playerController = playerObject.GetComponent<CharacterController>();
        firstPersonController = playerObject.GetComponent<FirstPersonController>();
        firstPersonLook = playerObject.GetComponentInChildren<FirstPersonLook>();

        // Disable components before moving player
        if (playerController != null) playerController.enabled = false;
        if (firstPersonController != null) firstPersonController.enabled = false;
        if (firstPersonLook != null) 
        {
            firstPersonLook.enabled = false;
            firstPersonLook.transform.localRotation = Quaternion.identity;
        }

        // Save player's position and rotation after disabling controls
        savedPlayerPosition = playerObject.transform.position;
        savedPlayerRotation = playerObject.transform.rotation;

        // Use the designated player stand point
        if (playerStandPoint != null)
        {
            // Move player to the designated position
            playerObject.transform.position = playerStandPoint.position;
        
            // Calculate the direction from the player to the table
            Vector3 lookDirection = transform.position - playerStandPoint.position;
            lookDirection.y = 0; // Keep the look direction level (no looking up/down)
        
            // Make the player face the table
            if (lookDirection != Vector3.zero)
            {
                playerObject.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
            else
            {
                // Fall back to using the stand point's rotation if calculation fails
                playerObject.transform.rotation = playerStandPoint.rotation;
            }
        }
        else
        {
            Debug.LogError("Player Stand Point is not assigned! Please assign it in the Inspector.");
        
            // Fallback to a calculated position if stand point is missing
            Vector3 tablePosition = transform.position;
            Vector3 directionToTable = tablePosition - playerObject.transform.position;
            directionToTable.y = 0;
            directionToTable = directionToTable.normalized;
        
            Vector3 newPosition = tablePosition - directionToTable * 2.5f;
            newPosition.y = savedPlayerPosition.y;
        
            playerObject.transform.position = newPosition;
        
            Vector3 lookDirection = tablePosition - newPosition;
            lookDirection.y = 0;
        
            if (lookDirection != Vector3.zero)
            {
                playerObject.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }
    
        // Show cursor and enable UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Enable Roulette UI
        if (rouletteUIParent != null)
        {
            rouletteUIParent.SetActive(true);
        }
        else
        {
            Debug.LogError("rouletteUIParent is null! Make sure it's assigned in the Inspector.");
        }

        // Reset the game for a fresh start
        ResetGame();

        // Update balance when starting the game
        UpdateBalanceDisplay();
    }

    void ExitRoulette()
    {
        if (isSpinning) return; // Don't allow exit while spinning
        
        isPlayingRoulette = false;
    
        // Restore player's position and rotation
        playerObject.transform.position = savedPlayerPosition;
        playerObject.transform.rotation = savedPlayerRotation;
    
        // Re-enable player movement and look
        if (playerController != null) playerController.enabled = true;
        if (firstPersonController != null) firstPersonController.enabled = true;
        if (firstPersonLook != null) firstPersonLook.enabled = true;
    
        // Lock cursor again
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Crosshair.SetActive(true); // Show crosshair when exiting Roulette
    
        // Disable Roulette UI
        if (rouletteUIParent != null)
            rouletteUIParent.SetActive(false);
    
        // Clear any active bets when exiting
        ClearBets();

        // Update balance one last time before exiting
        UpdateBalanceDisplay();
    }

    public void SelectChipValue(int value)
    {
        if (!bettingOpen) return;
    
        // If the same chip value is clicked again, just add to it instead of resetting
        if (selectedChipValue == value)
        {
            // Do nothing, we'll just keep the same value
            // The highlighting will show the user which chip is selected
        }
        else
        {
            // New chip selected, update the selected value
            selectedChipValue = value;
        }
    }

    public void PlaceBet(string betType)
    {
        if (!bettingOpen || selectedChipValue <= 0) return;

        // Check if player has enough money to place this bet
        if (balanceManager != null)
        {
            bool success = balanceManager.TrySpendMoney(selectedChipValue);
            if (!success)
            {
                // Display insufficient funds message
                string currentText = resultText.text;
                resultText.text = "Insufficient funds for this bet!";
                
                // Restore previous message after a delay
                StartCoroutine(RestoreTextAfterDelay(currentText, 2.0f));
                return;
            }
            
            // Update balance display immediately after successful bet
            UpdateBalanceDisplay();
        }

        // Add bet to current bets
        if (currentBets.ContainsKey(betType))
            currentBets[betType] += selectedChipValue;
        else
            currentBets[betType] = selectedChipValue;

        UpdateBetDisplay();
    }

    private System.Collections.IEnumerator RestoreTextAfterDelay(string textToRestore, float delay)
    {
        yield return new WaitForSeconds(delay);
        resultText.text = textToRestore;
    }

    void UpdateBetDisplay()
    {
        if (currentBets.Count == 0)
        {
            currentBetText.text = "No bets placed";
            return;
        }

        string betDisplay = "Current Bets:\n";
        foreach (var bet in currentBets)
        {
            betDisplay += $"{bet.Key}: ${bet.Value}\n";
        }
        currentBetText.text = betDisplay;
    }

    public void StartDeal()
    {
        if (!bettingOpen || currentBets.Count == 0 || isSpinning) return;
        
        // Close betting and disable controls
        bettingOpen = false;
        isSpinning = true;
        resultText.text = "Spinning...";

        // Play spin sound
        SoundManager.Instance.PlayRouletteSpin();
        // Start spin animation
        Invoke("Spin", spinAnimationTime);
    }

    public void ClearBets()
    {
        if (!bettingOpen || isSpinning) return;
    
        // Refund all current bets to the player
        if (balanceManager != null)
        {
            int totalRefund = 0;
            foreach (var bet in currentBets)
            {
                totalRefund += bet.Value;
            }
        
            if (totalRefund > 0)
            {
                balanceManager.AddMoney(totalRefund);
                resultText.text = $"Bets cleared. ${totalRefund} returned to your balance.";
                
                // Update balance display immediately after refund
                UpdateBalanceDisplay();
            }
        }
    
        currentBets.Clear();
        UpdateBetDisplay();
    }

    private void Spin()
    {
        ApiManager.Instance.ClearPrompt();
        
        // Simulate spinning the roulette wheel
        int result = Random.Range(0, 37);
        lastResult = result; // Store the result
        
        // Simple text output without color formatting
        resultText.text = $"Ball landed on {result}";
        
        // Keep the color variable for AI prompt only
        string resultColor = result == 0 ? "green" : (IsRed(result) ? "red" : "black");

        // Process all current bets
        int totalBetAmount = 0;
        int totalWinAmount = 0;
        Dictionary<string, int> winningBets = new Dictionary<string, int>();
        Dictionary<string, int> losingBets = new Dictionary<string, int>();

        // Calculate total bet amount (for display purposes only)
        foreach (var bet in currentBets)
        {
            totalBetAmount += bet.Value;
        }

        // Process each bet to see which won and which lost
        foreach (var bet in currentBets)
        {
            if (CheckWin(result, bet.Key))
            {
                int winAmount = CalculateWinnings(bet.Key, bet.Value);
                totalWinAmount += winAmount;
                winningBets.Add(bet.Key, winAmount);
            }
            else
            {
                losingBets.Add(bet.Key, bet.Value);
            }
        }

        // Add winnings to player's balance
        if (balanceManager != null && totalWinAmount > 0)
        {
            balanceManager.AddMoney(totalWinAmount);
            
            // Update balance display after adding winnings
            UpdateBalanceDisplay();
        }

        // Display simplified results
        string resultMessage;
        if (winningBets.Count > 0)
        {
            resultMessage = $"\nCongratulations! You won ${totalWinAmount}!";
            SoundManager.Instance.PlayWinSound();
        }
        else
        {
            resultMessage = "\nBetter luck next time!";
            SoundManager.Instance.PlayLoseSound();
        }
        
        resultText.text += resultMessage;
        
        // Create a description of the bets for AI
        string betsDescription = "";
        foreach (var bet in currentBets)
        {
            betsDescription += $"{bet.Key}: ${bet.Value}, ";
        }

        if (ApiManager.Instance.shouldShowPrompts)
        {
            string prompt = $"Roulette wheel landed on {result} ({resultColor}). " +
                            $"Player's bets were: {betsDescription} " +
                            $"Total winnings: ${totalWinAmount}. " +
                            $"The new total balance for the player is {balanceManager.GetBalance()}. " +
                            $"Give a brief croupier comment about this roulette spin.";

            // Send to AI
            ApiManager.Instance.SendGameUpdate(prompt);
        }
        
        // Reset game immediately without delay
        ResetGame();
    }

    private void ResetGame()
    {
        currentBets.Clear();
        selectedChipValue = 0;
        bettingOpen = true;
        isSpinning = false;
        UpdateBetDisplay();
    }

    // Processes bet results and calculates winnings based on bet type
    private int CalculateWinnings(string betType, int betAmount)
    {
        // Single number bet (including 0)
        if (int.TryParse(betType, out int number) && number >= 0 && number <= 36)
        {
            return betAmount * 36; // 35:1 payout plus original bet
        }
    
        // Even money bets
        if (betType == "Red" || betType == "Black" || 
            betType == "Odd" || betType == "Even" || 
            betType == "1to18" || betType == "19to36")
        {
            return betAmount * 2; // 1:1 payout plus original bet
        }
    
        // Dozen bets
        if (betType == "1st 12" || betType == "2nd 12" || betType == "3rd 12")
        {
            return betAmount * 3; // 2:1 payout plus original bet
        }
    
        return 0; // Unknown bet type
    }

    // Validates if a bet wins based on the spin result
    private bool CheckWin(int result, string betType)
    {
        bool isWin = false;
    
        // Handles all roulette bet types
        switch (betType)
        {
            case "Red":
                isWin = IsRed(result);
                break;
            
            case "Black":
                isWin = IsBlack(result);
                break;
            
            case "Odd":
                isWin = result % 2 != 0 && result != 0;
                break;
            
            case "Even":
                isWin = result % 2 == 0 && result != 0;
                break;
            
            case "1to18":
                isWin = result >= 1 && result <= 18;
                break;
            
            case "19to36":
                isWin = result >= 19 && result <= 36;
                break;
            
            case "1st 12":
                isWin = result >= 1 && result <= 12;
                break;
            
            case "2nd 12":
                isWin = result >= 13 && result <= 24;
                break;
            
            case "3rd 12":
                isWin = result >= 25 && result <= 36;
                break;
            
            default:
                // Check if it's a single number bet
                if (int.TryParse(betType, out int betNumber))
                {
                    isWin = result == betNumber;
                }
                else
                {
                    Debug.LogWarning($"Unknown bet type: {betType}");
                    isWin = false;
                }
                break;
        }
    
        return isWin;
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

    // Add this public method:
    public bool IsPlayingRoulette()
    {
        return isPlayingRoulette;
    }
}