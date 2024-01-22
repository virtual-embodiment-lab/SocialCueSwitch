using Physics = UnityEngine.Physics;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System.Collections;
using static vellib.GestureRecognizer;

namespace vellib
{
    public class SocialCueSwitch : MonoBehaviour
    {
        [System.Serializable]
        public class ProximityToneConfig
        {
            public AudioClip toneClip;
            public float proximityThreshold = 5.0f;
            public float volumeIncreaseThreshold = 2.0f;
            public float maxVolume = 1.0f;
        }
        [System.Serializable]
        public class ToneConfig
        {
            public AudioClip toneClip;
            public float volumeIncreaseThreshold = 2.0f;
            public float maxVolume = 1.0f;
        }
        [System.Serializable]
        public class RaycastInfo
        {
            public SocialCueSwitch otherObject;
            public Vector3 vector;
            public float distance;

            public RaycastInfo(SocialCueSwitch otherObject, Vector3 vector, float distance)
            {
                this.otherObject = otherObject;
                this.vector = vector;
                this.distance = distance;
            }
        }
        [System.Serializable]
        public class AudioConfig
        {
            public bool microphoneOn = true;
            [SerializeField] private float[] audioSamples = new float[1024];
            [SerializeField] private float volume;
            public float volumeSensitivity = 100.0f;

            public float Volume => volume * volumeSensitivity;
            public float[] AudioSamples => audioSamples;

            public float UpdateVolume(AudioSource audioSource)
            {
                audioSource.GetOutputData(audioSamples, 0);
                float sum = 0;
                for (int i = 0; i < audioSamples.Length; i++)
                {
                    sum += audioSamples[i] * audioSamples[i];
                }
                return volume = Mathf.Sqrt(sum / audioSamples.Length) * volumeSensitivity;
            }
        }
        [System.Serializable]
        public class CaptionSettings
        {
            public float displayTime = 3.0f;  // Time in seconds each caption is shown
            public Queue<string> captionQueue = new Queue<string>();
            public bool isDisplaying = false;
            public Color color = Color.black;
            public TMP_FontAsset font; // Changed from Font to TMP_FontAsset
            public float fontSize = 0.06f;
            public Dictionary<string, int> captionFrameCounters = new Dictionary<string, int>();
            public int blockFrameCount = 10; // the number of frames to block for, adjust as needed
        }

        [Header("Settings")]
        public bool localOwnership = false;
        public float raycastDistance = 100f;
        public bool showCaption = true;
        public bool showArrow = true;

        [Header("Tone Configurations")]
        public ProximityToneConfig proximityToneConfig;
        private AudioSource proximityToneSource;
        public ToneConfig observationToneConfig;
        private AudioSource observationToneSource;
        public ToneConfig gesturesToneConfig;
        private AudioSource gesturesToneSource;

        [Header("Avatar Configurations")]
        public SocialCueSwitch[] allObjects = null;
        public List<SocialCueSwitch> observingAvatars = new List<SocialCueSwitch>();
        public List<SocialCueSwitch> nearbyAvatars = new List<SocialCueSwitch>();
        public AudioConfig audioConfig = new AudioConfig();
        public CaptionSettings captionSettings = new CaptionSettings();
        public List<RaycastInfo> raycastInfos = new List<RaycastInfo>();

        [Header("Targets")]
        public Transform bodyTarget;
        public SocialCueSwitch cameraLookAtObject = null;

        [Header("Arrow Configurations")]
        public GameObject arrowPrefab;
        public Vector3 arrowRotationOffset = Vector3.zero;
        public Vector3 arrowPositionOffset = Vector3.zero;
        private GameObject arrowInstance;

        [Header("Other Objects")]
        public SocialCueSwitch talkingObject;
        private AudioSource audioSource;
        private TextMeshProUGUI captionText;
        private GameObject captionBackground;
        public GameObject captionCanvas = null;
        public GameObject captionTextObj = null;
        public GestureRecognizer gestureRecognizer = null;
        public Outline outline = null;
        public Gesture currentGesture = Gesture.None;
        [SerializeField] private bool directGazing = false;

        private void Start()
        {
            InitializeAudioSource();
            InitializeArrow();
            InitializeUI();
            ApplyGlowOutline();
            InitializeGestureRecognizer();
            InitializeProximityToneSource();
            InitializeObservationToneSource();
            InitializeGesturesToneSource();
        }

