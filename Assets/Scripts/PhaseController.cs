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

    [Header("Glue UI")]
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
    public Phase phase { get; private set; } = Phase.Snip;
    int targetCount = 0;

    // ---- Bucketed SNIP data (part + remaining count) ----
    [Serializable]
    public class SnipEntry
    {
        public PartSO part;
        public int remaining;
        public SnipEntry(PartSO p, int count = 0) { part = p; remaining = count; }
    }

    readonly List<SnipEntry> _heads = new();
    readonly List<SnipEntry> _bodies = new();
    readonly List<SnipEntry> _weapons = new();

    // GLUE state
    int headIndex = 0, bodyIndex = 0, weaponIndex = 0;
    int assembledCount = 0;

    // PAINT gate
    bool paintedAny = false;

    // Optional highlight event (unchanged)
    public event Action<SlotType, PartSO> OnSnipSelectionChanged;

    void Start()
    {
        Enter(Phase.Snip);
        UpdateButtons();
    }

    // ---------------- Kit / Phase control ----------------

    // Called by KitSelector / SnipPanelSpawner
    public void SetActiveKit(KitSO kit)
    {
        activeKit = kit;
        targetCount = Mathf.Max(1, kit ? kit.unitCount : 1);

        ResetToSnip();
        assembledCount = 0;
        headIndex = bodyIndex = weaponIndex = 0;

        Log($"Kit set: {kit?.kitName} (need {targetCount} of each).");
        UpdateAssembleUI();
    }

    public void ResetToSnip()
    {
        _heads.Clear(); _bodies.Clear(); _weapons.Clear();
        paintedAny = false;
        Enter(Phase.Snip);
        UpdateNeedUI();
        UpdateButtons();

        // clear any button highlights
        OnSnipSelectionChanged?.Invoke(SlotType.Head, null);
        OnSnipSelectionChanged?.Invoke(SlotType.Body, null);
        OnSnipSelectionChanged?.Invoke(SlotType.Weapon, null);
    }

    void Enter(Phase p)
    {
        phase = p;

        if (snipPanel) snipPanel.SetActive(p == Phase.Snip);
        if (gluePanel) gluePanel.SetActive(p == Phase.Glue);
        if (paintPanel) paintPanel.SetActive(p == Phase.Paint);
        if (phaseText) phaseText.text = $"Phase: {p}";

        if (p == Phase.Glue)
        {
            // clamp indices to lists
            headIndex = Mathf.Clamp(headIndex, 0, Mathf.Max(0, _heads.Count - 1));
            bodyIndex = Mathf.Clamp(bodyIndex, 0, Mathf.Max(0, _bodies.Count - 1));
            weaponIndex = Mathf.Clamp(weaponIndex, 0, Mathf.Max(0, _weapons.Count - 1));
            UpdateGlueLabels();
            UpdateAssembleUI();
        }

        UpdateButtons();
    }

    // ---------------- SNIP / GLUE interactions ----------------

    // SNIP click: add 1 to the chosen part’s bucket (capped by target)
    public void OnPartClicked(PartSO part)
    {
        if (phase != Phase.Snip && phase != Phase.Glue) return;

        if (phase == Phase.Snip)
        {
            var list = GetBuckets(part.slot);
            if (TotalPicked(list) >= targetCount) { Log($"Already have {targetCount} {part.slot}s."); return; }

            var entry = EnsureBucket(list, part);
            entry.remaining += 1;

            desk?.EquipPart(part);                // quick preview
            UpdateNeedUI();
            UpdateButtons();
            OnSnipSelectionChanged?.Invoke(part.slot, part);
            return;
        }

        if (phase == Phase.Glue)
        {
            // optional: preview swap
            desk?.EquipPart(part);
            UpdateButtons();
        }
    }

    // Cycle left/right through available entries (skips x0 where possible)
    public void CycleHead(int dir) => CycleBucket(_heads, ref headIndex, dir);
    public void CycleBody(int dir) => CycleBucket(_bodies, ref bodyIndex, dir);
    public void CycleWeapon(int dir) => CycleBucket(_weapons, ref weaponIndex, dir);

    void CycleBucket(List<SnipEntry> list, ref int idx, int dir)
    {
        if (list.Count == 0) return;
        int start = idx;
        for (int i = 0; i < list.Count; i++)
        {
            idx = (idx + dir + list.Count) % list.Count;
            if (list[idx].remaining > 0) break; // land on something available
        }
        UpdateGlueLabels();
    }

    // Assemble one full unit if all three selected entries have stock
    public void OnAssembleClicked()
    {
        if (phase != Phase.Glue || activeKit == null) return;
        if (_heads.Count == 0 || _bodies.Count == 0 || _weapons.Count == 0) return;

        var h = _heads[Mathf.Clamp(headIndex, 0, _heads.Count - 1)];
        var b = _bodies[Mathf.Clamp(bodyIndex, 0, _bodies.Count - 1)];
        var w = _weapons[Mathf.Clamp(weaponIndex, 0, _weapons.Count - 1)];

        if (h.remaining <= 0 || b.remaining <= 0 || w.remaining <= 0)
        {
            Log("Not enough parts left for this combo.");
            return;
        }

        // consume 1 of each part
        h.remaining--; b.remaining--; w.remaining--;
        assembledCount = Mathf.Clamp(assembledCount + 1, 0, targetCount);

        // quality-of-life: auto-move off empty entries
        if (h.remaining == 0) CycleHead(+1);
        if (b.remaining == 0) CycleBody(+1);
        if (w.remaining == 0) CycleWeapon(+1);

        UpdateGlueLabels();
        UpdateAssembleUI();
        UpdateButtons();
    }

    // ---------------- PAINT ----------------

    public void OnColorPicked(Color c)
    {
        if (phase != Phase.Paint) return;
        desk?.SetColor(c);
        paintedAny = true;
        UpdateButtons();
    }

    // ---------------- Nav ----------------

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

    // ---------------- UI helpers ----------------

    void UpdateButtons()
    {
        bool canNext = false;

        if (phase == Phase.Snip)
        {
            canNext =
                TotalPicked(_heads) >= targetCount &&
                TotalPicked(_bodies) >= targetCount &&
                TotalPicked(_weapons) >= targetCount;
        }
        else if (phase == Phase.Glue)
        {
            canNext = (assembledCount >= targetCount && targetCount > 0);
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
        if (headNeedText) headNeedText.text = $"Head {TotalPicked(_heads)}/{targetCount}";
        if (bodyNeedText) bodyNeedText.text = $"Body {TotalPicked(_bodies)}/{targetCount}";
        if (weaponNeedText) weaponNeedText.text = $"Weapon {TotalPicked(_weapons)}/{targetCount}";
    }

    void UpdateGlueLabels()
    {
        if (currentHeadOption && _heads.Count > 0)
        {
            var e = _heads[Mathf.Clamp(headIndex, 0, _heads.Count - 1)];
            currentHeadOption.text = $"{NiceName(e.part)} (x{e.remaining})";
            desk?.EquipPart(e.part);
        }
        if (currentBodyOption && _bodies.Count > 0)
        {
            var e = _bodies[Mathf.Clamp(bodyIndex, 0, _bodies.Count - 1)];
            currentBodyOption.text = $"{NiceName(e.part)} (x{e.remaining})";
            desk?.EquipPart(e.part);
        }
        if (currentWeaponOption && _weapons.Count > 0)
        {
            var e = _weapons[Mathf.Clamp(weaponIndex, 0, _weapons.Count - 1)];
            currentWeaponOption.text = $"{NiceName(e.part)} (x{e.remaining})";
            desk?.EquipPart(e.part);
        }
        UpdateAssembleUI();
    }

    void UpdateAssembleUI()
    {
        if (assembleProgressText && activeKit)
            assembleProgressText.text = $"Assembled: {assembledCount}/{targetCount}";
    }

    string NiceName(PartSO p) =>
        string.IsNullOrEmpty(p.displayName) ? p.name : p.displayName;

    void Log(string msg)
    {
        if (!logText) return;
        logText.text += (logText.text.Length > 0 ? "\n" : "") + "• " + msg;
    }

    // ---------------- Buckets utils ----------------

    List<SnipEntry> GetBuckets(SlotType slot) => slot switch
    {
        SlotType.Head => _heads,
        SlotType.Body => _bodies,
        SlotType.Weapon => _weapons,
        _ => _bodies
    };

    SnipEntry EnsureBucket(List<SnipEntry> buckets, PartSO part)
    {
        foreach (var e in buckets) if (e.part == part) return e;
        var ne = new SnipEntry(part, 0);
        buckets.Add(ne);
        return ne;
    }

    int TotalPicked(List<SnipEntry> buckets)
    {
        int t = 0; foreach (var e in buckets) t += Mathf.Max(0, e.remaining); return t;
    }

    // (Optional helpers you were using elsewhere)
    public int GetSnipCount(SlotType slot) => TotalPicked(GetBuckets(slot));
    public int GetTargetCount() => targetCount;
}
