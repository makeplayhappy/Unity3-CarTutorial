using UnityEngine;
using System.Collections;

[System.Serializable]
public partial class SoundController : MonoBehaviour
{
    /////////
    // SoundController.js
    //
    // This script controls the sound for a car. It automatically creates the needed AudioSources, and ensures
    // that only a certain number of sound are played at any time, by limiting the number of OneShot
    // audio clip that can be played at any time. This is to ensure that it does not play more sounds than Unity
    // can handle.
    // The script handles the sound for the engine, both idle and running, gearshifts, skidding and crashing.
    // PlayOneShot is used for the non-looping sounds are needed. A separate AudioSource is create for the OneShot
    // AudioClips, since the should not be affected by the pitch-changes applied to other AudioSources.
    private Car car;
    public AudioClip D;
    public float DVolume;
    public AudioClip E;
    public float EVolume;
    public AudioClip F;
    public float FVolume;
    public AudioClip K;
    public float KVolume;
    public AudioClip L;
    public float LVolume;
    public AudioClip wind;
    public float windVolume;
    public AudioClip tunnelSound;
    public float tunnelVolume;
    public AudioClip crashLowSpeedSound;
    public float crashLowVolume;
    public AudioClip crashHighSpeedSound;
    public float crashHighVolume;
    public AudioClip skidSound;
    public AudioClip BackgroundMusic;
    public float BackgroundMusicVolume;
    private AudioSource DAudio;
    private AudioSource EAudio;
    private AudioSource FAudio;
    private AudioSource KAudio;
    private AudioSource LAudio;
    private AudioSource tunnelAudio;
    private AudioSource windAudio;
    private AudioSource skidAudio;
    private AudioSource carAudio;
    private AudioSource backgroundMusic;
    private float carMaxSpeed;
    private float gearShiftTime;
    private bool shiftingGear;
    private int gearShiftsStarted;
    private int crashesStarted;
    private float crashTime;
    private int oneShotLimit;
    private float idleFadeStartSpeed;
    private float idleFadeStopSpeed;
    private float idleFadeSpeedDiff;
    private float speedFadeStartSpeed;
    private float speedFadeStopSpeed;
    private float speedFadeSpeedDiff;
    private bool soundsSet;
    private float inputFactor;
    private int gear;
    private int topGear;
    private float idlePitch;
    private float startPitch;
    private float lowPitch;
    private float medPitch;
    private float highPitchFirst;
    private float highPitchSecond;
    private float highPitchThird;
    private float highPitchFourth;
    private float shiftPitch;
    private float prevPitchFactor;
    // Create the needed AudioSources
    public virtual void Awake()
    {
        this.car = (Car) this.transform.GetComponent(typeof(Car));
        this.DVolume = this.DVolume * 0.4f;
        this.EVolume = this.EVolume * 0.4f;
        this.FVolume = this.FVolume * 0.4f;
        this.KVolume = this.KVolume * 0.7f;
        this.LVolume = this.LVolume * 0.4f;
        this.windVolume = this.windVolume * 0.4f;
        this.DAudio = (AudioSource) this.gameObject.AddComponent(typeof(AudioSource));
        this.DAudio.loop = true;
        this.DAudio.playOnAwake = true;
        this.DAudio.clip = this.D;
        this.DAudio.volume = this.DVolume;
        this.EAudio = (AudioSource) this.gameObject.AddComponent(typeof(AudioSource));
        this.EAudio.loop = true;
        this.EAudio.playOnAwake = true;
        this.EAudio.clip = this.E;
        this.EAudio.volume = this.EVolume;
        this.FAudio = (AudioSource) this.gameObject.AddComponent(typeof(AudioSource));
        this.FAudio.loop = true;
        this.FAudio.playOnAwake = true;
        this.FAudio.clip = this.F;
        this.FAudio.volume = this.FVolume;
        this.KAudio = (AudioSource) this.gameObject.AddComponent(typeof(AudioSource));
        this.KAudio.loop = true;
        this.KAudio.playOnAwake = true;
        this.KAudio.clip = this.K;
        this.KAudio.volume = this.KVolume;
        this.LAudio = (AudioSource) this.gameObject.AddComponent(typeof(AudioSource));
        this.LAudio.loop = true;
        this.LAudio.playOnAwake = true;
        this.LAudio.clip = this.L;
        this.LAudio.volume = this.LVolume;
        this.windAudio = (AudioSource) this.gameObject.AddComponent(typeof(AudioSource));
        this.windAudio.loop = true;
        this.windAudio.playOnAwake = true;
        this.windAudio.clip = this.wind;
        this.windAudio.volume = this.windVolume;
        this.tunnelAudio = (AudioSource) this.gameObject.AddComponent(typeof(AudioSource));
        this.tunnelAudio.loop = true;
        this.tunnelAudio.playOnAwake = false;
        this.tunnelAudio.clip = this.tunnelSound;
        //	tunnelAudio.maxVolume = tunnelVolume;
        this.tunnelAudio.volume = this.tunnelVolume;
        this.skidAudio = (AudioSource) this.gameObject.AddComponent(typeof(AudioSource));
        this.skidAudio.loop = true;
        this.skidAudio.playOnAwake = true;
        this.skidAudio.clip = this.skidSound;
        this.skidAudio.volume = 0f;
        this.carAudio = (AudioSource) this.gameObject.AddComponent(typeof(AudioSource));
        this.carAudio.loop = false;
        this.carAudio.playOnAwake = false;
        this.carAudio.Stop();
        this.crashTime = Mathf.Max(this.crashLowSpeedSound.length, this.crashHighSpeedSound.length);
        this.soundsSet = false;
        this.idleFadeSpeedDiff = this.idleFadeStopSpeed - this.idleFadeStartSpeed;
        this.speedFadeSpeedDiff = this.speedFadeStopSpeed - this.speedFadeStartSpeed;
        this.backgroundMusic = (AudioSource) this.gameObject.AddComponent(typeof(AudioSource));
        this.backgroundMusic.loop = true;
        this.backgroundMusic.playOnAwake = true;
        this.backgroundMusic.clip = this.BackgroundMusic;
        //	backgroundMusic.maxVolume = BackgroundMusicVolume;
        //	backgroundMusic.minVolume = BackgroundMusicVolume;
        this.backgroundMusic.volume = this.BackgroundMusicVolume;
    }

