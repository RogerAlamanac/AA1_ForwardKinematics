using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagneticGripper : MonoBehaviour
{
    [Header("Magnet mount (child del end-effector)")]
    public Transform magnetPoint;

    [Header("Targets")]
    public LayerMask grabbableMask;          // Capas afectadas
    public float attractionRange = 3.0f;     // Distancia máxima de atracción
    public bool requireLineOfSight = true;   // Evitar atraer a través de obstáculos

    [Header("Modelo de fuerza (usa librerías del proyecto)")]
    [Tooltip("Aceleración máxima (m/s^2) cuando dist=0. Acelera independiente de la masa.")]
    public float maxAcceleration = 60f;

    [Tooltip("Curva de caída con la distancia: u = 1 - SmootherStep01(dist/range)")]
    public bool useSmootherStep = true;

    [Tooltip("Amortiguación cerca del imán para evitar oscilaciones (0..1)")]
    [Range(0f, 1f)] public float nearDamping = 0.5f;

    [Header("Snap opcional")]
    public bool enableAutoSnap = true;
    public float snapDistance = 0.08f;
    public bool makeKinematicOnSnap = true;

    [Header("Input")]
    public KeyCode attractKey = KeyCode.Space;

    void Reset()
    {
        // Auto-asigna hijo "Gripper" si existe; si no, usa este transform
        var t = transform.Find("Gripper");
        magnetPoint = t ? t : transform;
    }

    void Update()
    {
        if (!magnetPoint) return;
        if (Input.GetKey(attractKey))
            AttractNearby();
    }

    void AttractNearby()
    {
        // Buscar candidatos en rango
        Collider[] hits = Physics.OverlapSphere(
            magnetPoint.position,
            attractionRange,
            grabbableMask,
            QueryTriggerInteraction.Collide // incluir triggers si los hubiera
        );
        if (hits == null || hits.Length == 0) return;

        foreach (var col in hits)
        {
            var rb = col.attachedRigidbody;
            if (!rb) continue; // sólo cuerpos con física

            // Evitar auto-atraer partes del propio robot
            if (rb.transform.IsChildOf(transform)) continue;

            Vector3 toMagnet = magnetPoint.position - rb.worldCenterOfMass;
            float dist = toMagnet.magnitude;
            if (dist < VectorLib.EPSILON) continue; // usar epsilon de la librería

            // Línea de visión opcional
            if (requireLineOfSight)
            {
                RaycastHit rh;
                if (Physics.Raycast(rb.worldCenterOfMass, toMagnet / dist, out rh, dist, ~0, QueryTriggerInteraction.Ignore))
                {
                    // Si el primer choque no es el propio objeto ni parte del imán, se cancela
                    if (rh.collider.transform != col.transform && !rh.collider.transform.IsChildOf(transform))
                        continue;
                }
            }

            // t = dist / range -> 0..1; u = 1 - suavizado(t)
            float t = dist / (attractionRange <= 0f ? 1f : attractionRange);
            float u = 1f - (useSmootherStep ? LerpLib.SmootherStep01(t) : LerpLib.SmoothStep01(t));
            u = LerpLib.Retallar01(u); // clamp via librería

            float accel = maxAcceleration * u; // m/s^2
            Vector3 dir = toMagnet / dist;     // unitario

            rb.AddForce(dir * accel, ForceMode.Acceleration);

            // Amortiguación cerca del imán (sin Mathf)
            if (dist < snapDistance * 2f && nearDamping > 0f)
            {
                float damp = 1f - LerpLib.Retallar01(nearDamping * Time.deltaTime * 10f);
                rb.velocity *= damp;
                rb.angularVelocity *= damp;
            }

            // Auto-snap opcional
            if (enableAutoSnap && dist <= snapDistance)
            {
                Transform tr = rb.transform;
                tr.SetParent(magnetPoint, worldPositionStays: false);
                tr.localPosition = Vector3.zero;
                tr.localRotation = Quaternion.identity;
                if (makeKinematicOnSnap && rb) rb.isKinematic = true;
            }
        }
    }
}