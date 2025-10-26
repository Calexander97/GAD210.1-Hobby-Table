using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PaintPanelBuilder : MonoBehaviour
{
    public PhaseController phase;        // drag PhaseController
    public HobbyDeskController desk;     // drag HobbyDeskController
    public RectTransform swatchGrid;     // the grid object
    public TMP_Text currentSlotText;     // current slot text

    // basic palette
    public Color[] palette = new Color[]
    {
        Color.white, Color.black, Color.gray,
        new Color(0.8f,0.1f,0.1f),   // red
        new Color(0.1f,0.6f,0.9f),   // blue
        new Color(0.15f,0.7f,0.25f), // green
        new Color(0.95f,0.65f,0.1f), // yellow
        new Color(0.6f,0.3f,0.9f),   // purple
        new Color(0.7f,0.4f,0.2f),   // brown
        new Color(0.8f,0.8f,0.8f),   // light gray
        new Color(0.4f,0.4f,0.4f),   // dark gray
        new Color(0.9f,0.3f,0.3f)
    };

    void OnEnable()
    {
        BuildSwatches();
        UpdateSlotLabel();
    }

    void Update()
    {
        if (currentSlotText) UpdateSlotLabel();
    }

    void BuildSwatches()
    {
        foreach (Transform c in swatchGrid) Destroy(c.gameObject);

        foreach (var c in palette)
        {
            var go = new GameObject("Swatch", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(swatchGrid, false);
            var img = go.GetComponent<Image>();
            img.color = c;
            var btn = go.GetComponent<Button>();
            Color picked = c;
            btn.onClick.AddListener(() =>
            {
                phase.OnColorPicked(picked);
            });
        }
    }

    void UpdateSlotLabel()
    {
        if (!currentSlotText) return;
        currentSlotText.text = $"Slot: {desk.GetCurrentSlot()}";
    }
}
