using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SlotMachine : MonoBehaviour, IPointerClickHandler
{
    // Enum definition needs to be outside of any attribute
    public enum PositionDirection { Left, Right, Forward, Back, Custom }

    // Configuration for betting limits and auto-spin options
    public int minBet = 1;
    public int maxBet = 100;
    public int[] autoSpinOptions = { 10, 25, 50, 100, 200, 500 };
    private int[] predefinedBets = { 1, 2, 5, 10, 25, 50, 100 };
    private int currentBetIndex = 0;

    // UI References for GameObjects and text displays
    [Header("UI Elements")]
    public TMP_Text betAmountText; // Shows current bet amount
    public GameObject increaseBetButton; // GameObject to increase bet
    public GameObject decreaseBetButton; // GameObject to decrease bet
    public GameObject autoSpinButton; // GameObject to cycle through auto-spin options
    public GameObject startAutoSpinButton;
    public GameObject stopAutoSpinButton;
    public GameObject leverObject; // GameObject for the lever
    public GameObject[] rollers; // The spinning reels of the slot machine
    public TMP_Text resultText; // Displays win/lose outcome
    public TMP_Text autoSpinText;
    public TMP_Text amountText;
    public TMP_Text balanceText; // Shows current balance
    public GameObject slotMachineUIParent; // Parent object containing all UI elements

    [Header("Game Interaction")]
    public float interactionRange = 2f;
    public GameObject playerObject;

    [Header("Balance Management")]
    public MonoBehaviour balanceManagerObject; // Reference to the object with IBalanceManager implementation
    private IBalanceManager balanceManager;

    [Header("Automatic Positioning")]
    public PositionDirection positionDirection = PositionDirection.Right;
    public float standDistance = 1.2f;

    [Header("Player Positioning")]
    public Transform playerStandPoint; // Empty GameObject child you'll place where the player should stand
    public bool useCustomStandPoint = false; // Toggle to use either automatic or manual positioning

    private int betAmount; // Current bet amount
    private int autoSpinsRemaining; // Number of automatic spins left
    private bool isSpinning; // Whether reels are currently spinning
    private float[] stopTimes; // When each roller should stop spinning
    private string[] symbols = { "Bar", "Seven", "Bell", "Cherry" }; // Possible symbols on rollers
    private int currentAutoSpinIndex = 0; // Index into autoSpinOptions array
    private bool isPlayingSlotMachine = false; // Whether player is currently using machine
    
    private Vector3 savedPlayerPosition;
    private Quaternion savedPlayerRotation;
    private CharacterController playerController;
    private FirstPersonController playerMovementController;
    private FirstPersonLook firstPersonLook;
    private bool canStartNextSpin = true;
    
    void Start()
    {
        // Initialize bet amount to minimum bet
        betAmount = minBet;
        UpdateBetAmountText();
        UpdateAutoSpinButtonText();
    
        // Get the balance manager
        if (balanceManagerObject != null)
        {
            balanceManager = balanceManagerObject as IBalanceManager;
            if (balanceManager == null)
            {
                Debug.LogError("Balance Manager object found but doesn't implement IBalanceManager interface!");
            }
        }
        else
        {
            Debug.LogError("Balance Manager object not assigned in inspector!");
        }
    
        // Disable UI at start
        if (slotMachineUIParent != null)
            slotMachineUIParent.SetActive(false);
    }
    void Awake()
    {
        // Disable SlotMachine UI at start
        if (slotMachineUIParent != null)
            slotMachineUIParent.SetActive(false);
    }

    void Update()
    {
        if (!isPlayingSlotMachine && Input.GetKeyDown(KeyCode.E))
        {
            // First check if player is in range
            float distance = Vector3.Distance(playerObject.transform.position, transform.position);
            
            if (distance <= interactionRange)
            {
                // Then check if player is looking at the slot machine
                Camera playerCamera = playerObject.GetComponentInChildren<Camera>();
                if (playerCamera != null)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionRange))
                    {
                        // Check if the object hit is this slot machine
                        if (hit.transform == transform || hit.transform.IsChildOf(transform))
                        {
                            StartSlotMachine();
                        }
                    }
                }
                else
                {
                    
                    // Fallback if no camera is found - just check if player is facing the table
                    Vector3 directionToSlotMachine = transform.position - playerObject.transform.position;
                    directionToSlotMachine.y = 0; // Ignore height difference
                    
                    // Get forward direction of player
                    Vector3 playerForward = playerObject.transform.forward;
                    playerForward.y = 0; // Ignore height difference
                    
                    // Check if player is roughly facing the slot machine (dot product > 0.5 means < 60 degree angle)
                    if (Vector3.Dot(playerForward.normalized, directionToSlotMachine.normalized) > 0.5f)
                    {
                        StartSlotMachine();
                    }
                    else
                    {
                        Debug.Log("Player is not facing the slot machine");
                    }
                }
            }
        }
        else if (isPlayingSlotMachine && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitSlotMachine();
        }
        
        if (isPlayingSlotMachine && !isSpinning && autoSpinsRemaining > 0 && canStartNextSpin)
        {
            // Temporarily prevent more spins until delay completes
            canStartNextSpin = false;
        
            // Start a coroutine to handle the delay
            StartCoroutine(DelayedAutoSpin());
        }
            if (isPlayingSlotMachine && !isSpinning && Input.GetMouseButtonDown(0))
        {
            // First check if we're clicking on a UI element using EventSystem
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // Handled by the OnPointerClick method via EventSystem
                return;
            }
        
            // If not UI, try a 3D raycast for the lever or other 3D elements
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found!");
                return;
            }
        
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f); // Get all hits, not just the first one
        
            // Sort hits by distance to get closest first
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        
            // Process all hits to find interactive elements
            foreach (RaycastHit hit in hits)
            {
                GameObject hitObject = hit.collider.gameObject;
            
                // Check if this is one of our interactive elements
                if (hitObject == leverObject)
                {
                    StartSpin();
                    break; // Exit the loop once we've handled a click
                }
                else if (hitObject == increaseBetButton)
                {
                    IncreaseBet();
                    Debug.Log("Clicked increase bet button");
                    break;
                }
                else if (hitObject == decreaseBetButton)
                {
                    DecreaseBet();
                    Debug.Log("Clicked decrease bet button");
                    break;
                }
                else if (hitObject == autoSpinButton)
                {
                    CycleAutoSpinOption();
                    Debug.Log("Clicked auto spin button");
                    break;
                }
                else if (hitObject == startAutoSpinButton)
                {
                    StartAutoSpins();
                    Debug.Log("Clicked start auto spin button");
                    break;
                }
                else if (hitObject == stopAutoSpinButton)
                {
                    StopAutoSpins();
                    Debug.Log("Clicked stop auto spin button");
                    break;
                }
            }
        }
    }

    void StartSlotMachine()
    {
        isPlayingSlotMachine = true;
        canStartNextSpin = true;
    
        // Reset the bet amount to minimum when starting
        currentBetIndex = 0; // Set to first bet option (1)
        betAmount = predefinedBets[currentBetIndex];
        UpdateBetAmountText();
    
        // Update the balance display - KEEP THIS SEPARATE FROM BET AMOUNT
        UpdateBalanceDisplay();

            // Find and disable the Blackjack UI first
            GameObject blackjackUI = GameObject.Find("blackjackUIParent");
            if (blackjackUI != null)
            {
                blackjackUI.SetActive(false);
            }

            // Get and disable player components first
            playerController = playerObject.GetComponent<CharacterController>();
            playerMovementController = playerObject.GetComponent<FirstPersonController>();
            firstPersonLook = playerObject.GetComponentInChildren<FirstPersonLook>();

            // Save player's position and rotation
            savedPlayerPosition = playerObject.transform.position;
            savedPlayerRotation = playerObject.transform.rotation;

            // Disable movement but let camera look work
            if (playerController != null) playerController.enabled = false;
            if (playerMovementController != null) playerMovementController.enabled = false;
        
            // IMPROVED: Use the designated player stand point if available
            if (playerStandPoint != null)
            {
                // Move player to the designated position
                playerObject.transform.position = playerStandPoint.position;
            
                // Calculate the direction from the player to the slot machine
                Vector3 lookDirection = transform.position - playerStandPoint.position;
                lookDirection.y = 0; // Keep the look direction level (no looking up/down)
            
                // Make the player face the slot machine
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
                // Fallback to the old method if stand point is missing
                // Determine offset based on selected direction
                Vector3 slotPosition = transform.position;
                Vector3 offset;
            
                switch (positionDirection)
                {
                    case PositionDirection.Left:
                        offset = -transform.right * standDistance;
                        break;
                    case PositionDirection.Right:
                        offset = transform.right * standDistance;
                        break;
                    case PositionDirection.Forward:
                        offset = transform.forward * standDistance;
                        break;
                    case PositionDirection.Back:
                        offset = -transform.forward * standDistance;
                        break;
                    case PositionDirection.Custom:
                        // Use a custom vector if needed
                        offset = new Vector3(1, 0, 1).normalized * standDistance;
                        break;
                    default:
                        offset = transform.forward * standDistance;
                        break;
                }

                // Position the player at the selected offset
                Vector3 newPosition = slotPosition + offset;
                newPosition.y = savedPlayerPosition.y; // Keep original height
            
                // Make player face the slot machine
                Vector3 lookDirection = slotPosition - newPosition;
                lookDirection.y = 0; // Keep look direction level (no looking up/down)
            
                // Apply position and rotation
                playerObject.transform.position = newPosition;
            
                if (lookDirection != Vector3.zero)
                {
                    playerObject.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                }
            }

            // Enable slot machine UI
            if (slotMachineUIParent != null)
            {
                slotMachineUIParent.SetActive(true);
            }
            else
            {
                Debug.LogError("slotMachineUIParent is null! Make sure it's assigned in the Inspector.");
            }
    }

    void ExitSlotMachine()
    {
        if (isSpinning) return; // Don't exit while spinning
    
        isPlayingSlotMachine = false;
        canStartNextSpin = true;

        // Restore player's position and rotation
        playerObject.transform.position = savedPlayerPosition;
        playerObject.transform.rotation = savedPlayerRotation;

        // Re-enable player movement and look
        if (playerController != null) playerController.enabled = true;
        if (playerMovementController != null) playerMovementController.enabled = true;

        // Disable slot machine UI
        if (slotMachineUIParent != null)
            slotMachineUIParent.SetActive(false);

        // Reset auto spins
        autoSpinsRemaining = 0;
        UpdateAutoSpinCounterText();
    }

    private void SetUIElementsActive(bool isActive)
    {
        if (resultText != null) resultText.enabled = isActive;
        if (autoSpinText != null) autoSpinText.enabled = isActive;
        if (betAmountText != null) betAmountText.enabled = isActive;
        if (amountText != null) amountText.enabled = isActive;
    }

    public void SetBetAmount(int amount)
    {
    
        // Clamp the amount between min and max bet values
        betAmount = Mathf.Clamp(amount, minBet, maxBet);
    
        // Find the closest predefined bet value
        int closestIndex = 0;
        int minDifference = int.MaxValue;
    
        for (int i = 0; i < predefinedBets.Length; i++)
        {
            int diff = Mathf.Abs(predefinedBets[i] - betAmount);
            if (diff < minDifference)
            {
                minDifference = diff;
                closestIndex = i;
            }
        }
    
        currentBetIndex = closestIndex;
        betAmount = predefinedBets[currentBetIndex]; // Ensure exact match to a predefined value
    
        UpdateBetAmountText();
    }

    private void UpdateBetAmountText()
    {
        if (betAmountText != null)
        {
            betAmountText.text = "$" + betAmount.ToString();
        }
    }

    private void IncreaseBet()
    {
        // Increase bet index, but don't exceed array bounds
        currentBetIndex = Mathf.Min(currentBetIndex + 1, predefinedBets.Length - 1);
    
        // Apply the new bet amount
        SetBetAmount(predefinedBets[currentBetIndex]);
    }

    private void DecreaseBet()
    {
        currentBetIndex = Mathf.Max(currentBetIndex - 1, 0);
        SetBetAmount(predefinedBets[currentBetIndex]);
    }

    private void CycleAutoSpinOption()
    {
        currentAutoSpinIndex = (currentAutoSpinIndex + 1) % autoSpinOptions.Length;
        UpdateAutoSpinCounterText();
    }

    private void UpdateAutoSpinButtonText()
    {
        if (autoSpinText != null)
        {
            autoSpinText.text = autoSpinOptions[currentAutoSpinIndex].ToString();
        }
    }

    public void StartAutoSpins()
    {
        autoSpinsRemaining = autoSpinOptions[currentAutoSpinIndex];
        UpdateAutoSpinCounterText();
    }

    public void StopAutoSpins()
    {
        autoSpinsRemaining = 0;
        UpdateAutoSpinCounterText();
    }

    public void StartSpin()
    {
        if (isSpinning) return;
    
        // Check if player has enough money to place the bet
        if (balanceManager == null)
        {
            if (resultText != null)
                resultText.text = "Error: Balance system unavailable!";
            StartCoroutine(HideResultTextAfterDelay(2f));
            return;
        }
    
        if (!balanceManager.TrySpendMoney(betAmount))
        {
            if (resultText != null)
                resultText.text = "Insufficient funds!";
            StartCoroutine(HideResultTextAfterDelay(2f));
            return;
        }

        UpdateBalanceDisplay();

        isSpinning = true;
        stopTimes = new float[rollers.Length];
        for (int i = 0; i < rollers.Length; i++)
        {
            stopTimes[i] = Time.time + Random.Range(1f, 3f) + i * 0.5f; // Staggered stop times
        }
        StartCoroutine(SpinSlots());
    }

    // IPointerClickHandler implementation - for GameObject interaction
    public void OnPointerClick(PointerEventData eventData)
    {
        
        if (isPlayingSlotMachine && !isSpinning)
        {
            GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;

            if (clickedObject == increaseBetButton)
            {
                IncreaseBet();
            }
            else if (clickedObject == decreaseBetButton)
            {
                DecreaseBet();
            }
            else if (clickedObject == autoSpinButton)
            {
                CycleAutoSpinOption();
            }
            else if (clickedObject == startAutoSpinButton)
            {
                StartAutoSpins();
            }
            else if (clickedObject == stopAutoSpinButton)
            {
                StopAutoSpins();
            }
            else if (clickedObject == leverObject)
            {
                StartSpin();
            }
        }
    }

    // Spinning mechanics
    private IEnumerator SpinSlots()
    {
        // Clear any previous results
        if (resultText != null)
            resultText.text = "";
    
        // Initialize spinning state for all rollers
        bool[] rollerStopped = new bool[rollers.Length];
        float[] spinSpeeds = new float[rollers.Length];
        float[] decelerationRates = new float[rollers.Length];
        int[] targetSymbolIndices = new int[rollers.Length];
    
        // Set up initial speeds and target indices for each roller
        for (int i = 0; i < rollers.Length; i++)
        {
            rollerStopped[i] = false;
            spinSpeeds[i] = Random.Range(720f, 1080f); // Initial speed (degrees/second)
            decelerationRates[i] = spinSpeeds[i] / (stopTimes[i] - Time.time); // Calculate deceleration
            targetSymbolIndices[i] = Random.Range(0, symbols.Length);
        }
    
        // Main spin loop
        while (isSpinning)
        {
            bool allStopped = true;
        
            for (int i = 0; i < rollers.Length; i++)
            {
                if (!rollerStopped[i])
                {
                    // Rotate the roller based on current speed
                    rollers[i].transform.Rotate(0, spinSpeeds[i] * Time.deltaTime, 0);
                
                    // Decrease speed over time
                    if (Time.time < stopTimes[i])
                    {
                        // Still spinning at full or reducing speed
                        spinSpeeds[i] = Mathf.Max(spinSpeeds[i] - (decelerationRates[i] * Time.deltaTime), 0);
                        allStopped = false;
                    }
                    else
                    {
                        // Time to stop - align to exact symbol position
                        float targetAngle = 360.0f / symbols.Length * targetSymbolIndices[i];
                    
                        // Get current rotation in 0-360 range
                        float currentAngle = rollers[i].transform.eulerAngles.y % 360;
                    
                        // Calculate shortest distance to target angle
                        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
                    
                        if (Mathf.Abs(angleDifference) < 2.0f || spinSpeeds[i] < 1.0f)
                        {
                            // Close enough or moving too slowly, snap to exact position
                            rollers[i].transform.rotation = Quaternion.Euler(0, targetAngle, 0);
                            rollerStopped[i] = true;
                        }
                        else
                        {
                            // Still need to align - move slowly toward target
                            float alignSpeed = Mathf.Min(spinSpeeds[i], Mathf.Abs(angleDifference) * 2);
                            float direction = Mathf.Sign(angleDifference);
                        
                            rollers[i].transform.Rotate(0, alignSpeed * direction * Time.deltaTime, 0);
                            spinSpeeds[i] = Mathf.Max(spinSpeeds[i] * 0.95f, 20f); // Slow down but keep minimum speed
                        
                            allStopped = false;
                        }
                    }
                }
            }
        
            if (allStopped)
            {
                isSpinning = false;
                yield return new WaitForSeconds(0.5f); // Short pause before showing result
                CheckWin();
            }
        
            yield return null;
        }
    }

    private IEnumerator SpinRoller(GameObject roller, int symbolIndex)
    {
        // Define the target rotation based on the symbol index
        float targetAngle = 360.0f / symbols.Length * symbolIndex;

        // Define the duration of the spin
        float duration = 0.1f; // Quick rotation
        float elapsedTime = 0.0f;

        // Get the initial rotation
        Quaternion initialRotation = roller.transform.rotation;
        Quaternion finalRotation = Quaternion.Euler(0, targetAngle, 0);

        // Animate the rotation
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            roller.transform.rotation = Quaternion.Slerp(initialRotation, finalRotation, elapsedTime / duration);
            yield return null;
        }

        // Ensure the roller stops exactly at the target angle
        roller.transform.rotation = finalRotation;
    }

    private IEnumerator DelayedAutoSpin()
    {
        // Wait to show the previous result
        yield return new WaitForSeconds(2.0f);

        // Start the next spin
        StartSpin();
        autoSpinsRemaining--;
    
        // Update the display to show remaining auto spins
        UpdateAutoSpinCounterText();

        // Allow the next auto-spin after this one completes
        canStartNextSpin = true;
    }

    private void UpdateAutoSpinCounterText()
    {
        if (autoSpinText != null)
        {
            if (autoSpinsRemaining > 0)
            {
                // Show remaining auto spins
                autoSpinText.text = autoSpinsRemaining.ToString();
            }
            else
            {
                // Reset to show the option value when not auto-spinning
                autoSpinText.text = autoSpinOptions[currentAutoSpinIndex].ToString();
            }
        }
    }

    private void CheckWin()
    {
        // Get the current symbols showing on each roller
        string[] landedSymbols = new string[rollers.Length];
        for (int i = 0; i < rollers.Length; i++)
        {
            // Calculate which symbol is facing forward based on the roller's rotation
            float angle = rollers[i].transform.eulerAngles.y % 360;
            int symbolIndex = Mathf.RoundToInt(angle / (360f / symbols.Length)) % symbols.Length;
            landedSymbols[i] = symbols[symbolIndex];
        }
    
        // Calculate winnings based on the landed symbols
        int winnings = CalculateWinnings(landedSymbols);
    
        if (winnings > 0)
        {
            // Add winnings to player balance
            if (balanceManager != null)
            {
                balanceManager.AddMoney(winnings);
            
                // Update the balance display after adding winnings
                UpdateBalanceDisplay();

                // Prepare prompt for AI response
                string symbolsDisplay = string.Join(", ", landedSymbols);
                string prompt = $"Slot machine landed on: {symbolsDisplay}. " +
                                $"Player bet ${betAmount} and {(winnings > 0 ? $"won ${winnings}" : "lost")}. " +
                                $"Give a quick casino host comment about this spin.";

                // Send to AI
                ApiManager.Instance.SendGameUpdate(prompt);
            }
            else
            {
                Debug.LogError("Cannot add winnings: Balance Manager is not available!");
            }
            
            if (resultText != null)
            {
                resultText.text = $"You win ${winnings}!";
            }
        }
        else  // winnings is 0 or less
        {
            if (resultText != null)
            {
                resultText.text = "You lose!";

                // Add AI commentary for losses too
                string symbolsDisplay = string.Join(", ", landedSymbols);
                string prompt = $"Slot machine landed on: {symbolsDisplay}. " +
                                $"Player bet ${betAmount} and lost. " +
                                $"Give a quick casino host comment about this unlucky spin.";
                
                // Send to AI
                ApiManager.Instance.SendGameUpdate(prompt);
            }
            else
            {
                Debug.LogError("resultText is null, cannot display 'You lose!' message");
            }
        }

        // Only auto-hide the result text if there are no more auto-spins pending
        if (autoSpinsRemaining <= 0)
        {
            StartCoroutine(HideResultTextAfterDelay(3f));
        }
        // Otherwise the text will remain visible until the next spin starts
    }

    private void UpdateBalanceDisplay()
    {
        if (balanceManager != null && balanceText != null)
        {
            int currentBalance = balanceManager.GetBalance();
            balanceText.text = "$" + currentBalance.ToString();
        }
        else
        {
            if (balanceManager == null)
                Debug.LogWarning("Cannot update balance display: balanceManager is null");
            if (balanceText == null)
                Debug.LogWarning("Cannot update balance display: balanceText is null");
        }
    }
    
    private int CalculateWinnings(string[] landedSymbols)
    {
        // Check for all matching symbols (best payout)
        bool allSame = true;
        for (int i = 1; i < landedSymbols.Length; i++)
        {
            if (landedSymbols[i] != landedSymbols[0])
            {
                allSame = false;
                break;
            }
        }
    
        if (allSame)
        {
            // Special multipliers for specific symbols
            switch (landedSymbols[0])
            {
                case "Seven": return betAmount * 15; // Highest payout
                case "Bar": return betAmount * 10;
                case "Bell": return betAmount * 8;
                case "Cherry": return betAmount * 5;
                default: return betAmount * 5;
            }
        }
    
        // Check for pairs
        Dictionary<string, int> symbolCounts = new Dictionary<string, int>();
        foreach (string symbol in landedSymbols)
        {
            if (symbolCounts.ContainsKey(symbol))
                symbolCounts[symbol]++;
            else
                symbolCounts[symbol] = 1;
        }
    
        // Check for three of a kind or pairs
        foreach (var count in symbolCounts)
        {
            if (count.Value == 3) // Three of a kind but not all 3 are the same (already checked above)
                return betAmount * 3;
            else if (count.Value == 2) // Pair
                return betAmount * 2;
        }
    
        // No win - ensure we return 0
        return 0;
    }

    private IEnumerator HideResultTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (resultText != null)
            resultText.text = "";
    }
}