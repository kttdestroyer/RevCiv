using UnityEngine;
using UnityEngine.UI;

public class ResourceHUD : MonoBehaviour
{
    [Header("References")]
    public DataRegistry registry;
    public SelectionController selectionController;
    public Camera sceneCamera;

    [Header("UI Text Targets")]
    public Text titleText;
    public Text foodText;
    public Text woodText;
    public Text stoneText;
    public Text metalText;
    public Text knowledgeText;

    [Header("Find Target Options")]
    public bool preferSelectedUnitOwner = true;
    public bool preferSelectedSettlement = true;

    void Awake()
    {
        if (!selectionController)
        {
#if UNITY_6000_0_OR_NEWER
            selectionController = FindFirstObjectByType<SelectionController>();
#else
            selectionController = FindObjectOfType<SelectionController>();
#endif
        }
        if (!sceneCamera) sceneCamera = Camera.main;
    }

    void Update()
    {
        var s = FindTargetSettlement();
        if (!s) { RenderEmpty(); return; }
        Render(s);
    }

    Settlement FindTargetSettlement()
    {
        if (preferSelectedSettlement && selectionController && selectionController.selectedSettlement)
            return selectionController.selectedSettlement;

        if (preferSelectedUnitOwner && selectionController && selectionController.selectedUnit)
        {
            var u = selectionController.selectedUnit;
            var t = u.currentTile;
            if (t && t.ownerSettlementId >= 0)
            {
#if UNITY_6000_0_OR_NEWER
                var all = FindObjectsByType<Settlement>(FindObjectsSortMode.None);
#else
                var all = FindObjectsOfType<Settlement>();
#endif
                for (int i = 0; i < all.Length; i++)
                    if (all[i].settlementId == t.ownerSettlementId)
                        return all[i];
            }
        }

        // Fallback: nearest to camera
        Settlement nearest = null;
        float best = float.PositiveInfinity;
#if UNITY_6000_0_OR_NEWER
        var settlements = FindObjectsByType<Settlement>(FindObjectsSortMode.None);
#else
        var settlements = FindObjectsOfType<Settlement>();
#endif
        Vector3 refPos = sceneCamera ? sceneCamera.transform.position : Vector3.zero;

        for (int i = 0; i < settlements.Length; i++)
        {
            float d = Vector3.SqrMagnitude(settlements[i].transform.position - refPos);
            if (d < best) { best = d; nearest = settlements[i]; }
        }
        return nearest;
    }

    void RenderEmpty()
    {
        if (titleText) titleText.text = "No Settlement";
        if (foodText) foodText.text = "Food: -";
        if (woodText) woodText.text = "Wood: -";
        if (stoneText) stoneText.text = "Stone: -";
        if (metalText) metalText.text = "Metal: -";
        if (knowledgeText) knowledgeText.text = "Knowledge: -";
    }

    void Render(Settlement s)
    {
        if (!registry)
        {
            if (titleText) titleText.text = "HUD: Registry not assigned";
            return;
        }
        var food = registry.FindResource("food");
        var wood = registry.FindResource("wood");
        var stone = registry.FindResource("stone");
        var metal = registry.FindResource("metal");
        var knowledge = registry.FindResource("knowledge");

        if (titleText) titleText.text = $"{s.settlementName} (Pop {s.population})";
        if (foodText) foodText.text = $"Food: {s.Get(food)}";
        if (woodText) woodText.text = $"Wood: {s.Get(wood)}";
        if (stoneText) stoneText.text = $"Stone: {s.Get(stone)}";
        if (metalText) metalText.text = $"Metal: {s.Get(metal)}";
        if (knowledgeText) knowledgeText.text = $"Knowledge: {s.Get(knowledge)}";
    }
}
