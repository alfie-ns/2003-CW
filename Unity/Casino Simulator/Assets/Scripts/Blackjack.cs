using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

// This class implements a Blackjack game in Unity, handling all game logic and UI interactions
public class Blackjack : MonoBehaviour
{
    // UI references for disPlayingBlackjack game state (hands, balance, bet amount, results)
    [Header("UI Elements")]
    public TMP_Text playerHandText;
    public TMP_Text dealerHandText;
    public TMP_Text balanceText;
    public TMP_Text betText;
    public TMP_Text resultText;
    
    [Header("Buttons")]
    public Button[] chipButtons; // 1,5,10,25,100 value buttons
    public Button dealButton;
    public Button hitButton;
    public Button standButton;
    public Button clearBetButton;

    [Header("Game Interaction")]
    public float interactionRange = 2f;
    public GameObject blackjackUIParent;
    public GameObject playerObject;
    public GameObject Crosshair;
    public Transform playerStandPoint;
    
    [Header("Balance Management")]
    public MonoBehaviour balanceManagerObject; // Reference to the object with IBalanceManager implementation
    private IBalanceManager balanceManager;

    private bool isPlayingBlackjack = false;
    private Vector3 savedPlayerPosition;
    private Quaternion savedPlayerRotation;
    private CharacterController playerController;
    private FirstPersonController firstPersonController;
    private FirstPersonLook firstPersonLook;
    private List<Card> deck;
    private List<Card> playerHand;
    private List<Card> dealerHand;
    private int currentBet;
    private bool isPlayerTurn;

    // Card class to represent individual cards
    private class Card
    {
        public string Suit { get; set; }
        public string Rank { get; set; }
        public int Value { get; set; }

        public override string ToString()
        {
            return $"{Rank} of {Suit}";
        }
    }

    // Initializes the game on startup
    void Start()
    {
        // Get the balance manager
        balanceManager = balanceManagerObject as IBalanceManager;
        if (balanceManager == null)
        {
            Debug.LogError("Balance Manager not found or doesn't implement IBalanceManager!");
        }
        
        // Setup initial game state and UI
        InitializeChipButtons();
        SetupButtons();
        ResetGame();
        UpdateUI();
    }

    void Awake()
    {
        // Disable Blackjack UI at start
        if (blackjackUIParent != null)
            blackjackUIParent.SetActive(false);
    }

