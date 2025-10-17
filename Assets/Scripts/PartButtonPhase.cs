using UnityEngine;
using UnityEngine.UI;

public class PartButtonPhase : MonoBehaviour
{
    public PartSO part;
    public PhaseController phase;
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => phase.OnPartClicked(part));
    }
}
