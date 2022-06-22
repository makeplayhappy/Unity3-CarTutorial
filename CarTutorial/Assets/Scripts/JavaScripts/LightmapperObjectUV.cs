using UnityEngine;
using System.Collections;

[System.Serializable]
[UnityEngine.ExecuteInEditMode]
public partial class LightmapperObjectUV : MonoBehaviour
{
    public float fudgeScale;
    public Transform theObject;
    public Vector3 terrainSize; // inverse the z here
    public virtual void Start()
    {
        if (!this.theObject)
        {
            this.theObject = this.transform;
        }
    }

    public virtual void OnRenderObject()
    {
        Vector3 inverseScale = new Vector3(1f / this.terrainSize.x, 1f / this.terrainSize.y, 1f / this.terrainSize.z);
        Matrix4x4 uvMat = Matrix4x4.TRS(Vector3.Scale(new Vector3(this.theObject.position.x, this.theObject.position.z, this.theObject.position.y), inverseScale), Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(this.theObject.rotation), inverseScale * this.fudgeScale);
        Shader.SetGlobalMatrix("_LightmapMatrix", uvMat);
    }

    public LightmapperObjectUV()
    {
        this.fudgeScale = 1f;
        this.terrainSize = new Vector3(1450, 1450, -1450);
    }

}