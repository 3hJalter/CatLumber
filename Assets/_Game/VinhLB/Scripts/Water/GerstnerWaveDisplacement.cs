using UnityEngine;

namespace VinhLB
{
    public static class GerstnerWaveDisplacement
    {
        private static Vector3 GerstnerWave(Vector3 position, float steepness, float wavelength, float speed,
            float direction)
        {
            direction = direction * 2.0f - 1.0f;
            Vector2 d = new Vector2(Mathf.Cos(Mathf.PI * direction), Mathf.Sin(Mathf.PI * direction)).normalized;
            float k = 2.0f * Mathf.PI / wavelength;
            float a = steepness / k;
            float f = k * (Vector2.Dot(d, new Vector2(position.x, position.z)) - speed * Time.time);

            return new Vector3(d.x * a * Mathf.Cos(f), a * Mathf.Sin(f), d.y * a * Mathf.Cos(f));
        }

        public static Vector3 GetWaveDisplacement(Vector3 position, float steepness, float wavelength, float speed,
            float[] directions)
        {
            Vector3 offset = Vector3.zero;

            offset += GerstnerWave(position, steepness, wavelength, speed, directions[0]);
            offset += GerstnerWave(position, steepness, wavelength, speed, directions[1]);
            offset += GerstnerWave(position, steepness, wavelength, speed, directions[2]);
            offset += GerstnerWave(position, steepness, wavelength, speed, directions[3]);

            return offset;
        }
    }
}
