using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hobby/Kit")]
public class KitSO : ScriptableObject
{
    public string kitName = "Starter Kit";
    public int unitCount = 10; // Spearmen=10, Knights=3

    public List<PartSO> heads;
    public List<PartSO> bodies;
    public List<PartSO> weapons;
}
