using UnityEngine;
using System.Collections;

[System.Serializable]
public partial class SoundToggler : MonoBehaviour
{
    public float fadeTime;
    private SoundController soundScript;
    public virtual void Start()
    {
        this.soundScript = (SoundController) UnityEngine.Object.FindObjectOfType(typeof(SoundController));
    }

    public virtual void OnTriggerEnter()
    {
        this.StartCoroutine(this.soundScript.ControlSound(true, this.fadeTime));
    }

    public virtual void OnTriggerExit()
    {
        this.StartCoroutine(this.soundScript.ControlSound(false, this.fadeTime));
    }

    public SoundToggler()
    {
        this.fadeTime = 1f;
    }

}