        private void Update()
        {
            GetAllObjects();
            ClearObservingAvatars();
            ClearRaycastInfo();
            UpdateAudioSourceMute();
            UpdateOutlineWidth();
            ManageTalkingObjectAndArrow();
            UpdateCaption();
            DetectAndAddObservingAvatars();
            UpdateProximityTone();
            UpdateObservationTone();
            DetectGestures();
            UpdateCaptionFrameCounters();
        }

        private void InitializeAudioSource()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = Microphone.Start(null, true, 10, AudioSettings.outputSampleRate);
            audioSource.loop = true;
            audioSource.mute = !audioConfig.microphoneOn;
            while (!(Microphone.GetPosition(null) > 0)) { }
            audioSource.Play();
        }

        private void InitializeGesturesToneSource()
        {
            gesturesToneSource = gameObject.AddComponent<AudioSource>();
            gesturesToneSource.clip = gesturesToneConfig.toneClip;
            gesturesToneSource.volume = 0f;
        }

        private void InitializeProximityToneSource()
        {
            proximityToneSource = gameObject.AddComponent<AudioSource>();
            proximityToneSource.clip = proximityToneConfig.toneClip;
            proximityToneSource.loop = true;
            proximityToneSource.volume = 0f;
            proximityToneSource.Play();
        }

        private void InitializeObservationToneSource()
        {
            observationToneSource = gameObject.AddComponent<AudioSource>();
            observationToneSource.clip = observationToneConfig.toneClip;
            observationToneSource.loop = true;
            observationToneSource.volume = 0f;
            observationToneSource.Play();
        }

        private void InitializeArrow()
        {
            if (!localOwnership) return;
            arrowInstance = Instantiate(arrowPrefab, Camera.main.transform.position + Camera.main.transform.forward * 2f, Quaternion.identity);
            arrowInstance.transform.SetParent(transform);
            arrowInstance.SetActive(false);
        }

        private void UpdateAudioSourceMute()
        {
            audioSource.mute = !audioConfig.microphoneOn;
        }

        private void ClearRaycastInfo()
        {
            raycastInfos.Clear();
            cameraLookAtObject = null;
        }

        private void UpdateOutlineWidth()
        {
            if (localOwnership) return;
            if (!audioConfig.microphoneOn) outline.OutlineWidth = 0;
            else outline.OutlineWidth = audioConfig.UpdateVolume(audioSource);
        }

        private void UpdateObservationTone()
        {
            if (!localOwnership) return;
            if (observingAvatars.Count > 0)
            {
                observationToneSource.volume = observingAvatars.Count < allObjects.Length ? observationToneConfig.maxVolume : observationToneConfig.maxVolume * (observingAvatars.Count / allObjects.Length);

                observationToneSource.pitch = 1 + observingAvatars.Count * 0.1f;

                foreach (var observing in observingAvatars)
                {
                    AddCaption(observing.name, "eye contact");
                }
            }
            else
            {
                observationToneSource.volume = 0f;
            }
        }

        private void UpdateProximityTone()
        {
            nearbyAvatars = allObjects.Where(avatar => avatar != this).Where(avatar => Vector3.Distance(transform.position, avatar.transform.position) < proximityToneConfig.proximityThreshold).ToList();

            if (!localOwnership) return;
            if (nearbyAvatars.Count > 0)
            {
                float closestAvatarDistance = nearbyAvatars.Min(avatar =>
                    Vector3.Distance(transform.position, avatar.transform.position));

                proximityToneSource.volume = closestAvatarDistance < proximityToneConfig.volumeIncreaseThreshold ?
                    proximityToneConfig.maxVolume : proximityToneConfig.maxVolume * (proximityToneConfig.proximityThreshold - closestAvatarDistance) / (proximityToneConfig.proximityThreshold - proximityToneConfig.volumeIncreaseThreshold);

                proximityToneSource.pitch = 1 + nearbyAvatars.Count * 0.1f; // Increase pitch based on number of nearby avatars

                foreach (var nearby in nearbyAvatars)
                {
                    //AddCaption(nearby.name, "close proximity");
                }
            }
            else
            {
                proximityToneSource.volume = 0f;
            }
        }

        private void GetAllObjects()
        {
            allObjects = FindObjectsOfType<SocialCueSwitch>();
        }

