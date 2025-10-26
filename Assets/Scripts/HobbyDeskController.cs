using System;
using System.Collections.Generic;
using UnityEngine;

public class HobbyDeskController : MonoBehaviour
{
    [Header("Kit (optional lookup)")]
    public KitSO kit;

    // =============== SLOT GRID ===============
    [Header("Slot Grid")]
    public Transform slotsParent;      // UnitSlots
    public int maxUnits = 10;
    public Transform[] slots;          // auto-filled from slotsParent
    public GameObject unitRootPrefab;  // must contain HeadAnchor/BodyAnchor/WeaponAnchor

    // =============== CAMERA ===============
    [Header("Paint Camera Rig")]
    public Transform orbitPivot;       // moves to the active unit
    public Camera paintCamera;
    public float orbitSpeed = 120f, zoomSpeed = 5f, minDist = 2f, maxDist = 8f;

    // =============== RUNTIME ===============
    [Serializable]
    public class UnitInstance
    {
        public Transform unitRoot;
        public Transform headAnchor, bodyAnchor, weaponAnchor;
        public string headId, bodyId, weaponId;
        public Color headCol = Color.gray, bodyCol = Color.gray, weaponCol = Color.gray;
    }

    public List<UnitInstance> units = new();
    public int paintIndex = 0;

    // which sub-slot we’re painting (for the UI buttons)
    SlotType selectedSlot = SlotType.Body;

    // ---------- Back-compat preview anchors for SNIP/GLUE ----------
    Transform previewRoot, previewHead, previewBody, previewWeapon;

    // ---------- Simple save types (back-compat) ----------
    [Serializable]
    public class UnitSave
    {
        public string headId, bodyId, weaponId;
        public Color headCol, bodyCol, weaponCol;
    }
    [Serializable] public class SaveList { public List<UnitSave> items = new(); }

    Camera cam;

    void Awake()
    {
        cam = paintCamera ? paintCamera : Camera.main;

        if (slotsParent && (slots == null || slots.Length == 0))
        {
            int n = Mathf.Min(maxUnits, slotsParent.childCount);
            slots = new Transform[n];
            for (int i = 0; i < n; i++) slots[i] = slotsParent.GetChild(i);
        }
    }

    void Update() { HandleOrbit(); }

    // --------- ORBIT (camera only; units never rotate) ----------
    void HandleOrbit()
    {
        if (!orbitPivot || !cam) return;

        if (Input.GetMouseButton(1))
        {
            float dx = Input.GetAxis("Mouse X");
            float dy = Input.GetAxis("Mouse Y");
            cam.transform.RotateAround(orbitPivot.position, Vector3.up, dx * orbitSpeed * Time.deltaTime);
            cam.transform.RotateAround(orbitPivot.position, cam.transform.right, -dy * orbitSpeed * Time.deltaTime);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            Vector3 dir = (cam.transform.position - orbitPivot.position).normalized;
            float dist = Vector3.Distance(cam.transform.position, orbitPivot.position);
            float target = Mathf.Clamp(dist - scroll * zoomSpeed, minDist, maxDist);
            cam.transform.position = orbitPivot.position + dir * target;
            cam.transform.LookAt(orbitPivot.position);
        }
    }

    // =================== NEW API (multi-unit) ===================

    public Transform GetFreeSlot()
    {
        if (slots == null) return null;
        foreach (var s in slots)
        {
            if (s == null) continue;
            if (s.childCount == 0) return s;
        }
        return null;
    }

    public UnitInstance CreateEmptyUnitAt(Transform slot)
    {
        if (slot == null || unitRootPrefab == null) return null;

        var root = Instantiate(unitRootPrefab, slot);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;

        var u = new UnitInstance
        {
            unitRoot = root.transform,
            headAnchor = root.transform.Find("HeadAnchor"),
            bodyAnchor = root.transform.Find("BodyAnchor"),
            weaponAnchor = root.transform.Find("WeaponAnchor")
        };

        units.Add(u);
        return u;
    }

    public void BuildUnit(UnitInstance u, PartSO head, PartSO body, PartSO weapon)
    {
        if (u == null) return;

        void Spawn(Transform anchor, PartSO p, ref string idStore, ref Color colStore)
        {
            if (!anchor || p == null || p.prefab == null) return;
            foreach (Transform c in anchor) Destroy(c.gameObject);
            var go = Instantiate(p.prefab, anchor);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            ApplyColorToObject(go, colStore);
            idStore = p.id;
        }

        Spawn(u.headAnchor, head, ref u.headId, ref u.headCol);
        Spawn(u.bodyAnchor, body, ref u.bodyId, ref u.bodyCol);
        Spawn(u.weaponAnchor, weapon, ref u.weaponId, ref u.weaponCol);
    }

