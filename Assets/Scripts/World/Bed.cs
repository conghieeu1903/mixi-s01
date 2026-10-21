using UnityEngine;

// Giường: làm tối màn hình và tua tới sáng hôm sau khi người chơi bấm E đi ngủ
public class Bed : MonoBehaviour, IInteractable
{
    [Tooltip("Giờ thức dậy")]
    [SerializeField] int wakeUpHour = 8;

    [Tooltip("Hiệu ứng tối màn hình khi ngủ")]
    [SerializeField] ScreenFader fader;

    bool isSleeping;

    public string Prompt => "Đi ngủ";

    // Chỉ cho ngủ khi không đang trong quá trình chuyển cảnh ngủ
    public bool CanInteract(PlayerInteractor interactor)
    {
        return !isSleeping;
    }

    // Thực hiện đi ngủ: làm tối màn hình, tua đồng hồ tới sáng rồi mở lại màn hình
    public void Interact(PlayerInteractor interactor)
    {
        if (isSleeping) return;
        AudioManager.PlayAt("sleep_bed", transform.position);

        if (fader == null)
            fader = FindFirstObjectByType<ScreenFader>();

        PlayerInputReader input = interactor != null
            ? interactor.GetComponent<PlayerInputReader>()
            : FindFirstObjectByType<PlayerInputReader>();
        HudView hud = FindFirstObjectByType<HudView>();

        if (fader != null)
        {
            isSleeping = true;
            if (input != null) input.Freeze();

            fader.FadeOut(() =>
            {
                GameWorld.Instance.SleepUntilMorning(wakeUpHour);
                if (hud != null)
                    hud.ShowInfo($"Đã thức dậy — Ngày {GameWorld.Instance.Clock.Day}", true);

                fader.FadeIn(() =>
                {
                    if (input != null) input.Unfreeze();
                    isSleeping = false;
                });
            });
        }
        else
        {
            GameWorld.Instance.SleepUntilMorning(wakeUpHour);
            if (hud != null)
                hud.ShowInfo($"Đã thức dậy — Ngày {GameWorld.Instance.Clock.Day}", true);
        }
    }
}
