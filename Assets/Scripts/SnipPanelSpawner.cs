using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SnipPanelSpawner : MonoBehaviour
{
    [Header("Data")]
    public KitSO activeKit;
    public PhaseController phase;

    [Header("UI Parents (rows)")]
    public Transform headRow;
    public Transform bodyRow;
    public Transform weaponRow;

    [Header("Prefabs")]
    public GameObject partButtonPrefab;   // must have PartButtonPhase + Text/Icon components

    // Keep track so we can update highlight states
    Dictionary<SlotType, List<PartButtonPhase>> buttonsBySlot = new(){
        {SlotType.Head, new List<PartButtonPhase>()},
        {SlotType.Body, new List<PartButtonPhase>()},
        {SlotType.Weapon, new List<PartButtonPhase>()}
    };

    void OnEnable()
    {
        if (phase) phase.OnSnipSelectionChanged += HandleSelectionChanged;
    }
    void OnDisable()
    {
        if (phase) phase.OnSnipSelectionChanged -= HandleSelectionChanged;
    }

    public void RefreshFromKit(KitSO kit)
    {
        activeKit = kit;
        ClearAll();
        SpawnRow(kit.heads, headRow, SlotType.Head);
        SpawnRow(kit.bodies, bodyRow, SlotType.Body);
        SpawnRow(kit.weapons, weaponRow, SlotType.Weapon);
        // Reset phase so Next is gated correctly
        if (phase) phase.ResetToSnip();
    }

    void SpawnRow(List<PartSO> parts, Transform row, SlotType slot)
    {
        var list = buttonsBySlot[slot];
        list.Clear();

        // cap at 3 to match your layout
        int count = Mathf.Min(parts.Count, 3);
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(partButtonPrefab, row);
            var pb = go.GetComponent<PartButtonPhase>();
            pb.Init(parts[i], phase, slot);
            list.Add(pb);
        }

        // If your row already has 3 placeholders, you can hide extra children or leave gaps.
    }

    void HandleSelectionChanged(SlotType slot, PartSO selected)
    {
        var list = buttonsBySlot[slot];
        foreach (var pb in list)
        {
            pb.SetSelected(pb.part == selected);
        }
    }

    void ClearAll()
    {
        // Destroys all children in the rows (safe if these rows are dedicated)
        foreach (Transform c in headRow) Destroy(c.gameObject);
        foreach (Transform c in bodyRow) Destroy(c.gameObject);
        foreach (Transform c in weaponRow) Destroy(c.gameObject);
        buttonsBySlot[SlotType.Head].Clear();
        buttonsBySlot[SlotType.Body].Clear();
        buttonsBySlot[SlotType.Weapon].Clear();
    }
}
