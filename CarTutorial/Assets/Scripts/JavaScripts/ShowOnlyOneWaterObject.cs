using UnityEngine;
using System.Collections;

[System.Serializable]
public partial class ShowOnlyOneWaterObject : MonoBehaviour
{
    public GameObject nearWaterObject;
    public GameObject farWaterObject;
    public bool near;
    public virtual void Awake()
    {
        this.UpdateVisibility();
    }

    public virtual void OnTriggerEnter()
    {
        this.near = true;
        this.UpdateVisibility();
    }

    public virtual void OnTriggerExit()
    {
        this.near = false;
        this.UpdateVisibility();
    }

    public virtual void UpdateVisibility()
    {
        this.nearWaterObject.GetComponent<Renderer>().enabled = this.near;
        this.farWaterObject.GetComponent<Renderer>().enabled = !this.near;
    }

}