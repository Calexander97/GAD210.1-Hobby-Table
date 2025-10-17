using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartButtonPhase : MonoBehaviour
{
    [HideInInspector] public PartSO part;

    [Header("UI")]
    public TMP_Text label;
    public Image icon;
    public Image highlight;       // small image or outline (optional)

    PhaseController phase;
    SlotType slot;

    public void Init(PartSO p, PhaseController phaseCtrl, SlotType s)
    {
        part = p;
        phase = phaseCtrl;
        slot = s;

        if (label) label.text = string.IsNullOrEmpty(p.displayName) ? p.name : p.displayName;
        if (icon)
        {
            icon.sprite = p.icon;
            icon.enabled = (p.icon != null);
        }

        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => phase.OnPartClicked(part));

        SetSelected(false);
    }

    public void SetSelected(bool on)
    {
        if (highlight) highlight.enabled = on;

        // Optional: change Button ColorBlock
        var b = GetComponent<Button>();
        var colors = b.colors;
        colors.normalColor = on ? new Color(0.85f, 0.9f, 1f) : Color.white;
        colors.highlightedColor = on ? new Color(0.9f, 0.95f, 1f) : new Color(0.95f, 0.95f, 0.95f);
        b.colors = colors;
    }
}
