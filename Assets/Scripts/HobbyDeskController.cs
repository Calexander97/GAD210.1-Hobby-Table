using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HobbyDeskController : MonoBehaviour
{
    [Header("Data")]
    public KitSO kit;

    [Header("Anchors")]
    public Transform headAnchor, bodyAnchor, weaponAnchor;

    [Header("Camera Orbit")]
    public Transform orbitTarget;
    public float orbitSpeed = 120f, zoomSpeed = 5f, minDist = 2f, maxDist = 8f;

    Dictionary<SlotType, GameObject> currentParts = new();
    Dictionary<SlotType, string> currentIds = new();
    Dictionary<SlotType, Color> currentCols = new() {
        {SlotType.Head, Color.gray}, {SlotType.Body, Color.gray}, {SlotType.Weapon, Color.gray}
    };
    SlotType selectedSlot = SlotType.Body;

    Camera cam;
    void Awake() { cam = Camera.main; }

    void Update()
    {
        HandleOrbit();
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            TryPickSlotFromScene();
        }
    }

    void HandleOrbit()
    {
        if (Input.GetMouseButton(1))
        {
            float dx = Input.GetAxis("Mouse X");
            orbitTarget.Rotate(Vector3.up, dx * orbitSpeed * Time.deltaTime, Space.World);
        }
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            var dir = cam.transform.forward;
            var newPos = cam.transform.position + dir * (scroll * zoomSpeed);
            float dist = Vector3.Distance(newPos, orbitTarget.position);
            if (dist > minDist && dist < maxDist) cam.transform.position = newPos;
        }
    }

    void TryPickSlotFromScene()
    {
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 100f))
        {
            var root = hit.collider.transform;
            while (root.parent != null && root.parent != orbitTarget) root = root.parent;
            if (root == headAnchor) selectedSlot = SlotType.Head;
            else if (root == bodyAnchor) selectedSlot = SlotType.Body;
            else if (root == weaponAnchor) selectedSlot = SlotType.Weapon;
        }
    }

    public void SelectSlotHead() { selectedSlot = SlotType.Head; }
    public void SelectSlotBody() { selectedSlot = SlotType.Body; }
    public void SelectSlotWeapon() { selectedSlot = SlotType.Weapon; }

    public void EquipPart(PartSO part)
    {
        var anchor = GetAnchor(part.slot);
        if (currentParts.TryGetValue(part.slot, out var existing)) Destroy(existing);
        var go = Instantiate(part.prefab, anchor);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        currentParts[part.slot] = go;
        currentIds[part.slot] = part.id;
        ApplyColorToObject(go, currentCols[part.slot]);
    }

    public void SetColor(Color c)
    {
        currentCols[selectedSlot] = c;
        if (currentParts.TryGetValue(selectedSlot, out var go)) ApplyColorToObject(go, c);
    }

    void ApplyColorToObject(GameObject go, Color c)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", c);   // Standard/URP Lit default color
            r.SetPropertyBlock(mpb);
        }
    }

    public SlotType GetCurrentSlot() => selectedSlot;
    public bool HasAllPartsEquipped() =>
        currentParts.ContainsKey(SlotType.Head) &&
        currentParts.ContainsKey(SlotType.Body) &&
        currentParts.ContainsKey(SlotType.Weapon);

    Transform GetAnchor(SlotType t) => t switch
    {
        SlotType.Head => headAnchor,
        SlotType.Body => bodyAnchor,
        SlotType.Weapon => weaponAnchor,
        _ => bodyAnchor
    };

    // ----- Save/Load (simple) -----
    [Serializable]
    public class UnitSave
    {
        public string headId, bodyId, weaponId;
        public Color headCol, bodyCol, weaponCol;
    }
    [Serializable] public class SaveList { public List<UnitSave> items = new(); }
    const string SAVE_KEY = "Hobby_Collection";
    SaveList cache;

    public void SaveCurrentToCollection()
    {
        var u = new UnitSave
        {
            headId = currentIds.GetValueOrDefault(SlotType.Head),
            bodyId = currentIds.GetValueOrDefault(SlotType.Body),
            weaponId = currentIds.GetValueOrDefault(SlotType.Weapon),
            headCol = currentCols[SlotType.Head],
            bodyCol = currentCols[SlotType.Body],
            weaponCol = currentCols[SlotType.Weapon]
        };
        var list = LoadAll();
        list.items.Add(u);
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }
    public SaveList LoadAll()
    {
        if (cache != null) return cache;
        cache = PlayerPrefs.HasKey(SAVE_KEY)
            ? JsonUtility.FromJson<SaveList>(PlayerPrefs.GetString(SAVE_KEY))
            : new SaveList();
        return cache;
    }

    public PartSO FindPartById(string id)
    {
        foreach (var p in kit.heads) if (p.id == id) return p;
        foreach (var p in kit.bodies) if (p.id == id) return p;
        foreach (var p in kit.weapons) if (p.id == id) return p;
        return null;
    }
    public void SpawnFromSave(UnitSave u)
    {
        if (!string.IsNullOrEmpty(u.headId)) EquipPart(FindPartById(u.headId));
        if (!string.IsNullOrEmpty(u.bodyId)) EquipPart(FindPartById(u.bodyId));
        if (!string.IsNullOrEmpty(u.weaponId)) EquipPart(FindPartById(u.weaponId));
        // Reapply colors
        currentCols[SlotType.Head] = u.headCol;
        currentCols[SlotType.Body] = u.bodyCol;
        currentCols[SlotType.Weapon] = u.weaponCol;
        SetColor(u.bodyCol);
    }
}
