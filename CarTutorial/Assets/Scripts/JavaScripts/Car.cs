using UnityEngine;
using System.Collections;

[System.Serializable]
public class Wheel : object
{
    public WheelCollider collider;
    public Transform wheelGraphic;
    public Transform tireGraphic;
    public bool driveWheel;
    public bool steerWheel;
    public int lastSkidmark;
    public Vector3 lastEmitPosition;
    public float lastEmitTime;
    public Vector3 wheelVelo;
    public Vector3 groundSpeed;
    public Wheel()
    {
        this.lastSkidmark = -1;
        this.lastEmitPosition = Vector3.zero;
        this.lastEmitTime = Time.time;
        this.wheelVelo = Vector3.zero;
        this.groundSpeed = Vector3.zero;
    }

}
[System.Serializable]
public partial class Car : MonoBehaviour
{
    private float wheelRadius;
    public float suspensionRange;
    public float suspensionDamper;
    public float suspensionSpringFront;
    public float suspensionSpringRear;
    public Material brakeLights;
    public Vector3 dragMultiplier;
    public float throttle;
    private float steer;
    private bool handbrake;
    public Transform centerOfMass;
    public Transform[] frontWheels;
    public Transform[] rearWheels;
    private Wheel[] wheels;
    private WheelFrictionCurve wfc;
    public float topSpeed;
    public int numberOfGears;
    public int maximumTurn;
    public int minimumTurn;
    public float resetTime;
    private float resetTimer;
    private float[] engineForceValues;
    private float[] gearSpeeds;
    private int currentGear;
    private float currentEnginePower;
    private float handbrakeXDragFactor;
    private float initialDragMultiplierX;
    private float handbrakeTime;
    private float handbrakeTimer;
    private Skidmarks skidmarks;
    private ParticleEmitter skidSmoke;
    public float[] skidmarkTime;
    private SoundController sound;
    private bool canSteer;
    private bool canDrive;
    public virtual void Start()
    {
        this.wheels = new Wheel[this.frontWheels.Length + this.rearWheels.Length];
        this.sound = (SoundController) this.transform.GetComponent(typeof(SoundController));
         // Measuring 1 - 60
        accelerationTimer = Time.time;
        this.SetupWheelColliders();
        this.SetupCenterOfMass();
        this.topSpeed = this.Convert_Miles_Per_Hour_To_Meters_Per_Second(this.topSpeed);
        this.SetupGears();
        this.SetUpSkidmarks();
        this.initialDragMultiplierX = this.dragMultiplier.x;
    }

    public virtual void Update()
    {
        Vector3 relativeVelocity = this.transform.InverseTransformDirection(this.GetComponent<Rigidbody>().velocity);
        this.GetInput();
        this.Check_If_Car_Is_Flipped();
        this.UpdateWheelGraphics(relativeVelocity);
        this.UpdateGear(relativeVelocity);
    }

    public virtual void FixedUpdate()
    {
         // The rigidbody velocity is always given in world space, but in order to work in local space of the car model we need to transform it first.
        Vector3 relativeVelocity = this.transform.InverseTransformDirection(this.GetComponent<Rigidbody>().velocity);
        this.CalculateState();
        this.UpdateFriction(relativeVelocity);
        this.UpdateDrag(relativeVelocity);
        this.CalculateEnginePower(relativeVelocity);
        this.ApplyThrottle(this.canDrive, relativeVelocity);
        this.ApplySteering(this.canSteer, relativeVelocity);
    }

    /**************************************************/
    /* Functions called from Start()                  */
    /**************************************************/
    public virtual void SetupWheelColliders()
    {
        this.SetupWheelFrictionCurve();
        int wheelCount = 0;
        foreach (Transform t in this.frontWheels)
        {
            this.wheels[wheelCount] = this.SetupWheel(t, true);
            wheelCount++;
        }
        foreach (Transform t in this.rearWheels)
        {
            this.wheels[wheelCount] = this.SetupWheel(t, false);
            wheelCount++;
        }
    }

    public virtual void SetupWheelFrictionCurve()
    {
        this.wfc = default(WheelFrictionCurve);
        this.wfc.extremumSlip = 1;
        this.wfc.extremumValue = 50;
        this.wfc.asymptoteSlip = 2;
        this.wfc.asymptoteValue = 25;
        this.wfc.stiffness = 1;
    }

