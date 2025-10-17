using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class KitSelector : MonoBehaviour
{
    public List<KitSO> kits;
    public TMP_Dropdown dropdown;
    public SnipPanelSpawner snip;
    public TMP_Text phaseHeaderText;   // optional "Active Phase" text

    void Start()
    {
        dropdown.ClearOptions();
        var options = new List<TMP_Dropdown.OptionData>();
        foreach (var k in kits) options.Add(new TMP_Dropdown.OptionData(k.kitName));
        dropdown.AddOptions(options);
        dropdown.onValueChanged.AddListener(OnChanged);
        if (kits.Count > 0) snip.RefreshFromKit(kits[0]);
    }

    void OnChanged(int index)
    {
        if (index >= 0 && index < kits.Count) snip.RefreshFromKit(kits[index]);
    }
}
