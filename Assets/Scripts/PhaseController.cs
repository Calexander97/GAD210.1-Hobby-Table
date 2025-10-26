using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhaseController : MonoBehaviour
{
    public enum Phase { Snip, Glue, Paint, Complete }

    [Header("Core")]
    public HobbyDeskController desk;
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

    public KitSO activeKit { get; private set; }
    public Phase phase { get; private set; } = Phase.Snip;

    public PaintCameraOrbit paintCamOrbit;   // assign in Inspector
    public Transform paintOrbitPivot;        // empty transform used by PaintCameraOrbit

    // ----- SNIP data (counts per option) -----
    [Serializable]
    public class SnipEntry { public PartSO part; public int remaining; public SnipEntry(PartSO p) { part = p; remaining = 1; } }

    readonly List<SnipEntry> snipHeads = new();
    readonly List<SnipEntry> snipBodies = new();
    readonly List<SnipEntry> snipWeapons = new();

    int targetCount = 0;         // 10 for Spearmen, 3 for Knights (from KitSO.unitCount)
    int assembledCount = 0;

    // GLUE indices into the snip lists
    int headIndex = 0, bodyIndex = 0, weaponIndex = 0;

    // Paint phase
    bool paintedAny = false;

    void Start() { Enter(Phase.Snip); UpdateButtons(); }

    public void SetActiveKit(KitSO kit)
    {
        activeKit = kit;
        targetCount = Mathf.Max(1, kit ? kit.unitCount : 1);
        ResetToSnip();
        Log($"Kit set: {kit?.kitName} (need {targetCount} of each).");
    }

    public void ResetToSnip()
    {
        snipHeads.Clear(); snipBodies.Clear(); snipWeapons.Clear();
        assembledCount = 0; headIndex = bodyIndex = weaponIndex = 0;
        paintedAny = false;
        Enter(Phase.Snip);
        UpdateNeedUI();
        UpdateButtons();
    }

    // ---------- SNIP ----------
    public void OnPartClicked(PartSO part)
    {
        if (phase != Phase.Snip && phase != Phase.Glue) return;

        if (phase == Phase.Snip)
        {
            var list = GetList(part.slot);
            int total = TotalCount(list);
            if (total >= targetCount) { Log($"Already snipped {targetCount} {part.slot}(s)."); return; }

            var e = list.FirstOrDefault(x => x.part == part);
            if (e != null) e.remaining++;
            else list.Add(new SnipEntry(part));

            UpdateNeedUI();
            UpdateButtons();
            return;
        }

        if (phase == Phase.Glue)
        {
            // optional: preview on desk’s preview anchors (not required)
            desk.EquipPart(part);
        }
    }

    // ---------- NAV ----------
    public void Next()
    {
        if (phase == Phase.Snip) Enter(Phase.Glue);
        else if (phase == Phase.Glue) Enter(Phase.Paint);
        else if (phase == Phase.Paint)
        {
            // In paint phase, NEXT steps through units; when done -> Complete
            if (desk.units.Count == 0) { Enter(Phase.Complete); return; }
            int i = desk.paintIndex + 1;
            if (i < desk.units.Count) desk.SelectPaintIndex(i);
            else Enter(Phase.Complete);
            UpdateButtons();
        }
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

        if (p == Phase.Paint)
        {
            // first unit
            desk.FocusPaintUnit(0, paintOrbitPivot);
            if (paintCamOrbit) paintCamOrbit.pivot = paintOrbitPivot;
        }
        UpdateButtons();
    }

    // Hook these to your Paint UI "Prev"/"Next" unit buttons:
    public void PaintPrevUnit()
    {
        int idx = desk.NextPaintIndex(-1);
        desk.FocusPaintUnit(idx, paintOrbitPivot);
    }
    public void PaintNextUnit()
    {
        int idx = desk.NextPaintIndex(+1);
        desk.FocusPaintUnit(idx, paintOrbitPivot);
    }

    void UpdateButtons()
    {
        bool canNext = false;

        if (phase == Phase.Snip)
        {
            canNext =
                TotalCount(snipHeads) >= targetCount &&
                TotalCount(snipBodies) >= targetCount &&
                TotalCount(snipWeapons) >= targetCount;
        }
        else if (phase == Phase.Glue)
        {
            canNext = (assembledCount >= targetCount);
        }
        else if (phase == Phase.Paint)
        {
            // Let NEXT always advance through units; Finish goes to Complete
            canNext = true;
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

    // ---------- SNIP helpers ----------
    List<SnipEntry> GetList(SlotType slot) => slot switch
    {
        SlotType.Head => snipHeads,
        SlotType.Body => snipBodies,
        SlotType.Weapon => snipWeapons,
        _ => snipBodies
    };
    static int TotalCount(List<SnipEntry> list) => list.Sum(e => e.remaining);

    void UpdateNeedUI()
    {
        if (headNeedText) headNeedText.text = $"Head  {TotalCount(snipHeads)}/{targetCount}";
        if (bodyNeedText) bodyNeedText.text = $"Body  {TotalCount(snipBodies)}/{targetCount}";
        if (weaponNeedText) weaponNeedText.text = $"Weapon {TotalCount(snipWeapons)}/{targetCount}";
    }

    // ---------- GLUE ----------
    void ClampGlueIndices()
    {
        if (snipHeads.Count > 0) headIndex = Mathf.Clamp(headIndex, 0, snipHeads.Count - 1);
        if (snipBodies.Count > 0) bodyIndex = Mathf.Clamp(bodyIndex, 0, snipBodies.Count - 1);
        if (snipWeapons.Count > 0) weaponIndex = Mathf.Clamp(weaponIndex, 0, snipWeapons.Count - 1);
    }

    public void CycleHead(int dir) { Cycle(ref headIndex, snipHeads, dir); UpdateGlueLabels(); }
    public void CycleBody(int dir) { Cycle(ref bodyIndex, snipBodies, dir); UpdateGlueLabels(); }
    public void CycleWeapon(int dir) { Cycle(ref weaponIndex, snipWeapons, dir); UpdateGlueLabels(); }


    void Cycle(ref int idx, List<SnipEntry> list, int dir)
    {
        if (list == null || list.Count == 0) return;

        // normalize to +1 or -1 so any non-zero value works
        int step = dir >= 0 ? 1 : -1;

        int tries = list.Count; // avoid infinite loop if all remaining == 0
        do
        {
            idx = (idx + step + list.Count) % list.Count;
        }
        while (list[idx].remaining <= 0 && tries-- > 0);
    }


    void UpdateGlueLabels()
    {
        if (currentHeadOption) currentHeadOption.text = snipHeads.Count > 0 ? Nice(snipHeads[headIndex]) : "-";
        if (currentBodyOption) currentBodyOption.text = snipBodies.Count > 0 ? Nice(snipBodies[bodyIndex]) : "-";
        if (currentWeaponOption) currentWeaponOption.text = snipWeapons.Count > 0 ? Nice(snipWeapons[weaponIndex]) : "-";
    }
    string Nice(SnipEntry e) => $"{(string.IsNullOrEmpty(e.part.displayName) ? e.part.name : e.part.displayName)}  x{e.remaining}";

    public void OnAssembleClicked()
    {
        if (phase != Phase.Glue) return;
        if (snipHeads.Count == 0 || snipBodies.Count == 0 || snipWeapons.Count == 0) return;
        var h = snipHeads[headIndex];
        var b = snipBodies[bodyIndex];
        var w = snipWeapons[weaponIndex];
        if (h.remaining <= 0 || b.remaining <= 0 || w.remaining <= 0) { Log("One of the selected parts is out of stock."); return; }

        var slot = desk.GetFreeSlot();
        if (!slot) { Log("No free slot on the desk."); return; }

        var unit = desk.CreateEmptyUnitAt(slot);
        desk.BuildUnit(unit, h.part, b.part, w.part);

        h.remaining--; b.remaining--; w.remaining--;
        assembledCount = Mathf.Clamp(assembledCount + 1, 0, targetCount);

        UpdateGlueLabels();
        UpdateAssembleUI();
        UpdateButtons();
    }

    void UpdateAssembleUI()
    {
        if (assembleProgressText) assembleProgressText.text = $"Assembled: {assembledCount}/{targetCount}";
    }

    // ---------- PAINT ----------
    public void OnColorPicked(Color c)
    {
        if (phase != Phase.Paint) return;
        desk.SetColor(c);
        paintedAny = true;
    }

    // ---------- misc ----------
    void Log(string msg)
    {
        if (!logText) return;
        logText.text += (logText.text.Length > 0 ? "\n" : "") + "• " + msg;
    }
}
