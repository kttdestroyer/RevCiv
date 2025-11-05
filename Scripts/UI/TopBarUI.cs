using UnityEngine;

public class TopBarUI : MonoBehaviour
{
    public UnityEngine.UI.Text text;
    int day = 0;

    void OnEnable()
    {
        TimeController.OnDailyTick += OnDay;
    }

    void OnDisable()
    {
        TimeController.OnDailyTick -= OnDay;
    }

    void OnDay()
    {
        day++;
    }

    void Update()
    {
        if (text)
            text.text = $"Day {day} | Speed: {(TimeController.I ? TimeController.I.current.ToString() : "N/A")}";
    }
}
