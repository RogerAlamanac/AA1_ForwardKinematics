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

    [Header("Snap / bloqueo")]
    [Tooltip("Solo como respaldo; con colisión ya no hace falta un umbral pequeño")]
    public float snapDistance = 0.08f;

    [Tooltip("Si estaba no cinemático, al agarrar se fuerza isKinematic=true y se restaura al soltar")]
    public bool forceKinematicWhileGrabbed = true;

    [Header("Snap por colisión")]
    [Tooltip("Si un objeto en grabbableMask COLISIONA con el gripper, se bloquea")]
    public bool snapOnContact = true;

    [Tooltip("Exige mantener la tecla para que el snap por contacto ocurra")]
    public bool requireKeyForSnapOnContact = true;

    [Header("Input")]
    public KeyCode attractKey = KeyCode.Space;

    // --- Estado de agarre ---
    Rigidbody grabbedRb = null;
    bool prevKinematic = false;
    Transform prevParent = null;

    void Reset()
    {
        var t = transform.Find("Gripper");
        magnetPoint = t ? t : transform;
    }

    void Update()
    {
        if (!magnetPoint) return;

        // Pulsada: atraer o mantener bloqueado
        if (Input.GetKey(attractKey))
        {
            if (grabbedRb == null)
            {
                // Aún no hay objeto agarrado: atrae; si entra en snap, bloqueará por contacto
                AttractNearbyAndMaybeGrab();
            }
            else
            {
                // Ya hay objeto agarrado: mantenerlo fijo en el magnetPoint
                MaintainGrabbedTransform();
            }
        }

        // Soltada: liberar si hay algo agarrado
        if (Input.GetKeyUp(attractKey))
        {
            if (grabbedRb != null) ReleaseGrabbed();
        }
    }

    // --- SNAP POR CONTACTO ---
    // Usa OnTriggerEnter o OnCollisionEnter según como tengas configurado el collider del gripper.

    void OnTriggerEnter(Collider other)
    {
        TrySnapOnContact(other.attachedRigidbody);
    }

    void OnCollisionEnter(Collision collision)
    {
        TrySnapOnContact(collision.rigidbody);
    }

    void TrySnapOnContact(Rigidbody rb)
    {
        if (!snapOnContact) return;
        if (grabbedRb != null) return; // ya hay uno
        if (!rb) return;
        if (rb.transform.IsChildOf(transform)) return; // evitar agarrar partes propias
        if ((grabbableMask.value & (1 << rb.gameObject.layer)) == 0) return; // no está en la máscara

        // ¿Exigimos tecla?
        if (requireKeyForSnapOnContact && !Input.GetKey(attractKey)) return;

        // Listo: bloquear en el momento de la colisión
        Grab(rb);
    }

    // Mantiene al objeto agarrado exactamente en el magnetPoint
    void MaintainGrabbedTransform()
    {
        if (!grabbedRb) return;
        grabbedRb.transform.localPosition = Vector3.zero;
        grabbedRb.transform.localRotation = Quaternion.identity;
    }

    void AttractNearbyAndMaybeGrab()
    {
        // Buscar candidatos en rango
        Collider[] hits = Physics.OverlapSphere(
            magnetPoint.position,
            attractionRange,
            grabbableMask,
            QueryTriggerInteraction.Collide
        );
        if (hits == null || hits.Length == 0) return;

        // Elegir el más cercano válido (opcional, ayuda a no repartir fuerzas)
        Rigidbody best = null;
        float bestDist = float.MaxValue;

        foreach (var col in hits)
        {
            var rb = col.attachedRigidbody;
            if (!rb) continue;
            if (rb.transform.IsChildOf(transform)) continue; // evitar atraer partes propias

            Vector3 toMagnet = magnetPoint.position - rb.worldCenterOfMass;
            float dist = toMagnet.magnitude;
            if (dist < VectorLib.EPSILON) continue;

            if (requireLineOfSight)
            {
                RaycastHit rh;
                if (Physics.Raycast(rb.worldCenterOfMass, toMagnet / dist, out rh, dist, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (rh.collider.transform != col.transform && !rh.collider.transform.IsChildOf(transform))
                        continue; // sin visión limpia
                }
            }

            if (dist < bestDist)
            {
                bestDist = dist;
                best = rb;
            }
        }

        if (!best) return;

        // Respaldo: si está muy cerca del centro, haz snap (pero normalmente haremos snap por contacto)
        if (bestDist <= snapDistance)
        {
            Grab(best);
            return;
        }

        // Aplicar fuerza de atracción con amortiguación cercana
        Vector3 toMagnetBest = magnetPoint.position - best.worldCenterOfMass;
        float distBest = toMagnetBest.magnitude;
        Vector3 dir = toMagnetBest / distBest;

        float t = distBest / (attractionRange <= 0f ? 1f : attractionRange);
        float u = 1f - (useSmootherStep ? LerpLib.SmootherStep01(t) : LerpLib.SmoothStep01(t));
        u = LerpLib.Retallar01(u);

        float accel = maxAcceleration * u;
        best.AddForce(dir * accel, ForceMode.Acceleration);

        if (distBest < snapDistance * 2f && nearDamping > 0f)
        {
            float damp = 1f - LerpLib.Retallar01(nearDamping * Time.deltaTime * 10f);
            best.velocity *= damp;
            best.angularVelocity *= damp;
        }
    }

    void Grab(Rigidbody rb)
    {
        if (!rb) return;

        grabbedRb = rb;
        prevKinematic = rb.isKinematic;
        prevParent = rb.transform.parent;

        // Parent al imán y posicionamiento exacto
        rb.transform.SetParent(magnetPoint, worldPositionStays: false);
        rb.transform.localPosition = Vector3.zero;
        rb.transform.localRotation = Quaternion.identity;

        // Bloquear física si procede
        if (forceKinematicWhileGrabbed)
            rb.isKinematic = true;

        // Reset de velocidades para que no “tire” del imán
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void ReleaseGrabbed()
    {
        if (grabbedRb == null) return;

        // Restaurar parent y estado cinemático
        grabbedRb.transform.SetParent(prevParent, worldPositionStays: true);
        if (forceKinematicWhileGrabbed)
            grabbedRb.isKinematic = prevKinematic;

        // Opcional: limpiar velocidades (evita “saltos” al soltar)
        grabbedRb.velocity = Vector3.zero;
        grabbedRb.angularVelocity = Vector3.zero;

        // Limpiar estado
        grabbedRb = null;
        prevParent = null;
        prevKinematic = false;
    }
}