    public virtual void Update()
    {
        float carSpeed = this.car.GetComponent<Rigidbody>().velocity.magnitude;
        float carSpeedFactor = Mathf.Clamp01(carSpeed / this.car.topSpeed);
        this.KAudio.volume = Mathf.Lerp(0, this.KVolume, carSpeedFactor);
        this.windAudio.volume = Mathf.Lerp(0, this.windVolume, carSpeedFactor * 2);
        if (this.shiftingGear)
        {
            return;
        }
        float pitchFactor = this.Sinerp(0, this.topGear, carSpeedFactor);
        int newGear = (int) pitchFactor;
        pitchFactor = pitchFactor - newGear;
        float throttleFactor = pitchFactor;
        pitchFactor = pitchFactor * 0.3f;
        pitchFactor = pitchFactor + ((throttleFactor * 0.7f) * Mathf.Clamp01(this.car.throttle * 2));
        if (newGear != this.gear)
        {
            if (newGear > this.gear)
            {
                this.StartCoroutine(this.GearShift(this.prevPitchFactor, pitchFactor, this.gear, true));
            }
            else
            {
                this.StartCoroutine(this.GearShift(this.prevPitchFactor, pitchFactor, this.gear, false));
            }
            this.gear = newGear;
        }
        else
        {
            float newPitch = 0;
            if (this.gear == 0)
            {
                newPitch = Mathf.Lerp(this.idlePitch, this.highPitchFirst, pitchFactor);
            }
            else
            {
                if (this.gear == 1)
                {
                    newPitch = Mathf.Lerp(this.startPitch, this.highPitchSecond, pitchFactor);
                }
                else
                {
                    if (this.gear == 2)
                    {
                        newPitch = Mathf.Lerp(this.lowPitch, this.highPitchThird, pitchFactor);
                    }
                    else
                    {
                        newPitch = Mathf.Lerp(this.medPitch, this.highPitchFourth, pitchFactor);
                    }
                }
            }
            this.SetPitch(newPitch);
            this.SetVolume(newPitch);
        }
        this.prevPitchFactor = pitchFactor;
    }

