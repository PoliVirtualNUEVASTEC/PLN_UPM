using System;

namespace NpcAi.VrInput
{
    /// <summary>
    /// Vector de 3 componentes propio del nucleo (AD1): evita que <c>UnityEngine.Vector3</c>
    /// llegue a <see cref="SpatialPhysicalActionSource"/> y rompa la prueba sin escena. El
    /// nucleo usa <see cref="System.Math"/>, nunca <c>Mathf</c>.
    /// </summary>
    internal readonly struct Vec3
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3 operator -(Vec3 a, Vec3 b)
            => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public float Longitud => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        public static float Distancia(Vec3 a, Vec3 b) => (a - b).Longitud;

        /// <summary>Angulo en grados entre dos direcciones. -1 si alguna es degenerada (longitud cero).</summary>
        public static float AnguloEnGrados(Vec3 a, Vec3 b)
        {
            var largoA = a.Longitud;
            var largoB = b.Longitud;
            if (largoA <= 0f || largoB <= 0f) return -1f;

            var punto = a.X * b.X + a.Y * b.Y + a.Z * b.Z;
            var coseno = punto / (largoA * largoB);
            if (coseno > 1f) coseno = 1f;
            else if (coseno < -1f) coseno = -1f;

            return (float)(Math.Acos(coseno) * (180.0 / Math.PI));
        }
    }

    /// <summary>
    /// Una muestra digerida del estado espacial del usuario, lista para que el nucleo corra
    /// sus tres detectores (AD2/AD3). La produce <see cref="ISpatialSampler"/>; el nucleo
    /// nunca toca <c>Camera</c>, <c>Transform</c>, <c>Collider</c> ni <c>Time</c> directamente.
    /// </summary>
    internal readonly struct SpatialSample
    {
        public readonly Vec3 PosicionDeCabeza;
        public readonly Vec3 FrenteDeCabeza;
        public readonly Vec3 PosicionDelObjetivo;
        public readonly bool HayObjetivo;
        public readonly bool PulsoDeContacto;
        public readonly float DeltaSegundos;

        public SpatialSample(
            Vec3 posicionDeCabeza,
            Vec3 frenteDeCabeza,
            Vec3 posicionDelObjetivo,
            bool hayObjetivo,
            bool pulsoDeContacto,
            float deltaSegundos)
        {
            PosicionDeCabeza = posicionDeCabeza;
            FrenteDeCabeza = frenteDeCabeza;
            PosicionDelObjetivo = posicionDelObjetivo;
            HayObjetivo = hayObjetivo;
            PulsoDeContacto = pulsoDeContacto;
            DeltaSegundos = deltaSegundos;
        }
    }
}
