using UnityEngine;

public interface IBalanceManager
{
    int GetBalance();
    bool TrySpendMoney(int amount);
    void AddMoney(int amount);
}