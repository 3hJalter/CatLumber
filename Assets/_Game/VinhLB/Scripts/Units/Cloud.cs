using System;
using _Game.DesignPattern;
using UnityEngine;

namespace VinhLB
{
    public class Cloud : EnvironmentObject
    {
        private float _speed = 2f;
        private Vector3 _direction;
        private Vector3 _endPosition;

        private void Update()
        {
            Tf.position += _direction * _speed * Time.deltaTime;
            if (Vector3.Distance(Tf.position, _endPosition) < 0.1f)
            {
                Despawn();
            }
        }
        
        public void Initialize(float speed, Vector3 direction, Vector3 endPosition)
        {
            _speed = speed;
            _direction = direction;
            _endPosition = endPosition;
        }

        private void Despawn()
        {
            SimplePool.Despawn(this);
        }
    }
}