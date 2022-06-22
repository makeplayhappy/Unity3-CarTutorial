using UnityEngine;
using System.Collections;

[System.Serializable]
public partial class CrashController : MonoBehaviour
{
    public SoundController sound;
    private Car car;
    public virtual void OnCollisionEnter(Collision collInfo)
    {
        if (this.enabled && (collInfo.contacts.Length > 0))
        {
            float volumeFactor = Mathf.Clamp01(collInfo.relativeVelocity.magnitude * 0.08f);
            volumeFactor = volumeFactor * Mathf.Clamp01(0.3f + Mathf.Abs(Vector3.Dot(collInfo.relativeVelocity.normalized, collInfo.contacts[0].normal)));
            volumeFactor = (volumeFactor * 0.5f) + 0.5f;
            this.StartCoroutine(this.sound.Crash(volumeFactor));
        }
    }

    public virtual void Start()
    {
        this.sound = (SoundController) this.transform.root.GetComponent(typeof(SoundController));
        this.car = (Car) this.transform.GetComponent(typeof(Car));
    }

}