// PaintPanelBuilder.cs (unchanged but ensure using UnityEngine.UI;)
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PaintPanelBuilder : MonoBehaviour
{
    public PhaseController phase;
    public HobbyDeskController desk;
    public RectTransform swatchGrid;
    public TMP_Text currentSlotText;

    public Color[] palette = new Color[] {
        Color.white, Color.black, Color.gray,
        new Color(0.8f,0.1f,0.1f), new Color(0.1f,0.6f,0.9f),
        new Color(0.15f,0.7f,0.25f), new Color(0.95f,0.65f,0.1f),
        new Color(0.6f,0.3f,0.9f), new Color(0.7f,0.4f,0.2f),
        new Color(0.8f,0.8f,0.8f), new Color(0.4f,0.4f,0.4f), new Color(0.9f,0.3f,0.3f)
    };

    void OnEnable() { BuildSwatches(); UpdateSlotLabel(); }
    void Update() { UpdateSlotLabel(); }

    void BuildSwatches()
    {
        foreach (Transform c in swatchGrid) Destroy(c.gameObject);
        foreach (var c in palette)
        {
            var go = new GameObject("Swatch", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(swatchGrid, false);
            go.GetComponent<Image>().color = c;
            var picked = c;
            go.GetComponent<Button>().onClick.AddListener(() => phase.OnColorPicked(picked));
        }
    }

    void UpdateSlotLabel()
    {
        if (!currentSlotText) return;
        currentSlotText.text = $"Slot: {desk.GetCurrentSlot()}";
    }
}
