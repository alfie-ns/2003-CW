using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SlotMachine : MonoBehaviour, IPointerClickHandler
{
    // Configuration for betting limits and auto-spin options
    public int minBet = 1;
    public int maxBet = 10;
    public int[] autoSpinOptions = { 10, 25, 50, 100, 200, 500 };

    // UI References for buttons, text displays and game objects
    [Header("UI Elements")]
    public Text betAmountText; // Shows current bet amount
    public GameObject increaseBetButton; // Button to increase bet
    public GameObject decreaseBetButton; // Button to decrease bet
    public GameObject autoSpinButton; // Button to cycle through auto-spin options
    public GameObject startAutoSpinButton;
    public GameObject stopAutoSpinButton;
    public GameObject lever; // Physical lever player pulls to spin
    public GameObject[] rollers; // The spinning reels of the slot machine
    public Text resultText; // Displays win/lose outcome
    public Text interactionText; // Shows interaction prompts
    public Text autoSpinText;
    public Text exitText;
    public Text amountText;
    public Text autoText;

    private int betAmount; // Current bet amount
    private int autoSpinsRemaining; // Number of automatic spins left
    private bool isSpinning; // Whether reels are currently spinning
    private float[] stopTimes; // When each roller should stop spinning
    private string[] symbols = { "Bar", "Seven", "Bell", "Cherry" }; // Possible symbols on rollers
    private int currentAutoSpinIndex = 0; // Index into autoSpinOptions array
    private bool isInteracting = false; // Whether player is currently using machine
    private FirstPersonMovement playerMovement;
    private Transform playerTransform;
    private BoxCollider slotMachineCollider;

    void Start()
    {
        betAmount = minBet;
        UpdateBetAmountText();
        UpdateAutoSpinButtonText();
        interactionText.text = "Press E to interact";
        exitText.text = "";
        SetUIElementsActive(false);

        playerMovement = FindObjectOfType<FirstPersonMovement>();
        playerTransform = playerMovement.transform;
        slotMachineCollider = GetComponent<BoxCollider>();
    }

    void Update()
    {
        if (isInteracting)
        {
            if (autoSpinsRemaining > 0 && !isSpinning)
            {
                StartSpin();
                autoSpinsRemaining--;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                ExitInteraction();
            }
        }
        else
        {
            CheckLookingAtMachine();
        }

        // Detect mouse click
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedObject = hit.collider.gameObject;
                PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
                {
                    pointerCurrentRaycast = new RaycastResult { gameObject = clickedObject }
                };

                OnPointerClick(pointerEventData);
            }
        }
    }

    // Player interaction logic
    private void CheckLookingAtMachine()
    {
        // Check if player is within range and looking at machine
        // Enable/disable interaction prompts accordingly
        if (Vector3.Distance(playerTransform.position, transform.position) <= 2f)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    interactionText.enabled = true;
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        StartInteraction();
                    }
                }
                else
                {
                    interactionText.enabled = false;
                }
            }
            else
            {
                interactionText.enabled = false;
            }
        }
        else
        {
            interactionText.enabled = false;
        }
    }

    private void StartInteraction()
    {
        isInteracting = true;
        interactionText.enabled = false;
        SetUIElementsActive(true);
        exitText.text = "Press C to exit";
        playerMovement.EnableMovement(false);

        // Disable the slot machine's box collider
        slotMachineCollider.enabled = false;
    }

    private void ExitInteraction()
    {
        isInteracting = false;
        interactionText.enabled = true;
        SetUIElementsActive(false);
        exitText.text = "";
        playerMovement.EnableMovement(true);

        // Enable the slot machine's box collider
        slotMachineCollider.enabled = true;
    }

    private void SetUIElementsActive(bool isActive)
    {
        resultText.enabled = isActive;
        autoSpinText.enabled = isActive;
        betAmountText.enabled = isActive;
        exitText.enabled = isActive;
        amountText.enabled = isActive;
        autoText.enabled = isActive;
    }

    public void SetBetAmount(int amount)
    {
        betAmount = Mathf.Clamp(amount, minBet, maxBet);
        UpdateBetAmountText();
    }

    private void UpdateBetAmountText()
    {
        betAmountText.text = betAmount.ToString();
    }

    private void IncreaseBet()
    {
        SetBetAmount(betAmount + 1);
        Debug.Log("Increase bet");
    }

    private void DecreaseBet()
    {
        SetBetAmount(betAmount - 1);
        Debug.Log("Decrease bet");
    }

    private void CycleAutoSpinOption()
    {
        currentAutoSpinIndex = (currentAutoSpinIndex + 1) % autoSpinOptions.Length;
        UpdateAutoSpinButtonText();
        Debug.Log("Cycle auto spin option");
    }

    private void UpdateAutoSpinButtonText()
    {
        autoSpinText.text = autoSpinOptions[currentAutoSpinIndex].ToString();
    }

    private void StartAutoSpins()
    {
        autoSpinsRemaining = autoSpinOptions[currentAutoSpinIndex];
    }

    private void StopAutoSpins()
    {
        autoSpinsRemaining = 0;
    }

    private void StartSpin()
    {
        if (isSpinning) return;

        Debug.Log("StartSpin called");

        isSpinning = true;
        stopTimes = new float[rollers.Length];
        for (int i = 0; i < rollers.Length; i++)
        {
            stopTimes[i] = Time.time + Random.Range(1f, 3f) + i * 0.5f; // Staggered stop times
            Debug.Log($"Stop time for roller {i}: {stopTimes[i]}");
        }
        StartCoroutine(SpinSlots());
    }

    // Spinning mechanics
    private IEnumerator SpinSlots()
    {
        // Main slot machine spinning logic
        // - Spins each roller independently
        // - Checks for winning combinations when all rollers stop
        // - Updates UI with results
        Debug.Log("SpinSlots coroutine started");

        while (isSpinning)
        {
            Debug.Log("Spinning...");
            bool allStopped = true;
            for (int i = 0; i < rollers.Length; i++)
            {
                if (Time.time < stopTimes[i])
                {
                    int symbolIndex = Random.Range(0, symbols.Length);
                    Debug.Log($"Roller {i} symbol: {symbols[symbolIndex]}");
                    StartCoroutine(SpinRoller(rollers[i], symbolIndex));
                    allStopped = false;
                }
            }

            if (allStopped)
            {
                isSpinning = false;
                CheckWin();
            }

            yield return null;
        }
        Debug.Log("SpinSlots coroutine ended");
    }

    private IEnumerator SpinRoller(GameObject roller, int symbolIndex)
    {
        Debug.Log($"SpinRoller started for roller with symbol index {symbolIndex}");

        // Define the target rotation based on the symbol index
        float targetAngle = 360.0f / symbols.Length * symbolIndex;

        // Define the duration of the spin
        float duration = 1.0f; // 1 second
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

        Debug.Log($"SpinRoller ended for roller with symbol index {symbolIndex}");
    }

    private void CheckWin()
    {
        // Get final symbols from roller positions
        // Check if all symbols match for a win
        // Update UI and trigger win/lose effects
        if (rollers == null || rollers.Length == 0)
        {
            Debug.LogError("rollers array is not initialized or empty.");
            return;
        }

        // Create an array to store the symbols
        string[] landedSymbols = new string[3];
        for (int i = 0; i < 3; i++)
        {
            float angle = rollers[i].transform.rotation.eulerAngles.y;
            int symbolIndex = Mathf.RoundToInt(angle / (360.0f / symbols.Length)) % symbols.Length;
            landedSymbols[i] = symbols[symbolIndex];
        }

        // Check for a win (all symbols in the array are the same)
        bool isWin = landedSymbols[0] == landedSymbols[1] && landedSymbols[1] == landedSymbols[2];

        if (isWin)
        {
            resultText.text = "You win!";
            Debug.Log("You win!");
        }
        else
        {
            resultText.text = "You lose!";
            Debug.Log("You lose!");
        }

        // Start coroutine to hide the result text after 2 seconds
        StartCoroutine(HideResultTextAfterDelay(2f));
    }

    private IEnumerator HideResultTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        resultText.text = "";
    }

    // UI Event handling
    public void OnPointerClick(PointerEventData eventData)
    {
        // Handles clicks on various UI elements:
        // - Bet amount adjustment
        // - Auto-spin controls
        // - Lever pull to start spin
        if (isInteracting)
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
            else if (clickedObject == lever)
            {
                StartSpin();
                Debug.Log("lever clicked");
            }
        }
    }
}