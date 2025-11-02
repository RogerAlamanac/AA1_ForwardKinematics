using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagneticGripper : MonoBehaviour
{
    [Header("Magnet mount (child del end-effector)")]
    public Transform magnetPoint;

    [Header("Targets")]
    public LayerMask grabbableMask;    
    public float attractionRange = 3.0f;    
    public bool requireLineOfSight = true;  

    [Header("Modelo de fuerza (usa librerías del proyecto)")]
    public float maxAcceleration = 60f;
    public bool useSmootherStep = true;

    [Range(0f, 1f)] public float nearDamping = 0.5f;

    [Header("Snap / bloqueo")]
    public float snapDistance = 0.08f;

    public bool forceKinematicWhileGrabbed = true;

    [Header("Input")]
    public KeyCode attractKey = KeyCode.Space;


    Rigidbody grabbedRb = null;
    bool prevKinematic = false;

    Vector3 grabbedLocalPos;   
    Quaternion grabbedLocalRot;  

    void Reset()
    {
        var t = transform.Find("Gripper");
        magnetPoint = t ? t : transform;
    }

    void Update()
    {
        if (!magnetPoint) return;

        if (Input.GetKey(attractKey))
        {
            if (grabbedRb == null)
            {
                AttractNearbyAndMaybeGrab();
            }
            else
            {
                MaintainGrabbedTransform();
            }
        }

        if (Input.GetKeyUp(attractKey))
        {
            if (grabbedRb != null) ReleaseGrabbed();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        TrySnapOnContact(collision.rigidbody);
    }

    void OnTriggerEnter(Collider other)
    {
        TrySnapOnContact(other.attachedRigidbody);
    }

    void TrySnapOnContact(Rigidbody rb)
    {
        if (grabbedRb != null) return;               
        if (!rb) return;
        if (!Input.GetKey(attractKey)) return;         
        if (rb.transform.IsChildOf(transform)) return; 
        if ((grabbableMask.value & (1 << rb.gameObject.layer)) == 0) return; 

        Grab(rb);
    }

    void MaintainGrabbedTransform()
    {
        if (!grabbedRb) return;

        Vector3 targetPos = magnetPoint.TransformPoint(grabbedLocalPos);
        Quaternion targetRot = magnetPoint.rotation * grabbedLocalRot;
   
        grabbedRb.MovePosition(targetPos);
        grabbedRb.MoveRotation(targetRot);
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

        // Elegir el más cercano válido
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
                        continue;
                }
            }

            if (dist < bestDist)
            {
                bestDist = dist;
                best = rb;
            }
        }

        if (!best) return;

  
        if (bestDist <= snapDistance)
        {
            Grab(best);
            return;
        }

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

        if (distBest <= snapDistance)
        {
            Grab(best);
        }
    }

    void Grab(Rigidbody rb)
    {
        if (!rb) return;

        grabbedRb = rb;
        prevKinematic = rb.isKinematic;

        grabbedLocalPos = magnetPoint.InverseTransformPoint(rb.transform.position);
        grabbedLocalRot = Quaternion.Inverse(magnetPoint.rotation) * rb.transform.rotation;

        // Bloquear física 
        if (forceKinematicWhileGrabbed)
            rb.isKinematic = true;

        // Reset de velocidades para que no tire del imán
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void ReleaseGrabbed()
    {
        if (grabbedRb == null) return;

        // Restaurar estado cinemático original
        if (forceKinematicWhileGrabbed)
            grabbedRb.isKinematic = prevKinematic;

        // Limpiar velocidades
        grabbedRb.velocity = Vector3.zero;
        grabbedRb.angularVelocity = Vector3.zero;

        // Limpiar estado
        grabbedRb = null;
    }
}
