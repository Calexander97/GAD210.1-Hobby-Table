using UnityEngine;

public class FinalDisplayController : MonoBehaviour
{
    public HobbyDeskController unitRigPrefab; // reuse your rig or make a tiny display prefab
    public Transform gridParent;
    public int columns = 5;
    public float spacing = 1.2f;
    public KitSO kit; // assign so we can resolve ids to PartSO via desk.FindPartById

    void Start()
    {
        var json = PlayerPrefs.GetString("Hobby_Final_Showcase", "");
        if (string.IsNullOrEmpty(json)) return;
        var list = JsonUtility.FromJson<HobbyDeskController.SaveList>(json);
        if (list == null) return;

        for (int i = 0; i < list.items.Count; i++)
        {
            var r = i / columns;
            var c = i % columns;
            var pos = new Vector3(c * spacing, 0, r * spacing);

            var rig = Instantiate(unitRigPrefab, gridParent);
            rig.transform.localPosition = pos;
            rig.kit = kit;

            var u = list.items[i];
            if (!string.IsNullOrEmpty(u.bodyId)) rig.EquipPart(rig.FindPartById(u.bodyId));
            if (!string.IsNullOrEmpty(u.headId)) rig.EquipPart(rig.FindPartById(u.headId));
            if (!string.IsNullOrEmpty(u.weaponId)) rig.EquipPart(rig.FindPartById(u.weaponId));

            rig.SelectSlotBody(); rig.SetColor(u.bodyCol);
            rig.SelectSlotHead(); rig.SetColor(u.headCol);
            rig.SelectSlotWeapon(); rig.SetColor(u.weaponCol);
        }
    }
}
