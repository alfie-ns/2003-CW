using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Craps : MonoBehaviour
{
    public Text resultText;
    public Button rollButton;
    public InputField betAmountInput;

    private int betAmount;
    private int point;
    private bool isPointSet;

    void Start()
    {
        rollButton.onClick.AddListener(RollDice);
        isPointSet = false;
    }

    void Update()
    {
        // Update bet amount from UI
        int.TryParse(betAmountInput.text, out betAmount);
    }

    private void RollDice()
    {
        int dice1 = Random.Range(1, 7);
        int dice2 = Random.Range(1, 7);
        int roll = dice1 + dice2;

        resultText.text = "Roll: " + roll + " (" + dice1 + " + " + dice2 + ")";

        if (!isPointSet)
        {
            if (roll == 7 || roll == 11)
            {
                resultText.text += "\nYou win!";
            }
            else if (roll == 2 || roll == 3 || roll == 12)
            {
                resultText.text += "\nYou lose!";
            }
            else
            {
                point = roll;
                isPointSet = true;
                resultText.text += "\nPoint is set to " + point;
            }
        }
        else
        {
            if (roll == point)
            {
                resultText.text += "\nYou win!";
                isPointSet = false;
            }
            else if (roll == 7)
            {
                resultText.text += "\nYou lose!";
                isPointSet = false;
            }
        }
    }
}