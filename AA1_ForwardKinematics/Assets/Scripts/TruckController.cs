using System;
using UnityEngine;

public class TruckController : MonoBehaviour
{
	public GameObject truckBody;

	public GameObject frontLeftWheel;
	public GameObject frontRightWheel;
	public GameObject rearLeftWheel;
	public GameObject rearRightWheel;

	public Vector3 frontLeftOffset;
	public Vector3 frontRightOffset;
	public Vector3 rearLeftOffset;
	public Vector3 rearRightOffset;

	[Header("Movimiento")]
	public float maxSpeed = 8f;
	public float acceleration = 4f;
	public float rotationSpeed = 50f;  
	public float wheelRadius = 0.5f;
	public float steerAngleMax = 25f;   
	public float steerSmooth = 5f;      
	public float speedDeadzone = 0.02f; 

	float currentSpeed = 0f;        
	float currentSteer = 0f;           
	float currentSteerVisual = 0f;     


	float moveInputRaw = 0f;            
	float steerInputRaw = 0f;          

	float spinFL = 0f, spinFR = 0f, spinRL = 0f, spinRR = 0f;


	void Start()
	{
		frontLeftOffset = GetLocalOffset(frontLeftWheel);
		frontRightOffset = GetLocalOffset(frontRightWheel);
		rearLeftOffset = GetLocalOffset(rearLeftWheel);
		rearRightOffset = GetLocalOffset(rearRightWheel);
    }

	void Update()
	{
		HandleInput();
		UpdateBody();
		UpdateWheels();
	}

	void HandleInput()
	{
		moveInputRaw = 0f;
		steerInputRaw = 0f;

		if (Input.GetKey(KeyCode.W)) moveInputRaw = 1f;
		else if (Input.GetKey(KeyCode.S)) moveInputRaw = -1f;

		if (Input.GetKey(KeyCode.D)) steerInputRaw = 1f;
		else if (Input.GetKey(KeyCode.A)) steerInputRaw = -1f;

		// Velocidad objetivo y lerp suave
		float targetSpeed = moveInputRaw * maxSpeed;
		currentSpeed = LerpLib.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

		if (moveInputRaw == 0f && System.Math.Abs(currentSpeed) < 0.05f)
			currentSpeed = 0f;

		// Dirección objetivo y lerp suave
		float targetSteer = steerInputRaw * steerAngleMax;
		currentSteer = LerpLib.Lerp(currentSteer, targetSteer, steerSmooth * Time.deltaTime);
	}

	void UpdateBody()
	{
		if (!truckBody) return;

		// SOLO avanzamos si damos gas (W/S)
		if (moveInputRaw != 0f)
		{
			Vector3 forward = truckBody.transform.forward;
			truckBody.transform.position += forward * currentSpeed * Time.deltaTime;
		}

		// SOLO giramos la carrocería si hay gas 
		if (moveInputRaw != 0f && Math.Abs(currentSpeed) > 0f)
		{
			float dirSign = currentSpeed >= 0f ? 1f : -1f;
			float turnAmount = (currentSteer / steerAngleMax) * rotationSpeed * dirSign * Time.deltaTime;

			Quaternion qTurn = QuaternionLib.DesDeEixAngle(Vector3.up, turnAmount);
			truckBody.transform.rotation = QuaternionLib.Producte(qTurn, truckBody.transform.rotation);
		}
	}

	void UpdateWheels()
	{
		if (!truckBody) return;

		float spinDeltaDeg = 0f;
		if (wheelRadius > 1e-6f)
			spinDeltaDeg = (currentSpeed * Time.deltaTime / (2f * LerpLib.PI * wheelRadius)) * 360f;

		spinFL += spinDeltaDeg;
		spinFR += spinDeltaDeg;
		spinRL += spinDeltaDeg;
		spinRR += spinDeltaDeg;
		currentSteerVisual = LerpLib.Lerp(currentSteerVisual, currentSteer, 8f * Time.deltaTime);
		Quaternion qSteer = QuaternionLib.DesDeEixAngle(Vector3.up, currentSteerVisual);

		UpdateWheel(frontLeftWheel, frontLeftOffset, qSteer, spinFL, true);
		UpdateWheel(frontRightWheel, frontRightOffset, qSteer, spinFR, true);
		UpdateWheel(rearLeftWheel, rearLeftOffset, Quaternion.identity, spinRL, false);
		UpdateWheel(rearRightWheel, rearRightOffset, Quaternion.identity, spinRR, false);
	}

	void UpdateWheel(GameObject wheel, Vector3 offsetLocal, Quaternion steerRot, float spinAngleDeg, bool isFront)
	{
		if (!wheel) return;

		Quaternion bodyRot = truckBody.transform.rotation;

		Vector3 worldOffset = QuaternionLib.RotarVector(bodyRot, offsetLocal);
		wheel.transform.position = truckBody.transform.position + worldOffset;

		Quaternion steer = isFront ? steerRot : Quaternion.identity;
		Quaternion spinLocalX = QuaternionLib.DesDeEixAngle(Vector3.right, spinAngleDeg);

		Quaternion totalRot = QuaternionLib.Producte(bodyRot, QuaternionLib.Producte(steer, spinLocalX));
		wheel.transform.rotation = totalRot;
	}

	Vector3 GetLocalOffset(GameObject wheel)
	{
		if (!truckBody || !wheel) return Vector3.zero;

		Quaternion invBodyRot = QuaternionLib.Inversa(truckBody.transform.rotation);
		Vector3 delta = wheel.transform.position - truckBody.transform.position;
		return QuaternionLib.RotarVector(invBodyRot, delta);
	}
}