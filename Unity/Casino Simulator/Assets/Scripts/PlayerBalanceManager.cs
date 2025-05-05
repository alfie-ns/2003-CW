using UnityEngine;
using UnityEngine.UI;
using TMPro; // Adding TextMeshPro namespace

public class PlayerBalanceManager : MonoBehaviour, IBalanceManager
{
    [SerializeField] private int currentBalance = 10000;
    [SerializeField] private GameObject balanceTextObject;
    
    // Reference to the text component
    private TextMeshProUGUI balanceText;

    private void Start()
    {
        // Find the text component
        if (balanceTextObject != null)
        {
            balanceText = balanceTextObject.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogError("Balance Text Object not assigned in the inspector");
        }
        
        UpdateBalanceText();
    }

    private void UpdateBalanceText()
    {
        if (balanceText != null)
        {
            balanceText.text = $"${currentBalance}";
        }
        else if (balanceTextObject != null)
        {
            Debug.LogWarning("No text component found to update balance display");
        }
    }
    
    public void SetBalance(int amount)
    {
    currentBalance = amount;
    UpdateBalanceText(); // Update the display after setting the balance
    }
    public int GetBalance()
    {
        return currentBalance;
    }
    
    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0) return false;
        
        if (currentBalance >= amount)
        {
            currentBalance -= amount;
            UpdateBalanceText(); // Add this to update display when spending
            return true;
        }
        return false;
    }
    
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        
        currentBalance += amount;
        UpdateBalanceText(); // Add this to update display when adding money
    }
}