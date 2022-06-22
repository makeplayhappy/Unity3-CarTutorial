using UnityEngine;
using System.Collections;

[System.Serializable]
public partial class PerformanceTweak : MonoBehaviour
{
    public FPSCounter fpsCounter;
    public Terrain terrain;
    public float messageTime;
    public float scrollTime;
    private object[] messages;
    private object[] times;
    private float lastTime;
    private bool doneNotes;
    private float origDetailDist;
    private float origSplatDist;
    private float origTreeDist;
    private int origMaxLOD;
    private bool softVegetationOff;
    private bool splatmapsOff;
    private float lowFPS;
    private float highFPS;
    private float skipChangesTimeout;
    private int nextTerrainChange;
    public virtual void Start()
    {
        if (!this.fpsCounter || !this.terrain)
        {
            Debug.LogWarning("Some of performance objects are not set up");
            this.enabled = false;
            return;
        }
        this.origDetailDist = this.terrain.detailObjectDistance;
        this.origSplatDist = this.terrain.basemapDistance;
        this.origTreeDist = this.terrain.treeDistance;
        this.origMaxLOD = this.terrain.heightmapMaximumLOD;
        this.skipChangesTimeout = 0f;
        float[] distances = new float[32];
        distances[16] = Camera.main.farClipPlane;
        Camera.main.layerCullDistances = distances;
    }

    public virtual void Update()
    {
        if (!this.fpsCounter || !this.terrain)
        {
            return;
        }
        if (!this.doneNotes && !Application.isEditor)
        {
            string gfxCard = SystemInfo.graphicsDeviceName.ToLower();
            string gfxVendor = SystemInfo.graphicsDeviceVendor.ToLower();
            if (gfxVendor.Contains("intel"))
            {
                 // on pre-GMA950, increase fog and reduce far plane by 4x :)
                this.softVegetationOff = true;
                QualitySettings.softVegetation = false;
                this.AddMessage("Note: turning off soft vegetation (Intel video card detected)");
            }
            else
            {
                if (gfxVendor == "sis")
                {
                    this.softVegetationOff = true;
                    QualitySettings.softVegetation = false;
                    this.AddMessage("Note: turning off soft vegetation (SIS video card detected)");
                }
                else
                {
                    if (gfxCard.Contains("geforce") && ((gfxCard.Contains("5200") || gfxCard.Contains("5500")) || gfxCard.Contains("6100")))
                    {
                         // on slow/old geforce cards, increase fog and reduce far plane by 2x
                        this.ReduceDrawDistance(2f, "Note: reducing draw distance (slow GeForce card detected)");
                        this.softVegetationOff = true;
                        QualitySettings.softVegetation = false;
                        this.AddMessage("Note: turning off soft vegetation (slow GeForce card detected)");
                    }
                    else
                    {
                    }
                }
            }
             // on other old cards, increase fog and reduce far plane by 2x
            //			if( hwWater == IslandWater.WaterMode.Simple )
            //			{
            //				ReduceDrawDistance( 2.0, "Note: reducing draw distance (old video card detected)" );
            //			}
            this.skipChangesTimeout = 0f;
            this.doneNotes = true;
        }
        this.DoTweaks();
        this.UpdateMessages();
    }

    public virtual void ReduceDrawDistance(float factor, string message)
    {
        this.AddMessage(message);
        //	RenderSettings.fogDensity *= factor;
        //	Camera.main.farClipPlane /= factor;
        float[] distances = Camera.main.layerCullDistances;
        int i = 0;
        while (i < distances.Length)
        {
            distances[i] = distances[i] / factor;
            i++;
        }
        Camera.main.layerCullDistances = distances;
    }

    public virtual void OnDisable()
    {
        QualitySettings.softVegetation = true;
    }

