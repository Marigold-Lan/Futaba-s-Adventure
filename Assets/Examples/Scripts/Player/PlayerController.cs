using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public void AddHealth(Player player)
    {
        AddHealth(player, 1);
    }


    public void AddHealth(Player player, int amount)
    {
        player.health.Increase(amount);
    }
}