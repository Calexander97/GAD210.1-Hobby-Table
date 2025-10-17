using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SnipPanelSpawner : MonoBehaviour
{
    [Header("Data")]
    public KitSO activeKit;
    public PhaseController phase;

    [Header("UI Parents")]
    public Transform headContainer;
    public Transform bodyContainer;
    public Transform weaponContainer;

    [Header("Prefabs")]
    public GameObject partButtonPrefab; // has PartButtonPhase component

    List<GameObject> spawned = new();

    public void RefreshFromKit(KitSO kit)
    {
        activeKit = kit;
        Clear();

        SpawnGroup(kit.heads, headContainer);
        SpawnGroup(kit.bodies, bodyContainer);
        SpawnGroup(kit.weapons, weaponContainer);
    }

    void SpawnGroup(List<PartSO> parts, Transform parent)
    {
        foreach (var p in parts)
        {
            var go = Instantiate(partButtonPrefab, parent);
            var pb = go.GetComponent<PartButtonPhase>();
            pb.part = p;
            pb.phase = phase;
            spawned.Add(go);
        }
    }

    void Clear()
    {
        foreach (var go in spawned) Destroy(go);
        spawned.Clear();
    }
}
