using UnityEngine;

namespace VinhLB
{
    public class BuoyantObject : HMonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Rigidbody _rigidbody;

        [Header("Water")]
        [SerializeField]
        private float _waterHeight;

        [Header("Waves")]
        [SerializeField]
        private float _steepness;
        [SerializeField]
        private float _wavelength;
        [SerializeField]
        private float _speed;
        [SerializeField]
        private float[] _directionArray = new float[4];

        [Header("Buoyancy")]
        [SerializeField]
        [Range(1.0f, 5.0f)]
        private float _strength = 1.0f;
        [SerializeField]
        [Range(0.2f, 5.0f)]
        private float _objectDepth = 1.0f;
        [SerializeField]
        private float _velocityDrag = 0.99f;
        [SerializeField]
        private float _angularDrag = 0.5f;

        [Header("Effectors")]
        [SerializeField]
        private Transform[] _effectorArray;

        private readonly Color _blue = new(0.2f, 0.67f, 0.92f);
        private readonly Color _green = new(0.2f, 0.92f, 0.51f);
        private readonly Color _orange = new(0.97f, 0.79f, 0.26f);

        private readonly Color _red = new(0.92f, 0.25f, 0.2f);

        private Vector3[] _effectorProjections;

        private void Awake()
        {
            _rigidbody.useGravity = false;

            _effectorProjections = new Vector3[_effectorArray.Length];
            for (int i = 0; i < _effectorArray.Length; i++) _effectorProjections[i] = _effectorArray[i].position;
        }

        private void FixedUpdate()
        {
            int effectorAmount = _effectorArray.Length;

            for (int i = 0; i < effectorAmount; i++)
            {
                Vector3 effectorPosition = _effectorArray[i].position;

                _effectorProjections[i] = effectorPosition;
                _effectorProjections[i].y = _waterHeight +
                                                GerstnerWaveDisplacement.GetWaveDisplacement(effectorPosition,
                                                    _steepness, _wavelength, _speed, _directionArray).y;

                // gravity
                _rigidbody.AddForceAtPosition(Physics.gravity / effectorAmount, effectorPosition,
                    ForceMode.Acceleration);

                float waveHeight = _effectorProjections[i].y;
                float effectorHeight = effectorPosition.y;

                if (!(effectorHeight < waveHeight)) continue; // submerged

                float submersion = Mathf.Clamp01(waveHeight - effectorHeight) / _objectDepth;
                float buoyancy = Mathf.Abs(Physics.gravity.y) * submersion * _strength;

                // buoyancy
                _rigidbody.AddForceAtPosition(Vector3.up * buoyancy, effectorPosition, ForceMode.Acceleration);

                // drag
                _rigidbody.AddForce(-_rigidbody.velocity * (_velocityDrag * Time.fixedDeltaTime),
                    ForceMode.VelocityChange);

                // torque
                _rigidbody.AddTorque(-_rigidbody.angularVelocity * (_angularDrag * Time.fixedDeltaTime),
                    ForceMode.Impulse);
            }
        }

        private void OnDisable()
        {
            _rigidbody.useGravity = true;
        }

        private void OnDrawGizmos()
        {
            if (_effectorArray == null) return;

            for (int i = 0; i < _effectorArray.Length; i++)
                if (!Application.isPlaying && _effectorArray[i] != null)
                {
                    Gizmos.color = _green;
                    Gizmos.DrawSphere(_effectorArray[i].position, 0.06f);
                }

                else
                {
                    if (_effectorArray[i] == null) return;

                    Gizmos.color =
                        _effectorArray[i].position.y < _effectorProjections[i].y ? _red : _green; // submerged

                    Gizmos.DrawSphere(_effectorArray[i].position, 0.06f);

                    Gizmos.color = _orange;
                    Gizmos.DrawSphere(_effectorProjections[i], 0.06f);

                    Gizmos.color = _blue;
                    Gizmos.DrawLine(_effectorArray[i].position, _effectorProjections[i]);
                }
        }
    }
}