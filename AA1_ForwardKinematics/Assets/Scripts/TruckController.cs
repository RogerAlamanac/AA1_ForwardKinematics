using UnityEngine;

public class TruckController : MonoBehaviour
{
    // --- Cos de la camioneta (objecte principal) ---
    public GameObject truckBody;

    // --- Rodes independents ---
    public GameObject frontLeftWheel;
    public GameObject frontRightWheel;
    public GameObject rearLeftWheel;
    public GameObject rearRightWheel;

    // --- Offset de cada roda respecte el cos ---
    public Vector3 frontLeftOffset;
    public Vector3 frontRightOffset;
    public Vector3 rearLeftOffset;
    public Vector3 rearRightOffset;

    // --- Paràmetres de moviment ---
    public float maxSpeed = 8f;
    public float acceleration = 4f;
    public float rotationSpeed = 50f;
    public float wheelRadius = 0.5f;
    public float steerAngleMax = 25f;
    public float steerSmooth = 5f;

    // --- Estat intern ---
    private float currentSpeed = 0f;
    private float currentSteer = 0f;
    private float currentSteerVisual = 0f;

    void Update()
    {
        HandleInput();
        UpdateBody();
        UpdateWheels();
    }

    void HandleInput()
    {
        float moveInput = Input.GetAxis("Vertical");
        float steerInput = Input.GetAxis("Horizontal");

        // Acceleració suau (usant la teva LerpLib)
        float targetSpeed = moveInput * maxSpeed;
        currentSpeed = LerpLib.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        // Control de direcció suau
        float targetSteer = steerInput * steerAngleMax;
        currentSteer = LerpLib.Lerp(currentSteer, targetSteer, steerSmooth * Time.deltaTime);
    }

    void UpdateBody()
    {
        // --- Mou el cos ---
        Vector3 forward = truckBody.transform.forward;
        Vector3 moveDelta = forward * currentSpeed * Time.deltaTime;
        truckBody.transform.position += moveDelta;

        // --- Rota el cos ---
        if (System.Math.Abs(currentSpeed) > 0.1f)
        {
            float turnAmount = (currentSteer / steerAngleMax) * rotationSpeed * (currentSpeed >= 0 ? 1f : -1f) * Time.deltaTime;
            Quaternion qTurn = QuaternionLib.DesDeEixAngle(Vector3.up, turnAmount);
            truckBody.transform.rotation = QuaternionLib.Producte(qTurn, truckBody.transform.rotation);
        }
    }

    void UpdateWheels()
    {
        // --- Calcular quant gira cada roda segons la distància recorreguda ---
        float wheelSpinAngle = (currentSpeed * Time.deltaTime / (2f * LerpLib.PI * wheelRadius)) * 360f;
        Quaternion qSpin = QuaternionLib.DesDeEixAngle(Vector3.right, wheelSpinAngle);

        // --- Gir visual de direcció ---
        currentSteerVisual = LerpLib.Lerp(currentSteerVisual, currentSteer, 8f * Time.deltaTime);
        Quaternion qSteer = QuaternionLib.DesDeEixAngle(Vector3.up, currentSteerVisual);

        // --- Posicionar i rotar cada roda manualment ---
        UpdateWheel(frontLeftWheel, frontLeftOffset, qSteer, qSpin, true);
        UpdateWheel(frontRightWheel, frontRightOffset, qSteer, qSpin, true);
        UpdateWheel(rearLeftWheel, rearLeftOffset, Quaternion.identity, qSpin, false);
        UpdateWheel(rearRightWheel, rearRightOffset, Quaternion.identity, qSpin, false);
    }

    void UpdateWheel(GameObject wheel, Vector3 offsetLocal, Quaternion steerRot, Quaternion spinRot, bool isFront)
    {
        if (wheel == null) return;

        // --- Calcular posició global ---
        Vector3 worldOffset = QuaternionLib.RotarVector(truckBody.transform.rotation, offsetLocal);
        wheel.transform.position = truckBody.transform.position + worldOffset;

        // --- Calcular rotació global ---
        Quaternion baseRot = truckBody.transform.rotation;
        Quaternion totalRot;

        if (isFront)
            totalRot = QuaternionLib.Producte(baseRot, QuaternionLib.Producte(steerRot, spinRot));
        else
            totalRot = QuaternionLib.Producte(baseRot, spinRot);

        wheel.transform.rotation = totalRot;
    }
}
