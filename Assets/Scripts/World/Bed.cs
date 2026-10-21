using UnityEngine;

// Giường: hiệu ứng lia camera nằm xuống gối, làm tối màn hình, tua tới sáng hôm sau rồi ngồi dậy
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

    // Thực hiện đi ngủ: lia camera nằm xuống giường, làm tối màn hình, tua đồng hồ tới sáng rồi ngồi dậy
    public void Interact(PlayerInteractor interactor)
    {
        if (isSleeping) return;
        AudioManager.PlayAt("sleep_bed", transform.position);

        if (fader == null)
            fader = FindFirstObjectByType<ScreenFader>();

        PlayerInputReader input = interactor != null
            ? interactor.GetComponent<PlayerInputReader>()
            : FindFirstObjectByType<PlayerInputReader>();
        PlayerLook look = interactor != null
            ? interactor.GetComponent<PlayerLook>()
            : FindFirstObjectByType<PlayerLook>();
        HudView hud = FindFirstObjectByType<HudView>();

        Vector3 pillowWorldPos = transform.TransformPoint(new Vector3(0f, 0.72f, -0.35f));
        Quaternion lieWorldRot = Quaternion.Euler(-45f, transform.eulerAngles.y - 90f, -8f);

        if (fader != null && look != null)
        {
            isSleeping = true;
            if (input != null) input.Freeze();

            look.PlayPoseTransition(
                pillowWorldPos,
                lieWorldRot,
                0.55f,
                0.45f,
                0.55f,
                () =>
                {
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
                });
        }
        else if (fader != null)
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
