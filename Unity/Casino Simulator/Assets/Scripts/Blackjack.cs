using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Blackjack : MonoBehaviour
{
    public Text playerHandText;
    public Text dealerHandText;
    public Text resultText;
    public Button hitButton;
    public Button standButton;
    public Button dealButton;
    public InputField betAmountInput;

    private List<int> playerHand;
    private List<int> dealerHand;
    private int betAmount;
    private bool isPlayerTurn;

    void Start()
    {
        hitButton.onClick.AddListener(Hit);
        standButton.onClick.AddListener(Stand);
        dealButton.onClick.AddListener(Deal);
        ResetGame();
    }

    void Update()
    {
        // Update bet amount from UI
        int.TryParse(betAmountInput.text, out betAmount);
    }

    private void Deal()
    {
        ResetGame();
        playerHand.Add(DrawCard());
        playerHand.Add(DrawCard());
        dealerHand.Add(DrawCard());
        dealerHand.Add(DrawCard());

        UpdateHandText();
        isPlayerTurn = true;
        resultText.text = "Your turn!";
    }

    private void Hit()
    {
        if (isPlayerTurn)
        {
            playerHand.Add(DrawCard());
            UpdateHandText();

            if (GetHandValue(playerHand) > 21)
            {
                resultText.text = "Bust! You lose!";
                isPlayerTurn = false;
            }
        }
    }

    private void Stand()
    {
        if (isPlayerTurn)
        {
            isPlayerTurn = false;
            DealerTurn();
        }
    }

    private void DealerTurn()
    {
        while (GetHandValue(dealerHand) < 17)
        {
            dealerHand.Add(DrawCard());
        }

        UpdateHandText();
        DetermineWinner();
    }

    private void DetermineWinner()
    {
        int playerValue = GetHandValue(playerHand);
        int dealerValue = GetHandValue(dealerHand);

        if (dealerValue > 21 || playerValue > dealerValue)
        {
            resultText.text = "You win!";
        }
        else if (playerValue == dealerValue)
        {
            resultText.text = "Push!";
        }
        else
        {
            resultText.text = "You lose!";
        }
    }

    private int DrawCard()
    {
        return Random.Range(1, 12); // Simplified card draw (1-11)
    }

    private int GetHandValue(List<int> hand)
    {
        int value = 0;
        int aceCount = 0;

        foreach (int card in hand)
        {
            if (card == 1)
            {
                aceCount++;
                value += 11;
            }
            else if (card > 10)
            {
                value += 10;
            }
            else
            {
                value += card;
            }
        }

        while (value > 21 && aceCount > 0)
        {
            value -= 10;
            aceCount--;
        }

        return value;
    }

    private void UpdateHandText()
    {
        playerHandText.text = "Player Hand: " + string.Join(", ", playerHand) + " (Value: " + GetHandValue(playerHand) + ")";
        dealerHandText.text = "Dealer Hand: " + string.Join(", ", dealerHand) + " (Value: " + GetHandValue(dealerHand) + ")";
    }

    private void ResetGame()
    {
        playerHand = new List<int>();
        dealerHand = new List<int>();
        resultText.text = "";
        UpdateHandText();
    }
}