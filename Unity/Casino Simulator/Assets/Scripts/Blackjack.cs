using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

// This class implements a Blackjack game in Unity, handling all game logic and UI interactions
public class Blackjack : MonoBehaviour
{
    // UI references for displaying game state (hands, balance, bet amount, results)
    [Header("UI Elements")]
    // Other UI elements
    public Text playerHandText;
    public Text dealerHandText;
    public Text balanceText;
    public Text betText;
    public Text resultText;
    
    [Header("Buttons")]
    public Button[] chipButtons; // 1,5,10,25,100 value buttons
    public Button dealButton;
    public Button hitButton;
    public Button standButton;
    public Button splitButton;
    public Button clearBetButton;

    private List<Card> deck;
    private List<Card> playerHand;
    private List<Card> dealerHand;
    private List<Card> splitHand;
    private int currentBet;
    private int playerBalance = 1000;
    private bool canSplit;
    private bool isPlayerTurn;
    private bool isSplitHand;

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
        // Setup initial game state and UI
        InitializeChipButtons();
        SetupButtons();
        ResetGame();
        UpdateUI();
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
        splitButton.onClick.AddListener(Split);
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
        if (playerBalance >= amount)
        {
            currentBet += amount;
            playerBalance -= amount;
            UpdateUI();
        }
    }

    void ClearBet()
    {
        playerBalance += currentBet;
        currentBet = 0;
        UpdateUI();
    }

    void Deal()
    {
        if (currentBet <= 0) return;

        InitializeDeck();
        playerHand = new List<Card>();
        dealerHand = new List<Card>();
        splitHand = null;

        // Deal initial cards
        playerHand.Add(DrawCard());
        dealerHand.Add(DrawCard());
        playerHand.Add(DrawCard());
        dealerHand.Add(DrawCard());

        canSplit = playerHand[0].Value == playerHand[1].Value;
        splitButton.interactable = canSplit;
        
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

    // Handles player actions (Hit, Stand, Split)
    void Hit()
    {
        // Adds a card to current hand and checks for bust
        if (!isPlayerTurn) return;

        List<Card> currentHand = isSplitHand ? splitHand : playerHand;
        currentHand.Add(DrawCard());

        if (GetHandValue(currentHand) > 21)
        {
            if (isSplitHand && GetHandValue(playerHand) <= 21)
            {
                isSplitHand = false;
            }
            else
            {
                ProcessBust();
            }
        }

        UpdateUI();
    }

    void Stand()
    {
        // Ends player turn and triggers dealer's turn
        if (!isPlayerTurn) return;

        if (isSplitHand && GetHandValue(playerHand) <= 21)
        {
            isSplitHand = false;
            UpdateUI();
            return;
        }

        isPlayerTurn = false;
        PlayDealerTurn();
    }

    void Split()
    {
        // Creates two hands from a pair, doubles the bet
        if (!canSplit || currentBet * 2 > playerBalance + currentBet) return;

        splitHand = new List<Card> { playerHand[1] };
        playerHand.RemoveAt(1);
        
        playerBalance -= currentBet;
        currentBet *= 2;

        playerHand.Add(DrawCard());
        splitHand.Add(DrawCard());

        isSplitHand = true;
        canSplit = false;
        splitButton.interactable = false;
        
        UpdateUI();
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
        // Compares hand values and updates balance/UI based on who won
        // Handles both main hand and split hand if it exists
        int dealerValue = GetHandValue(dealerHand);
        int playerValue = GetHandValue(playerHand);
        int splitValue = splitHand != null ? GetHandValue(splitHand) : 0;

        // Process main hand
        if (playerValue <= 21)
        {
            if (dealerValue > 21 || playerValue > dealerValue)
            {
                playerBalance += currentBet * 2;
                resultText.text = "Win!";
            }
            else if (playerValue == dealerValue)
            {
                playerBalance += currentBet;
                resultText.text = "Push";
            }
            else
            {
                resultText.text = "Lose";
            }
        }

        // Process split hand if it exists
        if (splitHand != null && splitValue <= 21)
        {
            if (dealerValue > 21 || splitValue > dealerValue)
            {
                playerBalance += currentBet * 2;
                resultText.text += " Split: Win!";
            }
            else if (splitValue == dealerValue)
            {
                playerBalance += currentBet;
                resultText.text += " Split: Push";
            }
            else
            {
                resultText.text += " Split: Lose";
            }
        }

        currentBet = 0;
    }

    // Special handling for when player gets Blackjack (21 on initial deal)
    void ProcessBlackjack()
    {
        // Pays out at 3:2 odds (2.5x bet)
        playerBalance += (int)(currentBet * 2.5f);
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
        splitButton.interactable = interactable && canSplit;
    }

    // Updates all UI elements with current game state
    void UpdateUI()
    {
        // Updates text displays for hands, balance, bet, etc.
        // Hides dealer's hole card during player's turn
        balanceText.text = $"Balance: ${playerBalance}";
        betText.text = $"Current Bet: ${currentBet}";
        
        playerHandText.text = "Player Hand: " + string.Join(", ", playerHand) + 
            $" (Value: {GetHandValue(playerHand)})";
        
        if (splitHand != null)
        {
            playerHandText.text += "\nSplit Hand: " + string.Join(", ", splitHand) + 
                $" (Value: {GetHandValue(splitHand)})";
        }

        dealerHandText.text = "Dealer Hand: " + 
            (isPlayerTurn ? dealerHand[0].ToString() + ", ?" : string.Join(", ", dealerHand)) +
            (isPlayerTurn ? "" : $" (Value: {GetHandValue(dealerHand)})");
    }

    void ResetGame()
    {
        playerHand = new List<Card>();
        dealerHand = new List<Card>();
        splitHand = null;
        resultText.text = "";
        isPlayerTurn = false;
        isSplitHand = false;
        canSplit = false;
        UpdateUI();
    }
}