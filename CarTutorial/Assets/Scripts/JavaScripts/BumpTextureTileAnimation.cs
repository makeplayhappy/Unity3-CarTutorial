using UnityEngine;
using System.Collections;

[System.Serializable]
public partial class BumpTextureTileAnimation : MonoBehaviour
{
    public int uvAnimationTileX;
    public int uvAnimationTileY;
    public float framesPerSecond;
    private Vector2 initTiling;
    public virtual void Start()
    {
        this.initTiling = this.GetComponent<Renderer>().material.GetTextureScale("_BumpMap");
    }

    public virtual void Update()
    {
        int index = (int) (Time.time * this.framesPerSecond);
        index = index % (this.uvAnimationTileX * this.uvAnimationTileY);
        Vector2 size = new Vector2(1f / this.uvAnimationTileX, 1f / this.uvAnimationTileY);
        float uIndex = index % this.uvAnimationTileX;
        float vIndex = index / this.uvAnimationTileX;
        Vector2 offset = new Vector2(uIndex * size.x, (1f - size.y) - (vIndex * size.y));
        size = Vector2.Scale(size, this.initTiling);
        this.GetComponent<Renderer>().material.SetTextureOffset("_BumpMap", offset);
        this.GetComponent<Renderer>().material.SetTextureScale("_BumpMap", size);
    }

    public BumpTextureTileAnimation()
    {
        this.uvAnimationTileX = 4;
        this.uvAnimationTileY = 4;
        this.framesPerSecond = 60f;
        this.initTiling = Vector2.zero;
    }

}