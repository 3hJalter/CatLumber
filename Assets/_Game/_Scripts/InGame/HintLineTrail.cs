using System;
using System.Collections.Generic;
using _Game.Utilities;
using DG.Tweening;
using MEC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Game._Scripts.InGame
{
    public class HintLineTrail : HMonoBehaviour
    {
        [SerializeField] private ParticleSystem particleTrail;
        [ReadOnly]
        [SerializeField] private float moveSpeed = Constants.HINT_LINE_TRAIL_SPEED;
        [ReadOnly]
        [SerializeField] private Vector3 currentDestination;
        [ReadOnly]
        [SerializeField] private bool isMoving;
        [ReadOnly]
        [SerializeField] private bool isLooping;
        [ReadOnly]
        [SerializeField] private List<Vector3> path = new();
        private int _pathDestinationIndex;

        private void Update()
        {
            if (!isMoving || path.Count <= 0) return;
            // Move to destination
            transform.position = Vector3.MoveTowards(transform.position, currentDestination, moveSpeed * Time.deltaTime);
            // if reach to destination, set new destination
            if (!(Vector3.Distance(Tf.position, currentDestination) < 0.01f)) return;
            if (_pathDestinationIndex < path.Count - 1)
            {
                _pathDestinationIndex++;
                currentDestination = GetDestination(_pathDestinationIndex);
            }
            else
            {
                isMoving = false;
                if (!isLooping) return;
                // Start from the beginning
               Timing.RunCoroutine(OnLoop(), LOOP_TAG);
            }
        }

        private const string LOOP_TAG = "Loop";
        private IEnumerator<float> OnLoop(float timing = 1f)
        {
            yield return Timing.WaitForSeconds(timing);
            particleTrail.Stop();
            OnPlay(path, moveSpeed, isLooping);
        }
        
        public void OnPlay(List<Vector3> pathIn, float speed = -1, bool isLoop = false)
        {
            if (pathIn.Count == 0) return;
            isLooping = isLoop;
            path = pathIn;
            Tf.position = GetDestination(0);
            // Reset the trail
            moveSpeed = speed > 0 ? speed : Constants.HINT_LINE_TRAIL_SPEED;
            // particleTrail.Stop();
            // particleTrail.Clear();
            Timing.RunCoroutine(WaitOneFrameToStart());
            isMoving = true;
            _pathDestinationIndex = 1;
            currentDestination = GetDestination(_pathDestinationIndex);
        }

        private Vector3 GetDestination(int index)
        {
            return index > path.Count - 1 ? Vector3.zero : path[index];
        }
        
        public void OnCancel()
        {
            isMoving = false;
            if (isLooping)
            {
                isLooping = false;
                Timing.KillCoroutines(LOOP_TAG);
            }
            particleTrail.Stop();
        }
        
        // Wait one frame, then set the trail to emitting = true 
        private IEnumerator<float> WaitOneFrameToStart()
        {
            yield return Timing.WaitForOneFrame;
            particleTrail.Play();
        }
    }
}