    public virtual void DoTweaks()
    {
        if (!this.fpsCounter.HasFPS())
        {
            return; // enough time did not pass yet to get decent FPS count
        }
        float fps = this.fpsCounter.GetFPS();
        // don't do too many adjustments at time... allow one per
        // FPS update interval
        this.skipChangesTimeout = this.skipChangesTimeout - Time.deltaTime;
        if (this.skipChangesTimeout < 0f)
        {
            this.skipChangesTimeout = 0f;
        }
        if (this.skipChangesTimeout > 0f)
        {
            return;
        }
        // terrain tweaks
        if (fps > 25f)
        {
             // bump up!
            ++this.nextTerrainChange;
            if (this.nextTerrainChange >= 4)
            {
                this.nextTerrainChange = 0;
            }
            if ((this.nextTerrainChange == 0) && (this.terrain.detailObjectDistance < this.origDetailDist))
            {
                this.terrain.detailObjectDistance = this.terrain.detailObjectDistance * 2f;
                if (!this.softVegetationOff)
                {
                    QualitySettings.softVegetation = true;
                }
                this.AddMessage("Framerate ok, increasing vegetation detail");
                return;
            }
            if (((this.nextTerrainChange == 1) && !this.splatmapsOff) && (this.terrain.basemapDistance < this.origSplatDist))
            {
                this.terrain.basemapDistance = this.terrain.basemapDistance * 2f;
                this.AddMessage("Framerate ok, increasing terrain texture detail");
                return;
            }
            if ((this.nextTerrainChange == 2) && (this.terrain.treeDistance < this.origTreeDist))
            {
                this.terrain.treeDistance = this.terrain.treeDistance * 2f;
                this.AddMessage("Framerate ok, increasing tree draw distance");
                return;
            }
        }
        if (fps < this.lowFPS)
        {
             // lower it
            ++this.nextTerrainChange;
            if (this.nextTerrainChange >= 4)
            {
                this.nextTerrainChange = 0;
                this.lowFPS = 10f; // ok, this won't be fast...
            }
            if ((this.nextTerrainChange == 0) && (this.terrain.detailObjectDistance >= (this.origDetailDist / 16f)))
            {
                this.terrain.detailObjectDistance = this.terrain.detailObjectDistance * 0.5f;
                QualitySettings.softVegetation = false;
                this.AddMessage("Framerate low, reducing vegetation detail");
                return;
            }
            if (((this.nextTerrainChange == 1) && !this.splatmapsOff) && (this.terrain.basemapDistance >= (this.origSplatDist / 16f)))
            {
                this.terrain.basemapDistance = this.terrain.basemapDistance * 0.5f;
                this.AddMessage("Framerate low, reducing terrain texture detail");
                return;
            }
            if ((this.nextTerrainChange == 2) && (this.terrain.treeDistance >= (this.origTreeDist / 16f)))
            {
                this.terrain.treeDistance = this.terrain.treeDistance * 0.5f;
                this.AddMessage("Framerate low, reducing tree draw distance");
                return;
            }
        }
        if (fps < 20)
        {
            if (QualitySettings.currentLevel > QualityLevel.Fastest)
            {
                QualitySettings.DecreaseLevel();
            }
        }
        else
        {
            if (fps > this.highFPS)
            {
                if (QualitySettings.currentLevel < QualityLevel.Fantastic)
                {
                    QualitySettings.IncreaseLevel();
                }
            }
        }
        if (QualitySettings.currentLevel < QualityLevel.Good)
        {
            Shader sh = Shader.Find("VertexLit");
            GameObject[] bumpedObjects = GameObject.FindGameObjectsWithTag("Bumped");
            int i = 0;
            while (i < bumpedObjects.Length)
            {
                bumpedObjects[i].GetComponent<Renderer>().material.shader = sh;
                i++;
            }
        }
    }

    public virtual void AddMessage(string t)
    {
        this.messages.Add(t);
        this.times.Add(this.messageTime);
        this.lastTime = this.scrollTime;
        this.skipChangesTimeout = this.fpsCounter.updateInterval * 3f;
    }

    public virtual void UpdateMessages()
    {
        float dt = Time.deltaTime;
        foreach (object t in this.times)
        {
            t = ((float) t) - dt;
        }
        while ((this.times.Length > 0) && (this.times[0] < 0f))
        {
            this.times.Shift();
            this.messages.Shift();
        }
        this.lastTime = this.lastTime - dt;
        if (this.lastTime < 0f)
        {
            this.lastTime = 0f;
        }
    }

    public virtual void OnGUI()
    {
        int height = 15;
        int n = this.messages.Length;
        Rect rc = new Rect(2, ((Screen.height - 2) - (n * height)) + ((this.lastTime / this.scrollTime) * height), 600, 20);
        int i = 0;
        while (i < n)
        {
            string text = (string) this.messages[i];
            float time = (float) this.times[i];
            float alpha = time / this.messageTime;
            if (alpha < 0.2f)
            {

                {
                    float _13 = alpha / 0.2f;
                    Color _14 = GUI.color;
                    _14.a = _13;
                    GUI.color = _14;
                }
            }
            else
            {
                if (alpha > 0.9f)
                {

                    {
                        float _15 = 1f - ((alpha - 0.9f) / (1 - 0.9f));
                        Color _16 = GUI.color;
                        _16.a = _15;
                        GUI.color = _16;
                    }
                }
                else
                {

                    {
                        float _17 = 1f;
                        Color _18 = GUI.color;
                        _18.a = _17;
                        GUI.color = _18;
                    }
                }
            }
            GUI.Label(rc, text);
            rc.y = rc.y + height;
            ++i;
        }
    }

    public PerformanceTweak()
    {
        this.messageTime = 10f;
        this.scrollTime = 0.7f;
        this.messages = new object[0];
        this.times = new object[0];
        this.lowFPS = 15f;
        this.highFPS = 35f;
        this.skipChangesTimeout = 1f;
    }

}