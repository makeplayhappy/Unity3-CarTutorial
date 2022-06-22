using UnityEngine;
using System.Collections;

[System.Serializable]
public partial class FPSCounter : MonoBehaviour
{
    // Attach this to a GUIText to make a frames/second indicator.
    //
    // It calculates frames/second over each updateInterval,
    // so the display does not keep changing wildly.
    //
    // It is also fairly accurate at very low FPS counts (<10).
    // We do this not by simply counting frames per interval, but
    // by accumulating FPS for each frame. This way we end up with
    // correct overall FPS even if the interval renders something like
    // 5.5 frames.
    public float updateInterval;
    private float accum; // FPS accumulated over the interval
    private int frames; // Frames drawn over the interval
    private float timeleft; // Left time for current interval
    private float fps; // Current FPS
    private double lastSample;
    private int gotIntervals;
    public virtual void Start()
    {
        this.timeleft = this.updateInterval;
        this.lastSample = Time.realtimeSinceStartup;
    }

    public virtual float GetFPS()
    {
        return this.fps;
    }

    public virtual bool HasFPS()
    {
        return this.gotIntervals > 2;
    }

    public virtual void Update()
    {
        ++this.frames;
        float newSample = Time.realtimeSinceStartup;
        double deltaTime = newSample - this.lastSample;
        this.lastSample = newSample;
        this.timeleft = (float) (this.timeleft - deltaTime);
        this.accum = (float) (this.accum + (1f / deltaTime));
        // Interval ended - update GUI text and start new interval
        if (this.timeleft <= 0f)
        {
             // display two fractional digits (f2 format)
            this.fps = this.accum / this.frames;
            //        guiText.text = fps.ToString("f2");
            this.timeleft = this.updateInterval;
            this.accum = 0f;
            this.frames = 0;
            ++this.gotIntervals;
        }
    }

    public virtual void OnGUI()
    {
        GUI.Box(new Rect(Screen.width - 160, 10, 150, 40), (this.fps.ToString("f2") + " | QSetting: ") + QualitySettings.currentLevel);
    }

    public FPSCounter()
    {
        this.updateInterval = 1f;
        this.fps = 15f;
    }

}