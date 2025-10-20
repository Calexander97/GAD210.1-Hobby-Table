using UnityEngine;
using System.Collections.Generic;

public class SnipPanelSpawner : MonoBehaviour
{
    [Header("Data")]
    public KitSO activeKit;
    public PhaseController phase;

    [Header("UI Parents (rows = the Content objects)")]
    public Transform headRow;
    public Transform bodyRow;
    public Transform weaponRow;

    [Header("Prefabs")]
    public GameObject partButtonPrefab;

    Dictionary<SlotType, List<PartButtonPhase>> buttonsBySlot = new(){
        {SlotType.Head,   new List<PartButtonPhase>()},
        {SlotType.Body,   new List<PartButtonPhase>()},
        {SlotType.Weapon, new List<PartButtonPhase>()}
    };

    void Start()
    {
        // Auto-spawn on scene load if a kit is assigned
        if (activeKit != null) RefreshFromKit(activeKit);
        else Debug.LogWarning("[SnipPanelSpawner] No activeKit set; nothing to spawn.");
    }

    void OnEnable() { if (phase) phase.OnSnipSelectionChanged += HandleSelectionChanged; }
    void OnDisable() { if (phase) phase.OnSnipSelectionChanged -= HandleSelectionChanged; }

    public void RefreshFromKit(KitSO kit)
    {
        if (kit == null) { Debug.LogError("[SnipPanelSpawner] RefreshFromKit NULL kit"); return; }
        if (phase == null) { Debug.LogError("[SnipPanelSpawner] PhaseController is NULL"); }
        if (headRow == null || bodyRow == null || weaponRow == null)
        {
            Debug.LogError("[SnipPanelSpawner] One or more row Transforms are NULL"); return;
        }
        if (partButtonPrefab == null)
        {
            Debug.LogError("[SnipPanelSpawner] partButtonPrefab is NULL"); return;
        }

        activeKit = kit;
        phase?.ResetToSnip();
        ClearAll();

        Debug.Log($"[SnipPanelSpawner] Spawning: H:{kit.heads?.Count} B:{kit.bodies?.Count} W:{kit.weapons?.Count}");

        SpawnRow(kit.heads, headRow, SlotType.Head);
        SpawnRow(kit.bodies, bodyRow, SlotType.Body);
        SpawnRow(kit.weapons, weaponRow, SlotType.Weapon);
    }

    void SpawnRow(List<PartSO> parts, Transform row, SlotType slot)
    {
        buttonsBySlot[slot].Clear();

        if (parts == null) { Debug.LogWarning($"[SnipPanelSpawner] {slot} list is NULL"); return; }

        int count = Mathf.Min(parts.Count, 3);
        for (int i = 0; i < count; i++)
        {
            var p = parts[i];
            if (p == null) { Debug.LogWarning($"[SnipPanelSpawner] NULL PartSO in {slot} @ {i}"); continue; }

            var go = Instantiate(partButtonPrefab);
            go.transform.SetParent(row, false); // keep rect scale/anchors
            var pb = go.GetComponent<PartButtonPhase>();
            if (pb == null) { Debug.LogError("[SnipPanelSpawner] partButtonPrefab missing PartButtonPhase"); Destroy(go); continue; }

            pb.Init(p, phase, slot);
            buttonsBySlot[slot].Add(pb);
        }
    }

    void HandleSelectionChanged(SlotType slot, PartSO selected)
    {
        foreach (var pb in buttonsBySlot[slot]) pb.SetSelected(pb.part == selected);
    }

    void ClearAll()
    {
        ClearRow(headRow); ClearRow(bodyRow); ClearRow(weaponRow);
        buttonsBySlot[SlotType.Head].Clear();
        buttonsBySlot[SlotType.Body].Clear();
        buttonsBySlot[SlotType.Weapon].Clear();
    }
    void ClearRow(Transform row)
    {
        for (int i = row.childCount - 1; i >= 0; i--)
        {
            var child = row.GetChild(i);
            if (child.GetComponent<PartButtonPhase>() != null) Destroy(child.gameObject);
        }
    }
}