    void Update()
    {
        if (!isPlayingBlackjack && Input.GetKeyDown(KeyCode.E))
        {
            // First check if player is in range
            float distance = Vector3.Distance(playerObject.transform.position, transform.position);
        
            if (distance <= interactionRange)
            {
                // Then check if player is looking at the blackjack table
                Camera playerCamera = playerObject.GetComponentInChildren<Camera>();
                if (playerCamera != null)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionRange))
                    {
                        // Check if the object hit is this blackjack table
                        if (hit.transform == transform || hit.transform.IsChildOf(transform))
                        {
                            Debug.Log("Player is looking at the blackjack table");
                            StartBlackjack();
                        }
                        else
                        {
                            Debug.Log("Player is looking at " + hit.transform.name + " but not the blackjack table");
                        }
                    }
                    else
                    {
                        Debug.Log("Player is not looking at anything within interaction range");
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
                        Debug.Log("Player is facing the blackjack table (fallback check)");
                        StartBlackjack();
                    }
                    else
                    {
                        Debug.Log("Player is not facing the blackjack table");
                    }
                }
            }
        }
        else if (isPlayingBlackjack && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitBlackjack();
        }
    }

    void StartBlackjack()
    {
        isPlayingBlackjack = true;
    
        Debug.Log("Starting Blackjack game");

        // Find and disable the SlotMachine UI first
        GameObject slotMachineUI = GameObject.Find("slotMachineUIParent");
        if (slotMachineUI != null)
        {
            slotMachineUI.SetActive(false);
            Debug.Log("SlotMachine UI disabled");
        }

        Crosshair.SetActive(false); // Hide crosshair when playing Blackjack

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

        // FIXED: Use the designated player stand point
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
                Debug.Log($"Player looking at table from position: {playerStandPoint.position} to table: {transform.position}");
            }
            else
            {
                // Fall back to using the stand point's rotation if calculation fails
                playerObject.transform.rotation = playerStandPoint.rotation;
                Debug.Log("Using standPoint rotation as fallback");
            }
        
            Debug.Log($"Moved player to designated stand point: {playerStandPoint.position}");
        }
        else
        {
            Debug.LogError("Player Stand Point is not assigned! Please assign it in the Inspector.");
        
            // Fallback to the old method if stand point is missing
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
                Debug.Log($"Player looking at table from calculated position: {newPosition} to table: {tablePosition}");
            }
        }
    
        // Show cursor and enable UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Enable Blackjack UI
        if (blackjackUIParent != null)
        {
            blackjackUIParent.SetActive(true);
            Debug.Log("Blackjack UI activated: " + blackjackUIParent.name);
        }
        else
        {
            Debug.LogError("blackjackUIParent is null! Make sure it's assigned in the Inspector.");
        }

        ResetGame();
    }

    void ExitBlackjack()
    {
        isPlayingBlackjack = false;
    
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

        Crosshair.SetActive(true); // Show crosshair when exiting Blackjack
    
        // Disable Blackjack UI
        if (blackjackUIParent != null)
            blackjackUIParent.SetActive(false);
    
        ResetGame();
    }

    void InitializeChipButtons()
    {
        int[] chipValues = { 1, 5, 10, 25, 100 };
        for (int i = 0; i < chipButtons.Length; i++)
        {
            int value = chipValues[i];
            chipButtons[i].onClick.AddListener(() => AddToBet(value));
        }
    }

    void SetupButtons()
    {
        dealButton.onClick.AddListener(Deal);
        hitButton.onClick.AddListener(Hit);
        standButton.onClick.AddListener(Stand);
        clearBetButton.onClick.AddListener(ClearBet);
    
        SetGameButtonsInteractable(false);
    }

    void InitializeDeck()
    {
        deck = new List<Card>();
        string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };
        string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

        foreach (string suit in suits)
        {
            for (int i = 0; i < ranks.Length; i++)
            {
                int value = i + 1;
                if (value > 10) value = 10;
                deck.Add(new Card { Suit = suit, Rank = ranks[i], Value = value });
            }
        }

        // Shuffle deck
        deck = deck.OrderBy(x => Random.value).ToList();
    }

    void AddToBet(int amount)
    {
        if (balanceManager.TrySpendMoney(amount))
        {
            // Clear result text when player starts betting for a new game
            if (currentBet == 0)
            {
                resultText.text = "";
            }
            
            currentBet += amount;
            UpdateUI();
        }
    }

    void ClearBet()
    {
        balanceManager.AddMoney(currentBet);
        currentBet = 0;
        resultText.text = "";
        UpdateUI();
    }

    void Deal()
    {
        if (currentBet <= 0) return;

        InitializeDeck();
        playerHand = new List<Card>();
        dealerHand = new List<Card>();

        // Deal initial cards
        playerHand.Add(DrawCard());
        dealerHand.Add(DrawCard());
        playerHand.Add(DrawCard());
        dealerHand.Add(DrawCard());
    
        isPlayerTurn = true;
        SetGameButtonsInteractable(true);
        dealButton.interactable = false;
        UpdateUI();

        if (GetHandValue(playerHand) == 21)
        {
            ProcessBlackjack();
        }
    }

    Card DrawCard()
    {
        if (deck.Count == 0) return null;
        Card card = deck[0];
        deck.RemoveAt(0);
        return card;
    }

    // Handles player actions (Hit and Stand)
    void Hit()
    {
        if (!isPlayerTurn) return;

        playerHand.Add(DrawCard());

        if (GetHandValue(playerHand) > 21)
        {
            ProcessBust();
        }

        UpdateUI();
    }

    void Stand()
    {
        if (!isPlayerTurn) return;
    
        isPlayerTurn = false;
        PlayDealerTurn();
    }

    void PlayDealerTurn()
    {
        while (GetHandValue(dealerHand) < 17)
        {
            dealerHand.Add(DrawCard());
        }

        DetermineWinner();
        SetGameButtonsInteractable(false);
        dealButton.interactable = true;
        UpdateUI();
    }

    // Calculates the total value of a hand, handling Aces as 1 or 11
    int GetHandValue(List<Card> hand)
    {
        // Special handling for Aces: initially count as 11, can reduce to 1 if would bust
        int value = 0;
        int aces = 0;

        foreach (Card card in hand)
        {
            if (card.Rank == "A")
            {
                aces++;
                value += 11;
            }
            else
            {
                value += card.Value;
            }
        }

        while (value > 21 && aces > 0)
        {
            value -= 10;
            aces--;
        }

        return value;
    }

    // Determines the winner between dealer and player hands
    void DetermineWinner()
    {
        int dealerValue = GetHandValue(dealerHand);
        int playerValue = GetHandValue(playerHand);

        if (playerValue <= 21)
        {
            if (dealerValue > 21 || playerValue > dealerValue)
            {
                balanceManager.AddMoney(currentBet * 2);
                resultText.text = "Win!";
            }
            else if (playerValue == dealerValue)
            {
                balanceManager.AddMoney(currentBet);
                resultText.text = "Push";
            }
            else
            {
                resultText.text = "Lose";
            }
        }

        currentBet = 0;
    }

    // Special handling for when player gets Blackjack (21 on initial deal)
    void ProcessBlackjack()
    {
        // Pays out at 3:2 odds (2.5x bet)
        int winnings = (int)(currentBet * 2.5f);
        balanceManager.AddMoney(winnings);
        resultText.text = "Blackjack!";
        currentBet = 0;
        SetGameButtonsInteractable(false);
        dealButton.interactable = true;
    }

    void ProcessBust()
    {
        resultText.text = "Bust!";
        currentBet = 0;
        SetGameButtonsInteractable(false);
        dealButton.interactable = true;
    }

    void SetGameButtonsInteractable(bool interactable)
    {
        hitButton.interactable = interactable;
        standButton.interactable = interactable;
    }

    // Updates all UI elements with current game state
    void UpdateUI()
    {
        // Only update the balance text if the manager is available
        if (balanceManager != null)
        {
            balanceText.text = $"${balanceManager.GetBalance()}";
        }
        
        betText.text = $"${currentBet}";
        
        playerHandText.text = "Player Hand:" + string.Join(", ", playerHand) + 
            $" (Value: {GetHandValue(playerHand)})";

        dealerHandText.text = "Dealer Hand:" + 
            (isPlayerTurn ? dealerHand[0].ToString() + ", ?" : string.Join(", ", dealerHand)) +
            (isPlayerTurn ? "" : $" (Value: {GetHandValue(dealerHand)})");
    }

    void ResetGame()
    {
        playerHand = new List<Card>();
        dealerHand = new List<Card>();
        resultText.text = "";
        isPlayerTurn = false;
        currentBet = 0;
        UpdateUI();
    }
}