        private void ClearObservingAvatars()
        {
            observingAvatars.Clear();

            foreach (SocialCueSwitch otherObject in allObjects)
            {
                if (otherObject.observingAvatars.Contains(this))
                {
                    otherObject.observingAvatars.Remove(this);
                }
            }
        }

        private void DetectAndAddObservingAvatars()
        {
            foreach (SocialCueSwitch otherObject in allObjects)
            {
                if (otherObject == this) continue;
                if (bodyTarget != null && otherObject.bodyTarget != null)
                {
                    PerformRaycast(bodyTarget.position, otherObject.bodyTarget.position - bodyTarget.position, otherObject);
                }
                PerformRaycastFromCamera(Camera.main.transform.position, Camera.main.transform.forward);
            }
        }

        public void PlayGestureAudio()
        {
            if (!localOwnership) return;
            gesturesToneSource.volume = gesturesToneConfig.maxVolume;
            gesturesToneSource.Play();
        }

        private void DetectGestures()
        {
            if (!localOwnership) return;
            //currentGesture = gestureRecognizer.CurrentGesture;

            foreach (SocialCueSwitch otherObject in nearbyAvatars)
            {
                if (otherObject.currentGesture != Gesture.None)
                {
                    AddCaption(otherObject.name, otherObject.currentGesture.ToString());
                }
            }
        }

        private void UpdateCaptionFrameCounters()
        {
            // Reduce the remaining block frames by one for all captions
            var keys = new List<string>(captionSettings.captionFrameCounters.Keys);
            foreach (var key in keys)
            {
                captionSettings.captionFrameCounters[key]--;
                if (captionSettings.captionFrameCounters[key] <= 0)
                {
                    captionSettings.captionFrameCounters.Remove(key);
                }
            }
        }

        private void PerformRaycast(Vector3 startPos, Vector3 direction, SocialCueSwitch otherObject)
        {
            RaycastHit hit;
            if (Physics.Raycast(startPos, direction.normalized, out hit, raycastDistance))
            {
                if (hit.transform.gameObject.GetComponent<SocialCueSwitch>() == otherObject)
                {
                    if (!raycastInfos.Exists(info => info.otherObject == otherObject))
                    {
                        raycastInfos.Add(new RaycastInfo(otherObject, direction, hit.distance));
                    }
                }
            }
        }

        private void PerformRaycastFromCamera(Vector3 startPos, Vector3 direction)
        {
            RaycastHit hit;
            if (Physics.Raycast(startPos, direction, out hit, raycastDistance))
            {
                SocialCueSwitch hitObject = hit.transform.gameObject.GetComponent<SocialCueSwitch>();
                if (hitObject != null)
                {
                    if (!raycastInfos.Exists(info => info.otherObject == hitObject))
                    {
                        cameraLookAtObject = hitObject;
                        raycastInfos.Add(new RaycastInfo(hitObject, direction, hit.distance));
                    }

                    if (!hitObject.observingAvatars.Contains(this))
                    {
                        hitObject.observingAvatars.Add(this);
                    }
                }
            }
        }

        public void SetDirectLook(bool value)
        {
            directGazing = value;
        }

        private void UpdateCaption()
        {
            if (!localOwnership) return;
            captionBackground.SetActive(showCaption);
            captionText.gameObject.SetActive(showCaption);
        }

        public void AddCaption(string name, string cue)
        {
            string caption = name + ": " + cue;

            if (captionSettings.captionFrameCounters.ContainsKey(caption) && captionSettings.captionFrameCounters[caption] > 0)
            {
                // Reduce the remaining block frames by one for this caption
                captionSettings.captionFrameCounters[caption]--;
                return;
            }

            // Otherwise add to queue and set block frames for this caption
            captionSettings.captionQueue.Enqueue(caption);
            captionSettings.captionFrameCounters[caption] = captionSettings.blockFrameCount;

            if (!captionSettings.isDisplaying)
                StartCoroutine(ShowCaptions());
        }

        private IEnumerator ShowCaptions()
        {
            while (captionSettings.captionQueue.Count > 0)
            {
                captionSettings.isDisplaying = true;

                string caption = captionSettings.captionQueue.Dequeue();
                captionText.text = caption;

                yield return new WaitForSeconds(captionSettings.displayTime);
            }

            captionText.text = "";
            captionSettings.isDisplaying = false;
        }

