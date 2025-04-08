using UnityEngine;

public class PlayerBalanceManager : MonoBehaviour, IBalanceManager
{
    [SerializeField] private int currentBalance = 10000;
    
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
            return true;
        }
        return false;
    }
    
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        
        currentBalance += amount;
    }

}