using System.Collections.Generic;
using UnityEngine;

// Showroom mua xe
public class VehicleDealer : MonoBehaviour, IInteractable
{
    [SerializeField] float defaultVehiclePrice = 150f;
    [SerializeField] VehicleController showroomVehicle;

    public HashSet<string> OwnedVehicles { get; } = new HashSet<string>();

    public string Prompt => $"Showroom Xe (Mua xe $ {defaultVehiclePrice:0.##})";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (BuyVehicle("car_sedan", defaultVehiclePrice))
        {
            AudioManager.PlayAt("store_bell", transform.position);
            AudioManager.Play("purchase");
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã mua xe tại Showroom! Lại gần xe bấm [E] để lái.", true);
        }
        else
        {
            AudioManager.PlayUi("purchase_fail");
            FindFirstObjectByType<HudView>()?.ShowInfo($"Không đủ $ {defaultVehiclePrice:0.##} để mua xe mới", false);
        }
    }

    public bool BuyVehicle(string vehicleId, float price)
    {
        if (string.IsNullOrEmpty(vehicleId)) return false;
        if (OwnedVehicles.Contains(vehicleId)) return true;
        if (!GameWorld.HasInstance || !GameWorld.Instance.Economy.TrySpend(price))
            return false;

        OwnedVehicles.Add(vehicleId);
        if (showroomVehicle != null)
            showroomVehicle.gameObject.SetActive(true);
        return true;
    }
}
