using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LerpLib : MonoBehaviour
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

    public static float Retallar01(float t) => t < 0f ? 0f : (t > 1f ? 1f : t);
    public static float Retallar(float valor, float min, float max)
        => valor < min ? min : (valor > max ? max : valor);

    /// valor modular en [0, llargada) tolerant a negatius
    public static float Repetir(float temps, float llargada)
    {
        if (llargada <= 0f) return 0f;
        float m = temps % llargada; // pot ser negatiu
        return m < 0f ? m + llargada : m;
    }

    /// oscil lacio 0..llargada..0.. (tipus PingPong)
    public static float PingPong(float temps, float llargada)
    {
        float r = Repetir(temps, llargada * 2f);
        return llargada - Abs(r - llargada);
    }

    //LERP BASIC (float)
    /// Interpolacio lineal amb clamp de t a [0,1]
    public static float Lerp(float inici, float fi, float t)
        => inici + (fi - inici) * Retallar01(t);

    /// Interpolacio lineal sense clamp
    public static float LerpSenseClamp(float inici, float fi, float t)
        => inici + (fi - inici) * t;

    /// InverseLerp: retorna t tal que Lerp(inici,fi,t) = valor
    public static float InverseLerp(float inici, float fi, float valor, bool clamp01 = true)
    {
        float den = fi - inici;
        if (Abs(den) < EPS) return 0f;
        float t = (valor - inici) / den;
        return clamp01 ? Retallar01(t) : t;
    }

    /// Remap d un rang a un altre
    public static float Remap(float inMin, float inMax, float outMin, float outMax, float valor, bool clampIn = true)
    {
        float t = InverseLerp(inMin, inMax, valor, clampIn);
        return LerpSenseClamp(outMin, outMax, t);
    }

    //CURVES SUAVITZADES
    /// SmoothStep cubic: 3t^2 - 2t^3 (amb clamp)
    public static float SmoothStep01(float t)
    {
        t = Retallar01(t);
        return t * t * (3f - 2f * t);
    }

    /// SmootherStep quintic: 6t^5 - 15t^4 + 10t^3 (amb clamp)
    public static float SmootherStep01(float t)
    {
        t = Retallar01(t);
        float t2 = t * t;
        float t3 = t2 * t;
        return t3 * (t * (6f * t - 15f) + 10f);
    }

    /// Lerp amb SmoothStep sobre t
    public static float LerpSuau(float inici, float fi, float t)
        => LerpSenseClamp(inici, fi, SmoothStep01(t));

    /// Lerp amb SmootherStep sobre t
    public static float LerpMesSuau(float inici, float fi, float t)
        => LerpSenseClamp(inici, fi, SmootherStep01(t));

    //LERP D ANGLES (GRAUS, wrap 360)
    /// Lerp d angles en graus pel cami curt
    public static float LerpAngle(float iniciGraus, float fiGraus, float t)
    {
        t = Retallar01(t);
        float delta = Repetir(fiGraus - iniciGraus + 180f, 360f) - 180f; // -180..180
        return iniciGraus + delta * t;
    }

    //LERP VECTORIAL
    public static Vector2 Lerp(Vector2 inici, Vector2 fi, float t)
        => inici + (fi - inici) * Retallar01(t);

    public static Vector2 LerpSenseClamp(Vector2 inici, Vector2 fi, float t)
        => inici + (fi - inici) * t;

    public static Vector3 Lerp(Vector3 inici, Vector3 fi, float t)
        => inici + (fi - inici) * Retallar01(t);

    public static Vector3 LerpSenseClamp(Vector3 inici, Vector3 fi, float t)
        => inici + (fi - inici) * t;

    public static Color Lerp(Color inici, Color fi, float t)
    {
        t = Retallar01(t);
        return new Color(
            inici.r + (fi.r - inici.r) * t,
            inici.g + (fi.g - inici.g) * t,
            inici.b + (fi.b - inici.b) * t,
            inici.a + (fi.a - inici.a) * t
        );
    }

    //INTERPOLACIO ESFERICA PER DIRECCIONS 3D
    // SlerpDir3: per vectors direccio (unitaris). Si no ho son, es normalitzen.
    public static Vector3 SlerpDir3(Vector3 iniciDir, Vector3 fiDir, float t)
    {
        t = Retallar01(t);
        Vector3 a = iniciDir.sqrMagnitude > EPS ? iniciDir / Sqrt(iniciDir.sqrMagnitude) : Vector3.forward;
        Vector3 b = fiDir.sqrMagnitude > EPS ? fiDir / Sqrt(fiDir.sqrMagnitude) : Vector3.forward;

        float punt = DotSenseMathf(a, b);         // [-1,1]
        punt = Retallar(punt, -1f, 1f);

        float omega = Acos(punt);                  // angle entre a i b
        if (Abs(omega) < 1e-5f) return a;         // gairebe iguals

        float sinOm = Sin(omega);
        float pesA = Sin((1f - t) * omega) / sinOm;
        float pesB = Sin(t * omega) / sinOm;

        Vector3 resultat = a * pesA + b * pesB;
        float m2 = resultat.sqrMagnitude;
        return m2 > EPS ? resultat / Sqrt(m2) : a;
    }

    // Dot product manual sense Mathf (evita dependencies)
    static float DotSenseMathf(Vector3 u, Vector3 v) => u.x * v.x + u.y * v.y + u.z * v.z;
}