    public virtual float Coserp(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, 1f - Mathf.Cos((value * Mathf.PI) * 0.5f));
    }

    public virtual float Sinerp(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, Mathf.Sin((value * Mathf.PI) * 0.5f));
    }

    public virtual void SetPitch(float pitch)
    {
        this.DAudio.pitch = pitch;
        this.EAudio.pitch = pitch;
        this.FAudio.pitch = pitch;
        this.LAudio.pitch = pitch;
        this.tunnelAudio.pitch = pitch;
    }

    public virtual void SetVolume(float pitch)
    {
        float pitchFactor = Mathf.Lerp(0, 1, (pitch - this.startPitch) / (this.highPitchSecond - this.startPitch));
        this.DAudio.volume = Mathf.Lerp(0, this.DVolume, pitchFactor);
        float fVolume = Mathf.Lerp(this.FVolume * 0.8f, this.FVolume, pitchFactor);
        this.FAudio.volume = (fVolume * 0.7f) + ((fVolume * 0.3f) * Mathf.Clamp01(this.car.throttle));
        float eVolume = Mathf.Lerp(this.EVolume * 0.89f, this.EVolume, pitchFactor);
        this.EAudio.volume = (eVolume * 0.8f) + ((eVolume * 0.2f) * Mathf.Clamp01(this.car.throttle));
    }

    public virtual IEnumerator GearShift(float oldPitchFactor, float newPitchFactor, int gear, bool shiftUp)
    {
        this.shiftingGear = true;
        float timer = 0;
        float pitchFactor = 0;
        float newPitch = 0;
        if (shiftUp)
        {
            while (timer < this.gearShiftTime)
            {
                pitchFactor = Mathf.Lerp(oldPitchFactor, 0, timer / this.gearShiftTime);
                if (gear == 0)
                {
                    newPitch = Mathf.Lerp(this.lowPitch, this.highPitchFirst, pitchFactor);
                }
                else
                {
                    newPitch = Mathf.Lerp(this.lowPitch, this.highPitchSecond, pitchFactor);
                }
                this.SetPitch(newPitch);
                this.SetVolume(newPitch);
                timer = timer + Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            while (timer < this.gearShiftTime)
            {
                pitchFactor = Mathf.Lerp(0, 1, timer / this.gearShiftTime);
                newPitch = Mathf.Lerp(this.lowPitch, this.shiftPitch, pitchFactor);
                this.SetPitch(newPitch);
                this.SetVolume(newPitch);
                timer = timer + Time.deltaTime;
                yield return null;
            }
        }
        this.shiftingGear = false;
    }

    public virtual void Skid(bool play, float volumeFactor)
    {
        if (!this.skidAudio)
        {
            return;
        }
        if (play)
        {
            this.skidAudio.volume = Mathf.Clamp01(volumeFactor + 0.3f);
        }
        else
        {
            this.skidAudio.volume = 0f;
        }
    }

    // Checks if the max amount of crash sounds has been started, and
    // if the max amount of total one shot sounds has been started.
    public virtual IEnumerator Crash(float volumeFactor)
    {
        if ((this.crashesStarted > 3) || this.OneShotLimitReached())
        {
            yield break;
        }
        if (volumeFactor > 0.9f)
        {
            this.carAudio.PlayOneShot(this.crashHighSpeedSound, Mathf.Clamp01((0.5f + (volumeFactor * 0.5f)) * this.crashHighVolume));
        }
        this.carAudio.PlayOneShot(this.crashLowSpeedSound, Mathf.Clamp01(volumeFactor * this.crashLowVolume));
        this.crashesStarted++;
        yield return new WaitForSeconds(this.crashTime);
        this.crashesStarted--;
    }

    // A function for testing if the maximum amount of OneShot AudioClips
    // has been started.
    public virtual bool OneShotLimitReached()
    {
        return (this.crashesStarted + this.gearShiftsStarted) > this.oneShotLimit;
    }

    public virtual void OnTriggerEnter(Collider coll)
    {
        SoundToggler st = (SoundToggler) coll.transform.GetComponent(typeof(SoundToggler));
        if (st)
        {
            this.StartCoroutine(this.ControlSound(true, st.fadeTime));
        }
    }

    public virtual void OnTriggerExit(Collider coll)
    {
        SoundToggler st = (SoundToggler) coll.transform.GetComponent(typeof(SoundToggler));
        if (st)
        {
            this.StartCoroutine(this.ControlSound(false, st.fadeTime));
        }
    }

    public virtual IEnumerator ControlSound(bool play, float fadeTime)
    {
        float timer = 0;
        if (play && !this.tunnelAudio.isPlaying)
        {
            this.tunnelAudio.volume = 0;
            this.tunnelAudio.Play();
            while (timer < fadeTime)
            {
                this.tunnelAudio.volume = Mathf.Lerp(0, this.tunnelVolume, timer / fadeTime);
                timer = timer + Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            if (!play && this.tunnelAudio.isPlaying)
            {
                while (timer < fadeTime)
                {
                    this.tunnelAudio.volume = Mathf.Lerp(0, this.tunnelVolume, timer / fadeTime);
                    timer = timer + Time.deltaTime;
                    yield return null;
                }
                this.tunnelAudio.Stop();
            }
        }
    }

    public SoundController()
    {
        this.DVolume = 1f;
        this.EVolume = 1f;
        this.FVolume = 1f;
        this.KVolume = 1f;
        this.LVolume = 1f;
        this.windVolume = 1f;
        this.tunnelVolume = 1f;
        this.crashLowVolume = 1f;
        this.crashHighVolume = 1f;
        this.BackgroundMusicVolume = 0.6f;
        this.carMaxSpeed = 55f;
        this.gearShiftTime = 0.1f;
        this.crashTime = 0.2f;
        this.oneShotLimit = 8;
        this.idleFadeStartSpeed = 3f;
        this.idleFadeStopSpeed = 10f;
        this.idleFadeSpeedDiff = 7f;
        this.speedFadeStopSpeed = 8f;
        this.speedFadeSpeedDiff = 8f;
        this.topGear = 6;
        this.idlePitch = 0.7f;
        this.startPitch = 0.85f;
        this.lowPitch = 1.17f;
        this.medPitch = 1.25f;
        this.highPitchFirst = 1.65f;
        this.highPitchSecond = 1.76f;
        this.highPitchThird = 1.8f;
        this.highPitchFourth = 1.86f;
        this.shiftPitch = 1.44f;
    }

}