using System.Collections;
using System.Collections.Generic;
using _Game.Utilities;
using DG.Tweening;
using UnityEngine;

namespace VinhLB
{
    public class Fish : EnvironmentObject
    {
        private const float MIN_DISTANCE = 0.1f;

        [SerializeField]
        private float _heightOffset = 0f;
        [SerializeField]
        private float _pathMovementDuration = 30f;
        [SerializeField]
        private float _moveSpeed = 4f;
        [SerializeField]
        private float _rotateSpeed = 180f;

        private List<Vector3> _waypointList;
        private Tween _movementTween;

        public void Initialize(List<Vector3> waypointList)
        {
            _waypointList = waypointList;
            for (int i = 0; i < _waypointList.Count; i++)
            {
                if (i % 2 == 1)
                {
                    Vector3 waypointPosition = _waypointList[i];
                    waypointPosition.y += _heightOffset;
                    _waypointList[i] = waypointPosition;
                }
            }

            Move();
        }

        public void ResetMovement()
        {
            Move();
        }

        private void Move()
        {
            Tf.position = _waypointList[0];

            if (_movementTween != null)
            {
                _movementTween.Kill();
                _movementTween = null;
            }

            _movementTween = Tf.DOPath(_waypointList.ToArray(), _pathMovementDuration, PathType.CatmullRom,
                    PathMode.Full3D, 5)
                // .OnWaypointChange(OnWaypointChange)
                .SetOptions(true)
                .SetLookAt(0.01f)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }

        private void OnWaypointChange(int waypointIndex)
        {
            if (waypointIndex % 2 == 1)
            {
                float jumpDuration = 1f;
                float endJumpPathPercentage =
                    _movementTween.ElapsedDirectionalPercentage() + jumpDuration / _pathMovementDuration; 
                // DevLog.Log(DevId.Vinh, $"Start: {_movementTween.ElapsedDirectionalPercentage()}");
                // DevLog.Log(DevId.Vinh, $"End: {endJumpPathPercentage}");

                Vector3 endJumpPosition = _movementTween.PathGetPoint(endJumpPathPercentage);
                Vector3 lastJumpPosition = _movementTween.PathGetPoint(_movementTween.ElapsedDirectionalPercentage());
                Tween jumpTween = Tf.DOJump(endJumpPosition, 2f, 1, jumpDuration).OnUpdate(() =>
                {
                    // DevLog.Log(DevId.Vinh, $"TF: {Tf.position}");
                    // DevLog.Log(DevId.Vinh, $"LJP: {lastJumpPosition}");
                    Vector3 direction = (Tf.position - lastJumpPosition).normalized;
                    if (direction != Vector3.zero)
                    {
                        Tf.forward = direction;
                    }
                    lastJumpPosition = Tf.position;
                });
            }
        }
    }
}