using System;
using UnityEngine;

public class TimeController : MonoBehaviour
{
    public static TimeController I;

    public enum Speed { Pause = 0, X1 = 1, X2 = 2, X5 = 5 }

    public Speed current = Speed.X1;
    public float baseTickSeconds = 1f; // 1 day per tick
    float accum = 0f;

    public static event Action OnDailyTick;

    void Awake()
    {
        if (I != null && I != this) Destroy(gameObject);
        else I = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
        if (Input.GetKeyDown(KeyCode.LeftBracket)) CycleDown();
        if (Input.GetKeyDown(KeyCode.RightBracket)) CycleUp();

        if (current == Speed.Pause) return;

        accum += Time.deltaTime;
        float tickSeconds = baseTickSeconds / Mathf.Max(1, (int)current);
        if (accum >= tickSeconds)
        {
            accum = 0f;
            OnDailyTick?.Invoke();
        }
    }

    public void TogglePause()
    {
        current = current == Speed.Pause ? Speed.X1 : Speed.Pause;
    }

    public void CycleUp()
    {
        current = current switch
        {
            Speed.Pause => Speed.X1,
            Speed.X1 => Speed.X2,
            Speed.X2 => Speed.X5,
            _ => Speed.X5
        };
    }

    public void CycleDown()
    {
        current = current switch
        {
            Speed.X5 => Speed.X2,
            Speed.X2 => Speed.X1,
            Speed.X1 => Speed.Pause,
            _ => Speed.Pause
        };
    }
}