    public virtual Wheel SetupWheel(Transform wheelTransform, bool isFrontWheel)
    {
        GameObject go = new GameObject(wheelTransform.name + " Collider");
        go.transform.position = wheelTransform.position;
        go.transform.parent = this.transform;
        go.transform.rotation = wheelTransform.rotation;
        WheelCollider wc = ((WheelCollider) go.AddComponent(typeof(WheelCollider))) as WheelCollider;
        wc.suspensionDistance = this.suspensionRange;
        JointSpring js = wc.suspensionSpring;
        if (isFrontWheel)
        {
            js.spring = this.suspensionSpringFront;
        }
        else
        {
            js.spring = this.suspensionSpringRear;
        }
        js.damper = this.suspensionDamper;
        wc.suspensionSpring = js;
        wheel = new Wheel();
        wheel.collider = wc;
        wc.sidewaysFriction = this.wfc;
        wheel.wheelGraphic = wheelTransform;
        wheel.tireGraphic = wheelTransform.GetComponentsInChildren(typeof(Transform))[1];
        this.wheelRadius = wheel.tireGraphic.GetComponent<Renderer>().bounds.size.y / 2;
        wheel.collider.radius = this.wheelRadius;
        if (isFrontWheel)
        {
            wheel.steerWheel = true;
            go = new GameObject(wheelTransform.name + " Steer Column");
            go.transform.position = wheelTransform.position;
            go.transform.rotation = wheelTransform.rotation;
            go.transform.parent = this.transform;
            wheelTransform.parent = go.transform;
        }
        else
        {
            wheel.driveWheel = true;
        }
        return wheel;
    }

    public virtual void SetupCenterOfMass()
    {
        if (this.centerOfMass != null)
        {
            this.GetComponent<Rigidbody>().centerOfMass = this.centerOfMass.localPosition;
        }
    }

    public virtual void SetupGears()
    {
        this.engineForceValues = new float[this.numberOfGears];
        this.gearSpeeds = new float[this.numberOfGears];
        float tempTopSpeed = this.topSpeed;
        int i = 0;
        while (i < this.numberOfGears)
        {
            if (i > 0)
            {
                this.gearSpeeds[i] = (tempTopSpeed / 4) + this.gearSpeeds[i - 1];
            }
            else
            {
                this.gearSpeeds[i] = tempTopSpeed / 4;
            }
            tempTopSpeed = tempTopSpeed - (tempTopSpeed / 4);
            i++;
        }
        float engineFactor = this.topSpeed / this.gearSpeeds[this.gearSpeeds.Length - 1];
        i = 0;
        while (i < this.numberOfGears)
        {
            float maxLinearDrag = this.gearSpeeds[i] * this.gearSpeeds[i];// * dragMultiplier.z;
            this.engineForceValues[i] = maxLinearDrag * engineFactor;
            i++;
        }
    }

    public virtual void SetUpSkidmarks()
    {
        if ((Skidmarks) UnityEngine.Object.FindObjectOfType(typeof(Skidmarks)))
        {
            this.skidmarks = (Skidmarks) UnityEngine.Object.FindObjectOfType(typeof(Skidmarks));
            this.skidSmoke = (ParticleEmitter) this.skidmarks.GetComponentInChildren(typeof(ParticleEmitter));
        }
        else
        {
            Debug.Log("No skidmarks object found. Skidmarks will not be drawn");
        }
        this.skidmarkTime = new float[4];
        foreach (float f in this.skidmarkTime)
        {
            f = 0f;
        }
    }

    /**************************************************/
    /* Functions called from Update()                 */
    /**************************************************/
    public virtual void GetInput()
    {
        this.throttle = Input.GetAxis("Vertical");
        this.steer = Input.GetAxis("Horizontal");
        if (this.throttle < 0f)
        {
            this.brakeLights.SetFloat("_Intensity", Mathf.Abs(this.throttle));
        }
        else
        {
            this.brakeLights.SetFloat("_Intensity", 0f);
        }
        this.CheckHandbrake();
    }

