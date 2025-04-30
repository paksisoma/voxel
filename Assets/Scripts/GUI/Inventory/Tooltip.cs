using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
    public static Tooltip Instance { get; private set; }

    public RectTransform tooltip;
    public ContentSizeFitter contentSizeFitter;
    public Text label;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        tooltip.position = Input.mousePosition;
    }

    public void Show(Item item)
    {
        label.text = item.itemName;

        if (item is Armor)
        {
            Armor armor = (Armor)item;

            label.text += "\n\nDefense: +" + Mathf.Round(armor.defense * 100);
            label.text += "\nResistance: +" + Mathf.Round(armor.resistance * 100);
        }

        tooltip.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip);
    }

    public void Hide()
    {
        tooltip.gameObject.SetActive(false);
    }
}