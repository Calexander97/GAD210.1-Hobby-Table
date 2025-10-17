using UnityEngine;
using UnityEngine.UI;

public class ColorButtonPhase : MonoBehaviour
{
    public HobbyDeskController controller;
    public PhaseController phase;
    public Color color = Color.gray;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => {
            controller.SetColor(color);
            phase.OnColorPicked(color);
        });
    }
}
