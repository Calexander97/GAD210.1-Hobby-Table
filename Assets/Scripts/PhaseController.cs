using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhaseController : MonoBehaviour
{
    public enum Phase { Snip, Glue, Paint, Complete }

    [Header("Core")]
    public HobbyDeskController desk;
    public SnipPanelSpawner snipSpawner;
    public GameObject snipPanel, gluePanel, paintPanel;
    public Button backBtn, nextBtn, saveBtn;
    public TMP_Text phaseText, logText;

    [Header("Glue UI (optional)")]
    public TMP_Text currentHeadOption;
    public TMP_Text currentBodyOption;
    public TMP_Text currentWeaponOption;
    public TMP_Text assembleProgressText;
    public Button assembleBtn;

    [Header("SNIP Need UI")]
    public TMP_Text headNeedText;
    public TMP_Text bodyNeedText;
    public TMP_Text weaponNeedText;

    // runtime
    public KitSO activeKit { get; private set; }
    int targetCount = 0;
    public Phase phase { get; private set; } = Phase.Snip;

    // collected parts in SNIP
    readonly List<PartSO> _snipHeads = new();
    readonly List<PartSO> _snipBodies = new();
    readonly List<PartSO> _snipWeapons = new();

    bool paintedAny = false;

    // optional: UI can listen for highlight updates
    public event Action<SlotType, PartSO> OnSnipSelectionChanged;

    void Start()
    {
        Enter(Phase.Snip);
        UpdateButtons();
    }

    // called by KitSelector / SnipPanelSpawner
    public void SetActiveKit(KitSO kit)
    {
        activeKit = kit;
        targetCount = Mathf.Max(1, kit ? kit.unitCount : 1);
        ResetToSnip();                 // ensure lists/labels reset
        Log($"Kit set: {kit?.kitName} (need {targetCount} of each).");
    }

    // PUBLIC so old callers compile
    public void ResetToSnip()
    {
        _snipHeads.Clear();
        _snipBodies.Clear();
        _snipWeapons.Clear();
        paintedAny = false;
        Enter(Phase.Snip);
        UpdateNeedUI();
        UpdateButtons();
        // clear any highlights
        OnSnipSelectionChanged?.Invoke(SlotType.Head, null);
        OnSnipSelectionChanged?.Invoke(SlotType.Body, null);
        OnSnipSelectionChanged?.Invoke(SlotType.Weapon, null);
    }

    // ---- UI hooks ----
    // SNIP: each click ADDS one part up to targetCount
    public void OnPartClicked(PartSO part)
    {
        if (phase != Phase.Snip && phase != Phase.Glue) return;

        if (phase == Phase.Snip)
        {
            var list = GetSnipList(part.slot);
            if (list.Count >= targetCount) { Log($"Already have {targetCount} {part.slot}s."); return; }
            list.Add(part);
            desk?.EquipPart(part); // quick preview
            UpdateNeedUI();
            UpdateButtons();
            OnSnipSelectionChanged?.Invoke(part.slot, part);
            return;
        }

        if (phase == Phase.Glue)
        {
            desk?.EquipPart(part);
            UpdateButtons();
        }
    }

    public void OnColorPicked(Color c)
    {
        if (phase != Phase.Paint) return;
        desk?.SetColor(c);
        paintedAny = true;
        UpdateButtons();
    }

    public void Next()
    {
        if (phase == Phase.Snip) Enter(Phase.Glue);
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
        UpdateButtons();
    }

    void UpdateButtons()
    {
        bool canNext = false;

        if (phase == Phase.Snip)
        {
            canNext =
                _snipHeads.Count >= targetCount &&
                _snipBodies.Count >= targetCount &&
                _snipWeapons.Count >= targetCount;
        }
        else if (phase == Phase.Glue)
        {
            canNext = desk != null && desk.HasAllPartsEquipped();
        }
        else if (phase == Phase.Paint)
        {
            canNext = paintedAny;
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

    void UpdateNeedUI()
    {
        if (!activeKit) return;
        if (headNeedText) headNeedText.text = $"Head {_snipHeads.Count}/{targetCount}";
        if (bodyNeedText) bodyNeedText.text = $"Body {_snipBodies.Count}/{targetCount}";
        if (weaponNeedText) weaponNeedText.text = $"Weapon {_snipWeapons.Count}/{targetCount}";
    }

    List<PartSO> GetSnipList(SlotType slot) => slot switch
    {
        SlotType.Head => _snipHeads,
        SlotType.Body => _snipBodies,
        SlotType.Weapon => _snipWeapons,
        _ => _snipBodies
    };

    void Log(string msg)
    {
        if (!logText) return;
        logText.text += (logText.text.Length > 0 ? "\n" : "") + "• " + msg;
    }

    // read-only accessors (for other systems if needed)
    public int GetSnipCount(SlotType slot) => GetSnipList(slot).Count;
    public int GetTargetCount() => targetCount;
}
