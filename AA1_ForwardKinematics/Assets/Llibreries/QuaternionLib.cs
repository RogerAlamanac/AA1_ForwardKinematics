using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuaternionLib : MonoBehaviour
{
    //CONSTANTS I AJUDES
    public const float EPS = 1e-6f;
    public const float PI = (float)System.Math.PI;
    public const float DEG_A_RAD = PI / 180f;
    public const float RAD_A_DEG = 180f / PI;

    static float Abs(float v) => (float)System.Math.Abs(v);
    static float Sqrt(float v) => (float)System.Math.Sqrt(v);
    static float Sin(float v) => (float)System.Math.Sin(v);
    static float Cos(float v) => (float)System.Math.Cos(v);
    static float Acos(float v) => (float)System.Math.Acos(v);
    static float Atan2(float y, float x) => (float)System.Math.Atan2(y, x);

    static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
    static float Clamp01(float t) => t < 0f ? 0f : (t > 1f ? 1f : t);

    // ---------- BASICS ----------
    /// Dot product entre dos quaternions
    public static float Punt(Quaternion a, Quaternion b)
        => a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

    /// Norm quadrada
    public static float NormaQuad(Quaternion q)
        => q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;

    /// Norma
    public static float Norma(Quaternion q)
        => Sqrt(NormaQuad(q));

    /// Normalitza; si molt petit, retorna identitat
    public static Quaternion Normalitzar(Quaternion q)
    {
        float n2 = NormaQuad(q);
        if (n2 <= EPS) return Quaternion.identity;
        float inv = 1f / Sqrt(n2);
        return new Quaternion(q.x * inv, q.y * inv, q.z * inv, q.w * inv);
    }

    /// Conjugat
    public static Quaternion Conjugat(Quaternion q)
        => new Quaternion(-q.x, -q.y, -q.z, q.w);

    /// Inversa (q^-1) = conjugat(q) / |q|^2
    public static Quaternion Inversa(Quaternion q)
    {
        float n2 = NormaQuad(q);
        if (n2 <= EPS) return Quaternion.identity;
        float inv = 1f / n2;
        return new Quaternion(-q.x * inv, -q.y * inv, -q.z * inv, q.w * inv);
    }

    /// Producte: composa rotacions (primer a, despres b): q = b * a
    public static Quaternion Producte(Quaternion b, Quaternion a)
    {
        return new Quaternion(
            b.w * a.x + b.x * a.w + b.y * a.z - b.z * a.y,
            b.w * a.y - b.x * a.z + b.y * a.w + b.z * a.x,
            b.w * a.z + b.x * a.y - b.y * a.x + b.z * a.w,
            b.w * a.w - b.x * a.x - b.y * a.y - b.z * a.z
        );
    }

    // ---------- CREACIÓ I DESCOMP. ----------
    /// Des de eix (unit) i angle en graus
    public static Quaternion DesDeEixAngle(Vector3 eix, float graus)
    {
        // normalitzar eix
        float m2 = eix.sqrMagnitude;
        Vector3 n = m2 > EPS ? eix / Sqrt(m2) : Vector3.up;
        float h = graus * DEG_A_RAD * 0.5f;
        float s = Sin(h);
        float c = Cos(h);
        return new Quaternion(n.x * s, n.y * s, n.z * s, c);
    }

    /// A eix-angle (graus). Retorna eix unit i angle en graus.
    public static void A_EixAngle(Quaternion q, out Vector3 eix, out float graus)
    {
        Quaternion u = Normalitzar(q);
        float ang = 2f * Acos(Clamp(u.w, -1f, 1f)); // radians
        float s = Sqrt(MathfMax(0f, 1f - u.w * u.w)); // = sin(ang/2)
        if (s < 1e-6f)
        {
            // angle ~ 0: eix arbitrary
            eix = new Vector3(1f, 0f, 0f);
            graus = ang * RAD_A_DEG;
        }
        else
        {
            eix = new Vector3(u.x / s, u.y / s, u.z / s);
            graus = ang * RAD_A_DEG;
        }
    }

    /// Des de Euler XYZ en graus (rotacio al voltant de X, despres Y, despres Z)
    public static Quaternion DesDeEulerXYZ(float rx, float ry, float rz)
    {
        float hx = rx * DEG_A_RAD * 0.5f;
        float hy = ry * DEG_A_RAD * 0.5f;
        float hz = rz * DEG_A_RAD * 0.5f;

        float cx = Cos(hx), sx = Sin(hx);
        float cy = Cos(hy), sy = Sin(hy);
        float cz = Cos(hz), sz = Sin(hz);

        // ordre XYZ: q = qz * qy * qx
        float w = cx * cy * cz - sx * sy * sz;
        float x = sx * cy * cz + cx * sy * sz;
        float y = cx * sy * cz - sx * cy * sz;
        float z = cx * cy * sz + sx * sy * cz;

        return Normalitzar(new Quaternion(x, y, z, w));
    }

    /// A Euler XYZ en graus (atencio a gimbal lock)
    public static Vector3 A_EulerXYZ(Quaternion q)
    {
        // Matriu de rotacio des de q
        float xx = q.x * q.x, yy = q.y * q.y, zz = q.z * q.z, ww = q.w * q.w;
        float xy = q.x * q.y, xz = q.x * q.z, yz = q.y * q.z;
        float wx = q.w * q.x, wy = q.w * q.y, wz = q.w * q.z;

        // Rotacio (column major assumint vectors columna)
        // r00 = 1-2(yy+zz) ; r01 = 2(xy-wz) ; r02 = 2(xz+wy)
        // r10 = 2(xy+wz)   ; r11 = 1-2(xx+zz); r12 = 2(yz-wx)
        // r20 = 2(xz-wy)   ; r21 = 2(yz+wx)  ; r22 = 1-2(xx+yy)
        float r00 = 1f - 2f * (yy + zz);
        float r01 = 2f * (xy - wz);
        float r02 = 2f * (xz + wy);
        float r10 = 2f * (xy + wz);
        float r11 = 1f - 2f * (xx + zz);
        float r12 = 2f * (yz - wx);
        float r20 = 2f * (xz - wy);
        float r21 = 2f * (yz + wx);
        float r22 = 1f - 2f * (xx + yy);

        // Descomposicio XYZ
        float ry = (float)System.Math.Asin(Clamp(r02, -1f, 1f)); // radians
        float rx = Atan2(-r12, r22);
        float rz = Atan2(-r01, r00);

        return new Vector3(rx * RAD_A_DEG, ry * RAD_A_DEG, rz * RAD_A_DEG);
    }

    // ---------- LOOK ROTATION ----------
    /// Crea rotacio que mira cap a forward amb up aproximat
    public static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        // normalitzar vectors i construir base ortonormal
        Vector3 f = forward.sqrMagnitude > EPS ? forward / Sqrt(forward.sqrMagnitude) : Vector3.forward;
        Vector3 u = up.sqrMagnitude > EPS ? up / Sqrt(up.sqrMagnitude) : Vector3.up;

        Vector3 r = Vector3.Cross(u, f);
        float r2 = r.sqrMagnitude;
        if (r2 <= EPS)
        {
            // up quasi paral lel a forward: tria un up diferent
            u = Abs(f.y) < 0.999f ? Vector3.up : Vector3.right;
            r = Vector3.Cross(u, f);
            r2 = r.sqrMagnitude;
            if (r2 <= EPS) r = new Vector3(1, 0, 0);
        }
        r /= Sqrt(r2);
        u = Vector3.Cross(f, r);

        // Matriu amb columnes r,u,f
        // Converteix matriu -> quaternion (branch numeric estable)
        float m00 = r.x, m01 = u.x, m02 = f.x;
        float m10 = r.y, m11 = u.y, m12 = f.y;
        float m20 = r.z, m21 = u.z, m22 = f.z;

        float tr = m00 + m11 + m22;
        Quaternion q;
        if (tr > 0f)
        {
            float s = Sqrt(tr + 1f) * 2f; // s = 4*w
            float inv = 1f / s;
            q = new Quaternion(
                (m21 - m12) * inv,
                (m02 - m20) * inv,
                (m10 - m01) * inv,
                0.25f * s
            );
        }
        else if (m00 > m11 && m00 > m22)
        {
            float s = Sqrt(1f + m00 - m11 - m22) * 2f;
            float inv = 1f / s;
            q = new Quaternion(
                0.25f * s,
                (m01 + m10) * inv,
                (m02 + m20) * inv,
                (m21 - m12) * inv
            );
        }
        else if (m11 > m22)
        {
            float s = Sqrt(1f + m11 - m00 - m22) * 2f;
            float inv = 1f / s;
            q = new Quaternion(
                (m01 + m10) * inv,
                0.25f * s,
                (m12 + m21) * inv,
                (m02 - m20) * inv
            );
        }
        else
        {
            float s = Sqrt(1f + m22 - m00 - m11) * 2f;
            float inv = 1f / s;
            q = new Quaternion(
                (m02 + m20) * inv,
                (m12 + m21) * inv,
                0.25f * s,
                (m10 - m01) * inv
            );
        }
        return Normalitzar(q);
    }

    // ---------- ROTAR VECTOR ----------
    /// Rota un vector v amb un quaternion q (metode rapid)
    public static Vector3 RotarVector(Quaternion q, Vector3 v)
    {
        // v' = v + 2*w*(q.xyz x v) + 2*(q.xyz x (q.xyz x v))
        Vector3 qv = new Vector3(q.x, q.y, q.z);
        Vector3 t = 2f * Vector3.Cross(qv, v);
        return v + q.w * t + Vector3.Cross(qv, t);
    }

    // ---------- ANGLE ENTRE ----------
    /// Angle entre dues rotacions (graus)
    public static float AngleEntre(Quaternion a, Quaternion b)
    {
        float d = Clamp(Abs(Punt(a, b)), -1f, 1f);
        // angle = 2 * acos(|dot|)
        return 2f * Acos(d) * RAD_A_DEG;
    }

    // ---------- LERP / NLERP / SLERP ----------
    /// Lerp de quaternions amb cami curt i normalitzacio (equivalent a nlerp clamped)
    public static Quaternion Lerp(Quaternion inici, Quaternion fi, float t)
    {
        t = Clamp01(t);
        Quaternion b = fi;
        if (Punt(inici, fi) < 0f) b = new Quaternion(-fi.x, -fi.y, -fi.z, -fi.w);
        Quaternion q = new Quaternion(
            inici.x + (b.x - inici.x) * t,
            inici.y + (b.y - inici.y) * t,
            inici.z + (b.z - inici.z) * t,
            inici.w + (b.w - inici.w) * t
        );
        return Normalitzar(q);
    }

    /// Nlerp (sense clamp extern)
    public static Quaternion Nlerp(Quaternion inici, Quaternion fi, float t)
    {
        Quaternion b = fi;
        if (Punt(inici, fi) < 0f) b = new Quaternion(-fi.x, -fi.y, -fi.z, -fi.w);
        Quaternion q = new Quaternion(
            inici.x + (b.x - inici.x) * t,
            inici.y + (b.y - inici.y) * t,
            inici.z + (b.z - inici.z) * t,
            inici.w + (b.w - inici.w) * t
        );
        return Normalitzar(q);
    }

    /// Slerp amb cami curt i fallback a nlerp per angles petits
    public static Quaternion Slerp(Quaternion inici, Quaternion fi, float t)
    {
        t = Clamp01(t);
        Quaternion b = fi;
        float dot = Punt(inici, fi);
        if (dot < 0f) { dot = -dot; b = new Quaternion(-fi.x, -fi.y, -fi.z, -fi.w); }

        dot = Clamp(dot, -1f, 1f);

        // si molt a prop, nlerp per estabilitat
        if (dot > 0.9995f)
            return Nlerp(inici, b, t);

        float omega = Acos(dot);      // angle entre
        float sinOm = Sin(omega);
        float w0 = Sin((1f - t) * omega) / sinOm;
        float w1 = Sin(t * omega) / sinOm;

        Quaternion q = new Quaternion(
            inici.x * w0 + b.x * w1,
            inici.y * w0 + b.y * w1,
            inici.z * w0 + b.z * w1,
            inici.w * w0 + b.w * w1
        );
        return Normalitzar(q);
    }

    // ---------- UTILS PRIVATS ----------
    static float MathfMax(float a, float b) => a > b ? a : b;
}
