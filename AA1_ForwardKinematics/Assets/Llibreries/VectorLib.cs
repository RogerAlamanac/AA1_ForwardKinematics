using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VectorLib : MonoBehaviour
{
    //CONSTANTS I AJUDES
    /// Valor petit per evitar divisions per zero o inestabilitats numeriques
    public const float EPSILON = 1e-6f;
    /// EPSILON al quadrat (util per comparar magnituds^2)
    public const float EPSILON_QUAD = EPSILON * EPSILON;
    /// Pi en float
    public const float PI = (float)System.Math.PI;
    /// Conversio graus -> radiants: graus * DEG_A_RAD = radiants
    public const float DEG_A_RAD = PI / 180f;
    /// Conversio radiants -> graus: radiants * RAD_A_DEG = graus
    public const float RAD_A_DEG = 180f / PI;
    // Wrappers de System.Math que retornen floats per evitar l'us de llibreries de unity
    static float Abs(float v) => (float)System.Math.Abs(v);
    static float Raiz(float v) => (float)System.Math.Sqrt(v);
    static float Seno(float r) => (float)System.Math.Sin(r);
    static float Coseno(float r) => (float)System.Math.Cos(r);
    static float ArcCos(float v) => (float)System.Math.Acos(v);
    static float ArcTan2(float y, float x) => (float)System.Math.Atan2(y, x);

    /// Retalla v al rang [min, max]
    static float Retallar(float v, float min, float max)
        => v < min ? min : (v > max ? max : v);
    /// Signe de v (-1, 0, +1)
    static float Signe(float v) => v > 0f ? 1f : (v < 0f ? -1f : 0f);



    //GENERALS
    /// Retorna vector normalitzat; si |v| es molt petit, retorna alternativa
    public static Vector2 NormalitzarSegur(Vector2 vector, Vector2 alternativa = default)
        => vector.sqrMagnitude > EPSILON_QUAD ? vector / Raiz(vector.sqrMagnitude) : alternativa;

    public static Vector3 NormalitzarSegur(Vector3 vector, Vector3 alternativa = default)
        => vector.sqrMagnitude > EPSILON_QUAD ? vector / Raiz(vector.sqrMagnitude) : alternativa;

    /// Comparacio aproximada amb tolerancia
    public static bool Aproximadament(Vector2 a, Vector2 b, float tol = 1e-5f)
        => (a - b).sqrMagnitude <= tol * tol;

    public static bool Aproximadament(Vector3 a, Vector3 b, float tol = 1e-5f)
        => (a - b).sqrMagnitude <= tol * tol;

    /// Projeccio escalar (component signada d a sobre una direccio normalitzada)
    public static float ProjeccioEscalar(Vector3 a, Vector3 direccioNormal) => Vector3.Dot(a, direccioNormal);



    //VECTOR2

    /// Perpendicular a l esquerra (90 graus)
    public static Vector2 PerpEsquerra(Vector2 v) => new Vector2(-v.y, v.x);

    /// Perpendicular a la dreta (90 graus)
    public static Vector2 PerpDreta(Vector2 v) => new Vector2(v.y, -v.x);

    /// Rota un vector 2D en graus; si s indica, al voltant d un pivot
    public static Vector2 Rotar(Vector2 vector, float graus, Vector2? pivot = null)
    {
        Vector2 v = pivot.HasValue ? vector - pivot.Value : vector;
        float r = graus * DEG_A_RAD;
        float c = Coseno(r);
        float s = Seno(r);
        Vector2 rotat = new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
        return pivot.HasValue ? rotat + pivot.Value : rotat;
    }

    /// Projeccio de a sobre b (b no cal que estigui normalitzat)
    public static Vector2 Projectar(Vector2 a, Vector2 b)
    {
        float denom = Vector2.Dot(b, b);
        if (denom <= EPSILON_QUAD) return Vector2.zero;
        return Vector2.Dot(a, b) / denom * b;
    }

    /// Rebutjar: component d a perpendicular a b (a - projeccio)
    public static Vector2 Rebutjar(Vector2 a, Vector2 b) => a - Projectar(a, b);

    /// Reflecteix un vector respecte una normal 2D
    public static Vector2 Reflectir(Vector2 vector, Vector2 normal)
    {
        Vector2 n = NormalitzarSegur(normal, Vector2.up);
        return vector - 2f * Vector2.Dot(vector, n) * n;
    }

    /// Angle amb signe d a fins a b (graus). Positiu en sentit antihorari
    public static float AngleAmbSigne(Vector2 a, Vector2 b)
    {
        float crossZ = a.x * b.y - a.y * b.x; // component Z de a x b en 2D
        float dot = Vector2.Dot(a, b);
        return ArcTan2(crossZ, dot) * RAD_A_DEG;
    }

    /// Construeix un vector 2D des de coordenades polars (radi, graus)
    public static Vector2 DesDePolars(float radi, float graus)
    {
        float r = graus * DEG_A_RAD;
        return new Vector2(radi * Coseno(r), radi * Seno(r));
    }



    //VECTOR3

    /// Projeccio de a sobre b (3D)
    public static Vector3 Projectar(Vector3 a, Vector3 b)
    {
        float denom = Vector3.Dot(b, b);
        if (denom <= EPSILON_QUAD) return Vector3.zero;
        return Vector3.Dot(a, b) / denom * b;
    }

    ///Rebutjar en 3D
    public static Vector3 Rebutjar(Vector3 a, Vector3 b) => a - Projectar(a, b);

    ///Projeccio d un vector sobre un pla amb normal donada
    public static Vector3 ProjectarEnPla(Vector3 vector, Vector3 normalPla)
    {
        Vector3 n = NormalitzarSegur(normalPla, Vector3.up);
        return vector - Vector3.Dot(vector, n) * n;
    }

    ///Reflectir en 3D
    public static Vector3 Reflectir(Vector3 vector, Vector3 normal)
    {
        Vector3 n = NormalitzarSegur(normal, Vector3.up);
        return vector - 2f * Vector3.Dot(vector, n) * n;
    }

    ///Angle sense signe entre dos vectors 3D (0..180 graus)
    public static float Angle(Vector3 a, Vector3 b)
    {
        float denomQuad = a.sqrMagnitude * b.sqrMagnitude;
        if (denomQuad <= EPSILON_QUAD) return 0f;
        float denom = Raiz(denomQuad);
        float cos = Retallar(Vector3.Dot(a, b) / denom, -1f, 1f);
        return ArcCos(cos) * RAD_A_DEG;
    }

    ///Angle amb signe d a fins a b al voltant d un eix (regla ma dreta)
    public static float AngleAmbSigneAlVoltantEix(Vector3 a, Vector3 b, Vector3 eix)
    {
        Vector3 eixN = NormalitzarSegur(eix, Vector3.up);
        Vector3 aPla = ProjectarEnPla(a, eixN);
        Vector3 bPla = ProjectarEnPla(b, eixN);

        Vector3 c = Vector3.Cross(aPla, bPla);
        float magnitudC = Raiz(c.sqrMagnitude);
        float signe = Signe(Vector3.Dot(eixN, c));
        float dot = Vector3.Dot(aPla, bPla);

        return ArcTan2(magnitudC * signe, dot) * RAD_A_DEG;
    }

    ///Base ortonormal (tangenta i bitangenta) a partir d una normal
    public static void BaseOrtonormal(Vector3 normal, out Vector3 tangenta, out Vector3 bitangenta)
    {
        Vector3 n = NormalitzarSegur(normal, Vector3.up);
        // triem un ajudant que no sigui gaire paral lel a n
        Vector3 ajudant = Abs(n.y) < 0.999f ? Vector3.up : Vector3.right;
        tangenta = NormalitzarSegur(Vector3.Cross(ajudant, n), Vector3.right);
        bitangenta = Vector3.Cross(n, tangenta);
    }

    ///Limita la magnitud a una longitud maxima
    public static Vector3 LimitarMagnitud(Vector3 vector, float longitudMax)
    {
        if (longitudMax <= 0f) return Vector3.zero;
        float m2 = vector.sqrMagnitude;
        float max2 = longitudMax * longitudMax;
        if (m2 > max2 && m2 > 0f)
        {
            float k = longitudMax / Raiz(m2);
            return vector * k;
        }
        return vector;
    }

    ///Producte triple escalar: a . (b x c)
    public static float TripleEscalar(Vector3 a, Vector3 b, Vector3 c)
        => Vector3.Dot(a, Vector3.Cross(b, c));

    ///Area del paralelogram generat per a i b (|a x b|)
    public static float AreaParalelogram(Vector3 a, Vector3 b)
        => Raiz(Vector3.Cross(a, b).sqrMagnitude);

    ///Area del triangle generat per a i b (meitat del paralelogram)
    public static float AreaTriangle(Vector3 a, Vector3 b)
        => 0.5f * AreaParalelogram(a, b);
}