    public void SelectPaintIndex(int i)
    {
        if (units.Count == 0) return;
        paintIndex = Mathf.Clamp(i, 0, units.Count - 1);

        if (orbitPivot && cam)
        {
            orbitPivot.position = units[paintIndex].unitRoot.position;
            float dist = Mathf.Clamp(Vector3.Distance(cam.transform.position, orbitPivot.position), minDist * 1.2f, maxDist * 0.8f);
            Vector3 dir = (cam.transform.position - orbitPivot.position).normalized;
            if (dir.sqrMagnitude < 0.001f) dir = new Vector3(0, 0.5f, 1).normalized;
            cam.transform.position = orbitPivot.position + dir * dist;
            cam.transform.LookAt(orbitPivot.position);
        }
    }

    public void PaintCurrentSlot(SlotType slot, Color c)
    {
        if (units.Count == 0) return;
        var u = units[paintIndex];

        Transform a = slot switch
        {
            SlotType.Head => u.headAnchor,
            SlotType.Body => u.bodyAnchor,
            SlotType.Weapon => u.weaponAnchor,
            _ => u.bodyAnchor
        };

        foreach (var r in a.GetComponentsInChildren<Renderer>())
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", c);
            r.SetPropertyBlock(mpb);
        }

        if (slot == SlotType.Head) u.headCol = c;
        else if (slot == SlotType.Body) u.bodyCol = c;
        else if (slot == SlotType.Weapon) u.weaponCol = c;
    }

    // UI hooks to set which sub-slot we’re painting
    public void SelectSlotHead() => selectedSlot = SlotType.Head;
    public void SelectSlotBody() => selectedSlot = SlotType.Body;
    public void SelectSlotWeapon() => selectedSlot = SlotType.Weapon;
    public SlotType GetCurrentSlot() => selectedSlot;

    // Keeps PaintPanelBuilder + PhaseController happy
    public void SetColor(Color c) => PaintCurrentSlot(selectedSlot, c);

    // =================== BACK-COMPAT SHIMS ===================

    // old preview path (used by PhaseController during SNIP/GLUE)
    public void EquipPart(PartSO part)
    {
        if (part == null || part.prefab == null) return;

        EnsurePreviewRoot();

        Transform anchor = part.slot switch
        {
            SlotType.Head => previewHead,
            SlotType.Body => previewBody,
            SlotType.Weapon => previewWeapon,
            _ => previewBody
        };

        foreach (Transform c in anchor) Destroy(c.gameObject);
        var go = Instantiate(part.prefab, anchor);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        // default preview color
        ApplyColorToObject(go, Color.gray);
    }

    // lookup used by FinalDisplay/older save code
    public PartSO FindPartById(string id)
    {
        if (string.IsNullOrEmpty(id) || kit == null) return null;
        foreach (var p in kit.heads) if (p && p.id == id) return p;
        foreach (var p in kit.bodies) if (p && p.id == id) return p;
        foreach (var p in kit.weapons) if (p && p.id == id) return p;
        return null;
    }

    // color getter used by PhaseController when persisting
    public Color GetColorFor(SlotType slot)
    {
        if (units.Count > 0)
        {
            var u = units[Mathf.Clamp(paintIndex, 0, units.Count - 1)];
            if (slot == SlotType.Head) return u.headCol;
            if (slot == SlotType.Body) return u.bodyCol;
            if (slot == SlotType.Weapon) return u.weaponCol;
        }
        // fallback
        return slot switch
        {
            SlotType.Head => Color.gray,
            SlotType.Body => Color.gray,
            SlotType.Weapon => Color.gray,
            _ => Color.gray
        };
    }

    // =================== helpers ===================
    static void ApplyColorToObject(GameObject go, Color c)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", c);
            r.SetPropertyBlock(mpb);
        }
    }

    void EnsurePreviewRoot()
    {
        if (previewRoot) return;

        previewRoot = new GameObject("PreviewRoot").transform;
        // put it somewhere unobtrusive near the pivot (or at origin)
        previewRoot.position = orbitPivot ? orbitPivot.position + new Vector3(-1.5f, 0f, 0f) : Vector3.zero;

        previewHead = new GameObject("HeadAnchor").transform; previewHead.SetParent(previewRoot, false);
        previewBody = new GameObject("BodyAnchor").transform; previewBody.SetParent(previewRoot, false);
        previewWeapon = new GameObject("WeaponAnchor").transform; previewWeapon.SetParent(previewRoot, false);

        // simple offsets so preview parts don’t overlap
        previewBody.localPosition = Vector3.zero;
        previewHead.localPosition = new Vector3(0f, 1.1f, 0f);
        previewWeapon.localPosition = new Vector3(0.35f, 0.6f, 0f);
    }
}
