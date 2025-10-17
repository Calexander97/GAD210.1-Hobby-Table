using System;
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

    // Broadcast when a SNIP selection changes so UI can highlight the chosen button
    public event Action<SlotType, PartSO> OnSnipSelectionChanged;

    // Current SNIP selections
    Dictionary<SlotType, PartSO> chosen = new() {
        { SlotType.Head, null }, { SlotType.Body, null }, { SlotType.Weapon, null }
    };
    Dictionary<SlotType, bool> painted = new() {
        { SlotType.Head, false }, { SlotType.Body, false }, { SlotType.Weapon, false }
    };

    public Phase phase { get; private set; } = Phase.Snip;

    void Start() { Enter(Phase.Snip); }

    public void ResetToSnip()
    {
        chosen[SlotType.Head] = null;
        chosen[SlotType.Body] = null;
        chosen[SlotType.Weapon] = null;
        painted[SlotType.Head] = painted[SlotType.Body] = painted[SlotType.Weapon] = false;
        Enter(Phase.Snip);
        UpdateButtons();
        // Notify UI to clear highlights
        OnSnipSelectionChanged?.Invoke(SlotType.Head, null);
        OnSnipSelectionChanged?.Invoke(SlotType.Body, null);
        OnSnipSelectionChanged?.Invoke(SlotType.Weapon, null);
    }

    // -------- UI hooks ----------
    public void OnPartClicked(PartSO part)
    {
        if (phase == Phase.Snip)
        {
            chosen[part.slot] = part;
            Log($"Snipped {part.slot}: {(!string.IsNullOrEmpty(part.displayName) ? part.displayName : part.name)}");
            OnSnipSelectionChanged?.Invoke(part.slot, part);
            UpdateButtons();
        }
        else if (phase == Phase.Glue)
        {
            desk.EquipPart(part);
            Log($"Glued {part.slot}: {(!string.IsNullOrEmpty(part.displayName) ? part.displayName : part.name)}");
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
        if (snipPanel) snipPanel.SetActive(p == Phase.Snip);
        if (gluePanel) gluePanel.SetActive(p == Phase.Glue);
        if (paintPanel) paintPanel.SetActive(p == Phase.Paint);
        if (phaseText) phaseText.text = $"Phase: {p}";
        if (p == Phase.Snip) Log("Choose one Head, Body, and Weapon.");
        if (p == Phase.Glue) Log("Assemble/glue your chosen parts (swap if needed).");
        if (p == Phase.Paint) Log("Pick a slot and apply paint.");
        if (saveBtn) saveBtn.gameObject.SetActive(p == Phase.Complete);
        UpdateButtons();
    }

    void UpdateButtons()
    {
        bool canNext = false;
        if (phase == Phase.Snip)
        {
            canNext = (chosen[SlotType.Head] && chosen[SlotType.Body] && chosen[SlotType.Weapon]);
        }
        else if (phase == Phase.Glue)
        {
            canNext = desk.HasAllPartsEquipped();
        }
        else if (phase == Phase.Paint)
        {
            canNext = painted[SlotType.Head] || painted[SlotType.Body] || painted[SlotType.Weapon];
        }
        if (nextBtn) nextBtn.interactable = canNext;
        if (backBtn) backBtn.interactable = (phase != Phase.Snip);
        if (phase == Phase.Complete)
        {
            if (phaseText) phaseText.text = "Phase: Complete";
            if (nextBtn) nextBtn.interactable = false;
            if (backBtn) backBtn.interactable = true;
        }
    }

    void Log(string msg)
    {
        if (!logText) return;
        logText.text += (logText.text.Length > 0 ? "\n" : "") + "• " + msg;
    }

    // Expose current SNIP choice (handy for UI)
    public PartSO GetChosen(SlotType slot) => chosen[slot];
}
