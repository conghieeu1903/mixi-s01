using UnityEngine;
using UnityEngine.UIElements;

// Hộp hội thoại NPC: chân dung, câu thoại, các lựa chọn
public class DialogueView : UIPanel
{
    Label speakerLabel;
    Label textLabel;
    VisualElement choicesBox;
    bool built;

    void OnEnable()
    {
        built = false;
        EnsureFrame();
    }

    void EnsureFrame()
    {
        if (built || Root == null) return;
        built = true;

        VisualElement host = Root.Q<VisualElement>("worldPanels") ?? Root;
        host.Clear();
        host.AddToClassList("wf-screen");
        host.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
        host.style.alignItems = Align.Center;
        host.style.justifyContent = Justify.FlexEnd;
        host.style.paddingBottom = 40;

        VisualElement box = new VisualElement();
        box.AddToClassList("wf-panel");
        box.style.width = 640;
        box.style.paddingLeft = 20;
        box.style.paddingRight = 20;
        box.style.paddingTop = 16;
        box.style.paddingBottom = 16;

        speakerLabel = new Label("NPC");
        speakerLabel.AddToClassList("wf-subheading");
        speakerLabel.style.marginBottom = 6;

        textLabel = new Label("...");
        textLabel.AddToClassList("wf-text");
        textLabel.AddToClassList("wf-wrap");
        textLabel.style.marginBottom = 12;

        choicesBox = new VisualElement();

        box.Add(speakerLabel);
        box.Add(textLabel);
        box.Add(choicesBox);
        host.Add(box);
    }

    public void ShowLine(DialogueLine line)
    {
        EnsureFrame();
        if (line == null) return;

        if (speakerLabel != null) speakerLabel.text = line.speaker;
        if (textLabel != null) textLabel.text = line.text;
        if (choicesBox == null) return;

        choicesBox.Clear();
        DialogueSystem sys = FindFirstObjectByType<DialogueSystem>();

        if (line.choices != null && line.choices.Count > 0)
        {
            for (int i = 0; i < line.choices.Count; i++)
            {
                int idx = i;
                Button btn = new Button(() => sys?.Choose(idx))
                {
                    text = line.choices[i].text
                };
                btn.AddToClassList("wf-button");
                btn.AddToClassList("wf-button--small");
                btn.style.marginBottom = 6;
                choicesBox.Add(btn);
            }
        }
        else
        {
            Button nextBtn = new Button(() => sys?.Choose(0)) { text = "Tiếp tục" };
            nextBtn.AddToClassList("wf-button");
            nextBtn.AddToClassList("wf-button--primary");
            nextBtn.AddToClassList("wf-button--small");
            choicesBox.Add(nextBtn);
        }
    }
}