    public virtual void CheckHandbrake()
    {
        if (Input.GetKey("space"))
        {
            if (!this.handbrake)
            {
                this.handbrake = true;
                this.handbrakeTime = Time.time;
                this.dragMultiplier.x = this.initialDragMultiplierX * this.handbrakeXDragFactor;
            }
        }
        else
        {
            if (this.handbrake)
            {
                this.handbrake = false;
                this.StartCoroutine(this.StopHandbraking(Mathf.Min(5, Time.time - this.handbrakeTime)));
            }
        }
    }

    public virtual IEnumerator StopHandbraking(float seconds)
    {
        float diff = this.initialDragMultiplierX - this.dragMultiplier.x;
        this.handbrakeTimer = 1;
        // Get the x value of the dragMultiplier back to its initial value in the specified time.
        while ((this.dragMultiplier.x < this.initialDragMultiplierX) && !this.handbrake)
        {
            this.dragMultiplier.x = this.dragMultiplier.x + (diff * (Time.deltaTime / seconds));
            this.handbrakeTimer = this.handbrakeTimer - (Time.deltaTime / seconds);
            yield return null;
        }
        this.dragMultiplier.x = this.initialDragMultiplierX;
        this.handbrakeTimer = 0;
    }

    public virtual void Check_If_Car_Is_Flipped()
    {
        if ((this.transform.localEulerAngles.z > 80) && (this.transform.localEulerAngles.z < 280))
        {
            this.resetTimer = this.resetTimer + Time.deltaTime;
        }
        else
        {
            this.resetTimer = 0;
        }
        if (this.resetTimer > this.resetTime)
        {
            this.FlipCar();
        }
    }

    public virtual void FlipCar()
    {
        this.transform.rotation = Quaternion.LookRotation(this.transform.forward);
        this.transform.position = this.transform.position + (Vector3.up * 0.5f);
        this.GetComponent<Rigidbody>().velocity = Vector3.zero;
        this.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        this.resetTimer = 0;
        this.currentEnginePower = 0;
    }

