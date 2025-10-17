using UnityEngine;

public enum SlotType { Head, Body, Weapon }

[CreateAssetMenu(menuName = "Hobby/Part")]
public class PartSO : ScriptableObject
{
    public string id;                 // e.g. "head_basic_01"
    public string displayName;        // e.g. "Knight Visor" (optional)
    public Sprite icon;               // optional
    public SlotType slot;
    public GameObject prefab;
}
