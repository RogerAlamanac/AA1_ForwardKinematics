using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyRobotController : MonoBehaviour
{
    // --- Joints (esferes) ---
    public GameObject joint1Sphere;
    public GameObject joint2Sphere;
    public GameObject joint3Sphere;

    // --- Longitud dels segments ---
    public float segment1Length = 2f;
    public float segment2Length = 1.5f;
    public float segment3Length = 1f;

    // --- Angles de rotació (en graus) ---
    private float joint1AngleY = 0f; // rotació al voltant de l'eix Y
    private float joint2AngleZ = 0f; // rotació al voltant de l'eix Z
    private float joint3AngleZ = 0f; // rotació del canell

    public float rotationSpeed = 50f;

    void Update()
    {
        // Exemple d'input per canviar angles
        joint1AngleY += Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;
        joint2AngleZ += Input.GetAxis("Vertical") * rotationSpeed * Time.deltaTime;
       // joint3AngleZ += (Input.GetKey(KeyCode.Q) ? 1 : 0 - Input.GetKey(KeyCode.E) ? 1 : 0) * rotationSpeed * Time.deltaTime;

        // Calcular posicions de cada joint
        Vector3 joint1Pos = transform.position; // base fixa
        Vector3 joint2Pos = joint1Pos + Quaternion.Euler(0, joint1AngleY, 0) * Vector3.forward * segment1Length;
        Vector3 joint3Pos = joint2Pos + Quaternion.Euler(0, joint1AngleY, joint2AngleZ) * Vector3.forward * segment2Length;
        Vector3 endEffectorPos = joint3Pos + Quaternion.Euler(0, joint1AngleY, joint2AngleZ + joint3AngleZ) * Vector3.forward * segment3Length;

        // Aplicar posicions a les esferes
        joint1Sphere.transform.position = joint1Pos;
        joint2Sphere.transform.position = joint2Pos;
        joint3Sphere.transform.position = joint3Pos;

        // Opcional: si vols una esfera final per agafar objectes
        joint3Sphere.transform.position = endEffectorPos;
    }
}
