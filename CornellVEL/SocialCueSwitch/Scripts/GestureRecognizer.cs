using System.Collections.Generic;
using UnityEngine;

namespace vellib
{
    public class GestureRecognizer : MonoBehaviour
    {
        public enum Gesture
        {
            None,
            Nodding,
            Shaking
        }

        public SocialCueSwitch socialCueSwitch = null;
        public Gesture CurrentGesture { get; private set; } = Gesture.None;

        public Transform headTransform; // The Transform component of the VR headset

        private Vector3 lastRotation;
        private List<Vector3> rotationDeltas = new List<Vector3>();
        private const float gestureRecognitionThreshold = 60f; // Degrees of rotation
        private const float gestureRecognitionTime = 1f; // Seconds over which to measure rotation
        private float timeSinceLastGestureCheck = 0f;

        private void Start()
        {
            // Get the MainCamera to use as the headTransform
            GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraObj != null)
            {
                headTransform = cameraObj.transform;
            }
            else
            {
                Debug.LogError("No GameObject found with tag 'MainCamera'.");
                return;
            }

            lastRotation = headTransform.localRotation.eulerAngles;
        }

        private void Update()
        {
            if (headTransform == null) return;

            // Record the change in rotation since the last frame
            var currentRotation = headTransform.localRotation.eulerAngles;
            rotationDeltas.Add(currentRotation - lastRotation);
            lastRotation = currentRotation;

            timeSinceLastGestureCheck += Time.deltaTime;

            if (timeSinceLastGestureCheck > gestureRecognitionTime)
            {
                RecognizeGesture();
                rotationDeltas.Clear();
                timeSinceLastGestureCheck = 0f;
            }
        }

        private void RecognizeGesture()
        {
            float xChange = 0f;
            float yChange = 0f;

            foreach (var rotationDelta in rotationDeltas)
            {
                xChange += Mathf.Abs(rotationDelta.x);
                yChange += Mathf.Abs(rotationDelta.y);
            }

            if (xChange > yChange && xChange > gestureRecognitionThreshold)
            {
                CurrentGesture = Gesture.Nodding;
                socialCueSwitch.PlayGestureAudio();
            }
            else if (yChange > xChange && yChange > gestureRecognitionThreshold)
            {
                CurrentGesture = Gesture.Shaking;
                socialCueSwitch.PlayGestureAudio();
            }
            else
            {
                CurrentGesture = Gesture.None;
            }
        }
    }
}
