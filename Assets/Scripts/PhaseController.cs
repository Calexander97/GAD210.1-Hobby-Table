using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhaseController : MonoBehaviour
{
    public enum Phase { Snip, Glue, Paint, Complete }

    [Header("Refs")]
    public HobbyDeskController desk;
    public GameObject snipPanel, gluePanel, paintPanel;
    public Button backBtn, nextBtn, saveBtn;
    public TMP_Text phaseText, logText;

    Dictionary<SlotType, PartSO> chosen = new() {
        { SlotType.Head, null }, { SlotType.Body, null }, { SlotType.Weapon, null }
    };
    Dictionary<SlotType, bool> painted = new() {
        { SlotType.Head, false }, { SlotType.Body, false }, { SlotType.Weapon, false }
    };

    Phase phase = Phase.Snip;

    void Start() { Enter(Phase.Snip); }

    // --- UI hooks ---
    public void OnPartClicked(PartSO part)
    {
        if (phase == Phase.Snip)
        {
            chosen[part.slot] = part;
            Log($"Snipped {part.slot}: {part.name}");
            UpdateButtons();
        }
        else if (phase == Phase.Glue)
        {
            desk.EquipPart(part);
            Log($"Glued {part.slot}: {part.name}");
            UpdateButtons();
        }
    }
    public void OnColorPicked(Color c)
    {
        if (phase != Phase.Paint) return;
        desk.SetColor(c);
        painted[desk.GetCurrentSlot()] = true;
        UpdateButtons();
    }

    public void Next()
    {
        if (phase == Phase.Snip)
        {
            Enter(Phase.Glue);
            foreach (var kv in chosen) if (kv.Value != null) desk.EquipPart(kv.Value);
        }
        else if (phase == Phase.Glue) Enter(Phase.Paint);
        else if (phase == Phase.Paint) Enter(Phase.Complete);
    }
    public void Back()
    {
        if (phase == Phase.Glue) Enter(Phase.Snip);
        else if (phase == Phase.Paint) Enter(Phase.Glue);
    }

    void Enter(Phase p)
    {
        phase = p;
        snipPanel.SetActive(p == Phase.Snip);
        gluePanel.SetActive(p == Phase.Glue);
        paintPanel.SetActive(p == Phase.Paint);
        if (phaseText) phaseText.text = $"Phase: {p}";
        if (p == Phase.Snip) Log("Choose one Head, Body and Weapon to snip.");
        if (p == Phase.Glue) Log("Glue your chosen parts. You can still swap.");
        if (p == Phase.Paint) Log("Select a slot and apply paint.");
        saveBtn.gameObject.SetActive(p == Phase.Complete);
        UpdateButtons();
    }

    void UpdateButtons()
    {
        bool canNext = false;
        if (phase == Phase.Snip)
        {
            canNext = chosen[SlotType.Head] && chosen[SlotType.Body] && chosen[SlotType.Weapon];
        }
        else if (phase == Phase.Glue)
        {
            canNext = desk.HasAllPartsEquipped();
        }
        else if (phase == Phase.Paint)
        {
            canNext = painted[SlotType.Head] || painted[SlotType.Body] || painted[SlotType.Weapon];
        }
        nextBtn.interactable = canNext;
        backBtn.interactable = (phase != Phase.Snip);
        if (phase == Phase.Complete)
        {
            if (phaseText) phaseText.text = "Phase: Complete";
            nextBtn.interactable = false;
            backBtn.interactable = true;
        }
    }

    void Log(string msg)
    {
        if (!logText) return;
        logText.text += (logText.text.Length > 0 ? "\n" : "") + "• " + msg;
    }
}
