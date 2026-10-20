using System.Collections.Generic;
using UnityEngine;

// Tủ lạnh: chứa đồ ăn, mở ra để lấy đồ ăn
public class Fridge : MonoBehaviour, IInteractable
{
    [SerializeField] List<FoodData> contents = new List<FoodData>();

    public string Prompt => "Mở tủ lạnh";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }

    // Ăn một món trong tủ
    public void Eat(FoodData food)
    {
    }
}
