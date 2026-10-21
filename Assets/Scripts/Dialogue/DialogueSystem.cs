using System;
using UnityEngine;

// Chạy hội thoại: hiện câu thoại, nhận lựa chọn, kết thúc
public class DialogueSystem : MonoBehaviour
{
    public bool IsRunning { get; private set; }
    public DialogueData CurrentDialogue { get; private set; }
    public int CurrentLineIndex { get; private set; }
    public DialogueLine CurrentLine =>
        CurrentDialogue != null && CurrentLineIndex >= 0 && CurrentLineIndex < CurrentDialogue.lines.Count
            ? CurrentDialogue.lines[CurrentLineIndex]
            : null;

    public event Action<DialogueLine> LineShown;
    public event Action Ended;

    public void Begin(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Count == 0)
            return;

        CurrentDialogue = dialogue;
        CurrentLineIndex = 0;
        IsRunning = true;

        DialogueView view = FindFirstObjectByType<DialogueView>();
        if (view != null)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.Open(view);
            else
                view.Show();
            view.ShowLine(CurrentLine);
        }

        LineShown?.Invoke(CurrentLine);
    }

    public void Choose(int choiceIndex)
    {
        if (!IsRunning || CurrentLine == null) return;

        int next = -1;
        if (CurrentLine.choices != null && choiceIndex >= 0 && choiceIndex < CurrentLine.choices.Count)
            next = CurrentLine.choices[choiceIndex].nextLine;
        else if (CurrentLine.choices == null || CurrentLine.choices.Count == 0)
            next = CurrentLineIndex + 1;

        if (next >= 0 && CurrentDialogue != null && next < CurrentDialogue.lines.Count)
        {
            CurrentLineIndex = next;
            AudioManager.Play("npc_agree");
            FindFirstObjectByType<DialogueView>()?.ShowLine(CurrentLine);
            LineShown?.Invoke(CurrentLine);
        }
        else
        {
            AudioManager.Play("npc_joy");
            End();
        }
    }

    public void End()
    {
        if (!IsRunning) return;
        IsRunning = false;
        CurrentDialogue = null;
        CurrentLineIndex = -1;

        DialogueView view = FindFirstObjectByType<DialogueView>();
        if (view != null && view.IsOpen)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.Close(view);
            else
                view.Hide();
        }

        Ended?.Invoke();
    }
}
