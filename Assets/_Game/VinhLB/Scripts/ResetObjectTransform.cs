using UnityEngine;

namespace VinhLB
{
    public class ResetObjectTransform : HMonoBehaviour
    {
        [SerializeField]
        private bool _position;
        [SerializeField]
        private bool _rotation;
        [SerializeField]
        private bool _scale;

        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Vector3 _startLocalScale;

        private void Start()
        {
            _startPosition = Tf.position;
            _startRotation = Tf.rotation;
            _startLocalScale = Tf.localScale;
        }

        private void LateUpdate()
        {
            if (_position)
            {
                Tf.position = _startPosition;
            }
            if (_rotation)
            {
                Tf.rotation = _startRotation;
            }
            if (_scale)
            {
                Tf.localScale = _startLocalScale;
            }
        }
    }
}