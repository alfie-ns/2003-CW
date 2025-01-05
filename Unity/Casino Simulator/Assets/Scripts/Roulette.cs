using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Roulette : MonoBehaviour
{
    public Text resultText;
    public Button spinButton;
    public Dropdown betTypeDropdown;
    public InputField betAmountInput;

    private int betAmount;
    private string betType;

    void Start()
    {
        spinButton.onClick.AddListener(Spin);
    }

    void Update()
    {
        // Update bet amount and type from UI
        int.TryParse(betAmountInput.text, out betAmount);
        betType = betTypeDropdown.options[betTypeDropdown.value].text;
    }

    private void Spin()
    {
        // Simulate spinning the roulette wheel
        int result = Random.Range(0, 37); // 0-36 for European roulette

        // Display the result
        resultText.text = "Result: " + result;

        // Check if the player wins
        bool isWin = CheckWin(result);

        // Display win/lose message
        if (isWin)
        {
            resultText.text += "\nYou win!";
        }
        else
        {
            resultText.text += "\nYou lose!";
        }
    }

    private bool CheckWin(int result)
    {
        switch (betType)
        {
            case "Red":
                return IsRed(result);
            case "Black":
                return IsBlack(result);
            case "Odd":
                return result % 2 != 0;
            case "Even":
                return result % 2 == 0;
            case "1-18":
                return result >= 1 && result <= 18;
            case "19-36":
                return result >= 19 && result <= 36;
            case "Single Number":
                return result == int.Parse(betAmountInput.text);
            default:
                return false;
        }
    }

    private bool IsRed(int number)
    {
        int[] redNumbers = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
        return System.Array.IndexOf(redNumbers, number) >= 0;
    }

    private bool IsBlack(int number)
    {
        int[] blackNumbers = { 2, 4, 6, 8, 10, 11, 13, 15, 17, 20, 22, 24, 26, 28, 29, 31, 33, 35 };
        return System.Array.IndexOf(blackNumbers, number) >= 0;
    }
}