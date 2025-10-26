using System.Collections.Generic;
using UnityEngine;

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
    public GameObject partButtonPrefab;   // must contain Button + PartButtonPhase

    readonly Dictionary<SlotType, List<PartButtonPhase>> buttonsBySlot = new()
    {
        { SlotType.Head,   new List<PartButtonPhase>() },
        { SlotType.Body,   new List<PartButtonPhase>() },
        { SlotType.Weapon, new List<PartButtonPhase>() }
    };

    void Start()
    {
        if (activeKit) RefreshFromKit(activeKit);
    }

    public void RefreshFromKit(KitSO kit)
    {
        if (!kit || !phase || !partButtonPrefab || !headRow || !bodyRow || !weaponRow)
        {
            Debug.LogError("[SnipPanelSpawner] Missing refs"); return;
        }

        activeKit = kit;
        phase.SetActiveKit(kit);   // sets unitCount target & resets SNIP

        ClearAll();
        SpawnRow(kit.heads, headRow, SlotType.Head);
        SpawnRow(kit.bodies, bodyRow, SlotType.Body);
        SpawnRow(kit.weapons, weaponRow, SlotType.Weapon);
    }

    void SpawnRow(List<PartSO> parts, Transform row, SlotType slot)
    {
        buttonsBySlot[slot].Clear();
        if (parts == null) return;

        int count = Mathf.Min(parts.Count, 3);   // your 3-wide layout
        for (int i = 0; i < count; i++)
        {
            var p = parts[i];
            if (!p) continue;

            var go = Instantiate(partButtonPrefab, row, false);
            var pb = go.GetComponent<PartButtonPhase>();
            if (!pb) { Debug.LogError("partButtonPrefab missing PartButtonPhase"); Destroy(go); continue; }

            // NEW API: 3 arguments only
            pb.Init(p, phase, slot);
            buttonsBySlot[slot].Add(pb);
        }
    }

    void ClearAll()
    {
        void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
        }
        Clear(headRow); Clear(bodyRow); Clear(weaponRow);
        buttonsBySlot[SlotType.Head].Clear();
        buttonsBySlot[SlotType.Body].Clear();
        buttonsBySlot[SlotType.Weapon].Clear();
    }
}