        private void ManageTalkingObjectAndArrow()
        {
            if (!localOwnership) return;
            arrowInstance.SetActive(showArrow);
            if (!showArrow) return;

            float maxVolume = 0f;
            foreach (var obj in allObjects)
            {
                if (obj.audioConfig.Volume > maxVolume)
                {
                    talkingObject = obj;
                    maxVolume = obj.audioConfig.Volume;
                }
            }
            if (talkingObject != null)
            {
                Vector3 relativePos = transform.InverseTransformPoint(Camera.main.transform.position + Camera.main.transform.forward * 2f + arrowPositionOffset);
                arrowInstance.transform.position = transform.TransformPoint(relativePos);
                arrowInstance.transform.LookAt(transform.TransformPoint(transform.InverseTransformPoint(talkingObject.transform.position)));
                arrowInstance.transform.Rotate(arrowRotationOffset, Space.Self);
                arrowInstance.SetActive(true);
            }
            else
            {
                arrowInstance.SetActive(false);
            }
        }

        void ApplyGlowOutline()
        {
            if (localOwnership) return;
            outline = gameObject.AddComponent<Outline>();
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = Color.yellow;
        }

        void InitializeGestureRecognizer()
        {
            if (!localOwnership) return;
            gestureRecognizer = gameObject.AddComponent<GestureRecognizer>();
            gestureRecognizer.socialCueSwitch = this;
        }

        private void InitializeUI()
        {
            if (!localOwnership) return;

            int UILayer = LayerMask.NameToLayer("UI");

            // Create the canvas
            captionCanvas = new GameObject("Caption Canvas");
            captionCanvas.layer = UILayer;
            Canvas canvas = captionCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            RectTransform canvasRect = captionCanvas.GetComponent<RectTransform>();
            canvasRect.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            canvasRect.sizeDelta = new Vector2(0.6f, 0.15f);

            // Attach the canvas to the camera and adjust its position
            captionCanvas.transform.SetParent(Camera.main.transform);
            captionCanvas.transform.localPosition = new Vector3(0.0f, -0.2f, 1f);

            // Create the caption background
            captionBackground = new GameObject("Caption Background");
            captionBackground.layer = UILayer;
            captionBackground.transform.parent = captionCanvas.transform;
            RectTransform canvasRectTransform = captionBackground.AddComponent<RectTransform>();
            canvasRectTransform.pivot = new Vector2(0.5f, 0.5f);
            canvasRectTransform.anchorMin = Vector2.zero; // Lower left corner
            canvasRectTransform.anchorMax = Vector2.one; // Upper right corner
            canvasRectTransform.offsetMin = Vector2.zero;
            canvasRectTransform.offsetMax = Vector2.zero;
            canvasRectTransform.anchoredPosition = Vector2.zero; // Position to parent center
            canvasRectTransform.sizeDelta = new Vector2(0f, 0f);
            canvasRectTransform.localPosition = new Vector3(0, 0, 0);
            Image bgImage = captionBackground.AddComponent<Image>();
            bgImage.color = Color.white;
            bgImage.raycastTarget = false;

            // Create the caption text
            captionTextObj = new GameObject("Caption");
            captionTextObj.layer = UILayer;
            captionTextObj.transform.parent = captionBackground.transform;
            RectTransform textRectTransform = captionTextObj.AddComponent<RectTransform>();
            textRectTransform.pivot = new Vector2(0.5f, 0.5f);
            textRectTransform.anchorMin = Vector2.zero; // Lower left corner
            textRectTransform.anchorMax = Vector2.one; // Upper right corner
            textRectTransform.offsetMin = Vector2.zero;
            textRectTransform.offsetMax = Vector2.zero;
            textRectTransform.anchoredPosition = Vector2.zero; // Position to parent center
            textRectTransform.sizeDelta = new Vector2(0f, 0f);
            textRectTransform.localPosition = new Vector3(0, 0, 0);
            captionText = captionTextObj.AddComponent<TextMeshProUGUI>();
            captionText.text = "";
            captionText.color = captionSettings.color;
            captionText.font = captionSettings.font;
            captionText.fontSize = captionSettings.fontSize;
            captionText.enableAutoSizing = false;
            captionText.alignment = TextAlignmentOptions.Center;
        }
    }
}