    public float wheelCount;
    public virtual void UpdateWheelGraphics(Vector3 relativeVelocity)
    {
        this.wheelCount = -1;
        foreach (Wheel w in this.wheels)
        {
            this.wheelCount++;
            WheelCollider wheel = w.collider;
            WheelHit wh = default(WheelHit);
            // First we get the velocity at the point where the wheel meets the ground, if the wheel is touching the ground
            if (wheel.GetGroundHit(out wh))
            {
                w.wheelGraphic.localPosition = wheel.transform.up * (this.wheelRadius + wheel.transform.InverseTransformPoint(wh.point).y);
                w.wheelVelo = this.GetComponent<Rigidbody>().GetPointVelocity(wh.point);
                w.groundSpeed = w.wheelGraphic.InverseTransformDirection(w.wheelVelo);
                // Code to handle skidmark drawing. Not covered in the tutorial
                if (this.skidmarks)
                {
                    if ((this.skidmarkTime[this.wheelCount] < 0.02f) && (w.lastSkidmark != -1))
                    {
                        this.skidmarkTime[this.wheelCount] = this.skidmarkTime[this.wheelCount] + Time.deltaTime;
                    }
                    else
                    {
                        float dt = this.skidmarkTime[this.wheelCount] == 0f ? Time.deltaTime : this.skidmarkTime[this.wheelCount];
                        this.skidmarkTime[this.wheelCount] = 0f;
                        float handbrakeSkidding = this.handbrake && w.driveWheel ? w.wheelVelo.magnitude * 0.3f : 0;
                        float skidGroundSpeed = Mathf.Abs(w.groundSpeed.x) - 15;
                        if ((skidGroundSpeed > 0) || (handbrakeSkidding > 0))
                        {
                            Vector3 staticVel = ((object) this.transform.TransformDirection(this.skidSmoke.localVelocity)) + this.skidSmoke.worldVelocity;
                            if (w.lastSkidmark != -1)
                            {
                                float emission = (float) UnityEngine.Random.Range(this.skidSmoke.minEmission, this.skidSmoke.maxEmission);
                                float lastParticleCount = w.lastEmitTime * emission;
                                float currentParticleCount = Time.time * emission;
                                int noOfParticles = Mathf.CeilToInt(currentParticleCount) - Mathf.CeilToInt(lastParticleCount);
                                int lastParticle = Mathf.CeilToInt(lastParticleCount);
                                int i = 0;
                                while (i <= noOfParticles)
                                {
                                    float particleTime = Mathf.InverseLerp(lastParticleCount, currentParticleCount, lastParticle + i);
                                    this.skidSmoke.Emit(Vector3.Lerp(w.lastEmitPosition, wh.point, particleTime) + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f)), staticVel + (w.wheelVelo * 0.05f), ((float) Random.Range(this.skidSmoke.minSize, this.skidSmoke.maxSize)) * Mathf.Clamp(skidGroundSpeed * 0.1f, 0.5f, 1), Random.Range(this.skidSmoke.minEnergy, this.skidSmoke.maxEnergy), Color.white);
                                    i++;
                                }
                            }
                            else
                            {
                                this.skidSmoke.Emit(wh.point + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f)), staticVel + (w.wheelVelo * 0.05f), ((float) Random.Range(this.skidSmoke.minSize, this.skidSmoke.maxSize)) * Mathf.Clamp(skidGroundSpeed * 0.1f, 0.5f, 1), Random.Range(this.skidSmoke.minEnergy, this.skidSmoke.maxEnergy), Color.white);
                            }
                            w.lastEmitPosition = wh.point;
                            w.lastEmitTime = Time.time;
                            w.lastSkidmark = this.skidmarks.AddSkidMark(wh.point + (this.GetComponent<Rigidbody>().velocity * dt), wh.normal, ((skidGroundSpeed * 0.1f) + handbrakeSkidding) * Mathf.Clamp01(wh.force / wheel.suspensionSpring.spring), w.lastSkidmark);
                            this.sound.Skid(true, Mathf.Clamp01(skidGroundSpeed * 0.1f));
                        }
                        else
                        {
                            w.lastSkidmark = -1;
                            this.sound.Skid(false, 0);
                        }
                    }
                }
            }
            else
            {
                 // If the wheel is not touching the ground we set the position of the wheel graphics to
                 // the wheel's transform position + the range of the suspension.
                w.wheelGraphic.position = wheel.transform.position + (-wheel.transform.up * this.suspensionRange);
                if (w.steerWheel)
                {
                    w.wheelVelo = w.wheelVelo * 0.9f;
                }
                else
                {
                    w.wheelVelo = w.wheelVelo * (0.9f * (1 - this.throttle));
                }
                if (this.skidmarks)
                {
                    w.lastSkidmark = -1;
                    this.sound.Skid(false, 0);
                }
            }
            // If the wheel is a steer wheel we apply two rotations:
            // *Rotation around the Steer Column (visualizes the steer direction)
            // *Rotation that visualizes the speed
            if (w.steerWheel)
            {
                Vector3 ea = w.wheelGraphic.parent.localEulerAngles;
                ea.y = this.steer * this.maximumTurn;
                w.wheelGraphic.parent.localEulerAngles = ea;
                w.tireGraphic.Rotate(((Vector3.right * (w.groundSpeed.z / this.wheelRadius)) * Time.deltaTime) * Mathf.Rad2Deg);
            }
            else
            {
                if (!this.handbrake && w.driveWheel)
                {
                     // If the wheel is a drive wheel it only gets the rotation that visualizes speed.
                     // If we are hand braking we don't rotate it.
                    w.tireGraphic.Rotate(((Vector3.right * (w.groundSpeed.z / this.wheelRadius)) * Time.deltaTime) * Mathf.Rad2Deg);
                }
            }
        }
    }

    public virtual void UpdateGear(Vector3 relativeVelocity)
    {
        this.currentGear = 0;
        int i = 0;
        while (i < (this.numberOfGears - 1))
        {
            if (relativeVelocity.z > this.gearSpeeds[i])
            {
                this.currentGear = i + 1;
            }
            i++;
        }
    }

    /**************************************************/
    /* Functions called from FixedUpdate()            */
    /**************************************************/
    public virtual void UpdateDrag(Vector3 relativeVelocity)
    {
        Vector3 relativeDrag = new Vector3(-relativeVelocity.x * Mathf.Abs(relativeVelocity.x), -relativeVelocity.y * Mathf.Abs(relativeVelocity.y), -relativeVelocity.z * Mathf.Abs(relativeVelocity.z));
        Vector3 drag = Vector3.Scale(this.dragMultiplier, relativeDrag);
        if (this.initialDragMultiplierX > this.dragMultiplier.x) // Handbrake code
        {
            drag.x = drag.x / (relativeVelocity.magnitude / (this.topSpeed / (1 + (2 * this.handbrakeXDragFactor))));
            drag.z = drag.z * (1 + Mathf.Abs(Vector3.Dot(this.GetComponent<Rigidbody>().velocity.normalized, this.transform.forward)));
            drag = drag + (this.GetComponent<Rigidbody>().velocity * Mathf.Clamp01(this.GetComponent<Rigidbody>().velocity.magnitude / this.topSpeed));
        }
        else
        {
             // No handbrake
            drag.x = drag.x * (this.topSpeed / relativeVelocity.magnitude);
        }
        if ((Mathf.Abs(relativeVelocity.x) < 5) && !this.handbrake)
        {
            drag.x = -relativeVelocity.x * this.dragMultiplier.x;
        }
        this.GetComponent<Rigidbody>().AddForce((this.transform.TransformDirection(drag) * this.GetComponent<Rigidbody>().mass) * Time.deltaTime);
    }

    public virtual void UpdateFriction(Vector3 relativeVelocity)
    {
        float sqrVel = relativeVelocity.x * relativeVelocity.x;
        // Add extra sideways friction based on the car's turning velocity to avoid slipping
        this.wfc.extremumValue = Mathf.Clamp(300 - sqrVel, 0, 300);
        this.wfc.asymptoteValue = Mathf.Clamp(150 - (sqrVel / 2), 0, 150);
        foreach (Wheel w in this.wheels)
        {
            w.collider.sidewaysFriction = this.wfc;
            w.collider.forwardFriction = this.wfc;
        }
    }

    public virtual void CalculateEnginePower(Vector3 relativeVelocity)
    {
        if (this.throttle == 0)
        {
            this.currentEnginePower = this.currentEnginePower - (Time.deltaTime * 200);
        }
        else
        {
            if (this.HaveTheSameSign(relativeVelocity.z, this.throttle))
            {
                normPower = (this.currentEnginePower / this.engineForceValues[this.engineForceValues.Length - 1]) * 2;
                this.currentEnginePower = this.currentEnginePower + ((Time.deltaTime * 200) * this.EvaluateNormPower(normPower));
            }
            else
            {
                this.currentEnginePower = this.currentEnginePower - (Time.deltaTime * 300);
            }
        }
        if (this.currentGear == 0)
        {
            this.currentEnginePower = Mathf.Clamp(this.currentEnginePower, 0, this.engineForceValues[0]);
        }
        else
        {
            this.currentEnginePower = Mathf.Clamp(this.currentEnginePower, this.engineForceValues[this.currentGear - 1], this.engineForceValues[this.currentGear]);
        }
    }

    public virtual void CalculateState()
    {
        this.canDrive = false;
        this.canSteer = false;
        foreach (Wheel w in this.wheels)
        {
            if (w.collider.isGrounded)
            {
                if (w.steerWheel)
                {
                    this.canSteer = true;
                }
                if (w.driveWheel)
                {
                    this.canDrive = true;
                }
            }
        }
    }

    public virtual void ApplyThrottle(bool canDrive, Vector3 relativeVelocity)
    {
        if (canDrive)
        {
            float throttleForce = 0;
            float brakeForce = 0;
            if (this.HaveTheSameSign(relativeVelocity.z, this.throttle))
            {
                if (!this.handbrake)
                {
                    throttleForce = (Mathf.Sign(this.throttle) * this.currentEnginePower) * this.GetComponent<Rigidbody>().mass;
                }
            }
            else
            {
                brakeForce = (Mathf.Sign(this.throttle) * this.engineForceValues[0]) * this.GetComponent<Rigidbody>().mass;
            }
            this.GetComponent<Rigidbody>().AddForce((this.transform.forward * Time.deltaTime) * (throttleForce + brakeForce));
        }
    }

    public virtual void ApplySteering(bool canSteer, Vector3 relativeVelocity)
    {
        if (canSteer)
        {
            float turnRadius = 3f / Mathf.Sin((90 - (this.steer * 30)) * Mathf.Deg2Rad);
            float minMaxTurn = this.EvaluateSpeedToTurn(this.GetComponent<Rigidbody>().velocity.magnitude);
            float turnSpeed = Mathf.Clamp(relativeVelocity.z / turnRadius, -minMaxTurn / 10, minMaxTurn / 10);
            this.transform.RotateAround(this.transform.position + ((this.transform.right * turnRadius) * this.steer), this.transform.up, ((turnSpeed * Mathf.Rad2Deg) * Time.deltaTime) * this.steer);
            Vector3 debugStartPoint = this.transform.position + ((this.transform.right * turnRadius) * this.steer);
            Vector3 debugEndPoint = debugStartPoint + (Vector3.up * 5);
            Debug.DrawLine(debugStartPoint, debugEndPoint, Color.red);
            if (this.initialDragMultiplierX > this.dragMultiplier.x) // Handbrake
            {
                float rotationDirection = Mathf.Sign(this.steer); // rotationDirection is -1 or 1 by default, depending on steering
                if (this.steer == 0)
                {
                    if (this.GetComponent<Rigidbody>().angularVelocity.y < 1) // If we are not steering and we are handbraking and not rotating fast, we apply a random rotationDirection
                    {
                        rotationDirection = Random.Range(-1f, 1f);
                    }
                    else
                    {
                        rotationDirection = this.GetComponent<Rigidbody>().angularVelocity.y; // If we are rotating fast we are applying that rotation to the car
                    }
                }
                // -- Finally we apply this rotation around a point between the cars front wheels.
                this.transform.RotateAround(this.transform.TransformPoint((this.frontWheels[0].localPosition + this.frontWheels[1].localPosition) * 0.5f), this.transform.up, (((this.GetComponent<Rigidbody>().velocity.magnitude * Mathf.Clamp01(1 - (this.GetComponent<Rigidbody>().velocity.magnitude / this.topSpeed))) * rotationDirection) * Time.deltaTime) * 2);
            }
        }
    }

    /**************************************************/
    /*               Utility Functions                */
    /**************************************************/
    public virtual float Convert_Miles_Per_Hour_To_Meters_Per_Second(float value)
    {
        return value * 0.44704f;
    }

    public virtual float Convert_Meters_Per_Second_To_Miles_Per_Hour(float value)
    {
        return value * 2.23693629f;
    }

    public virtual bool HaveTheSameSign(float first, float second)
    {
        if (Mathf.Sign(first) == Mathf.Sign(second))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public virtual float EvaluateSpeedToTurn(float speed)
    {
        if (speed > (this.topSpeed / 2))
        {
            return this.minimumTurn;
        }
        float speedIndex = 1 - (speed / (this.topSpeed / 2));
        return this.minimumTurn + (speedIndex * (this.maximumTurn - this.minimumTurn));
    }

    public virtual float EvaluateNormPower(float normPower)
    {
        if (normPower < 1)
        {
            return 10 - (normPower * 9);
        }
        else
        {
            return 1.9f - (normPower * 0.9f);
        }
    }

    public virtual float GetGearState()
    {
        Vector3 relativeVelocity = this.transform.InverseTransformDirection(this.GetComponent<Rigidbody>().velocity);
        float lowLimit = this.currentGear == 0 ? 0 : this.gearSpeeds[this.currentGear - 1];
        return (((relativeVelocity.z - lowLimit) / this.gearSpeeds[this.currentGear - lowLimit]) * (1 - (this.currentGear * 0.1f))) + (this.currentGear * 0.1f);
    }

    public Car()
    {
        this.wheelRadius = 0.4f;
        this.suspensionRange = 0.1f;
        this.suspensionDamper = 50;
        this.suspensionSpringFront = 18500;
        this.suspensionSpringRear = 9000;
        this.dragMultiplier = new Vector3(2, 5, 1);
        this.topSpeed = 160;
        this.numberOfGears = 5;
        this.maximumTurn = 15;
        this.minimumTurn = 10;
        this.resetTime = 5f;
        this.handbrakeXDragFactor = 0.5f;
        this.initialDragMultiplierX = 10f;
        this.handbrakeTimer = 1f;
    }

}