using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ToonyColorsPro
{
    namespace Demo
    {
        public class TCP2_Demo_Interactive : MonoBehaviour
        {
            public TCP2_Demo_Camera demoCamera;
            public Camera camera;
            public Canvas canvas;
            public CanvasScaler canvasScaler;

            [Space] public HorizontalLayoutGroup layoutGroup;

            public ContentSizeFitter sizeFitter;

            [Space] public RectTransform textBox;

            public Text text;
            public Image line;

            [Space] public float camAnimDuration = 0.5f;

            public float maxCamAnimDuration = 1.0f;
            public bool camAnimBasedOnDistance;
            public float uiAnimDuration = 0.5f;

            [Space] public Button envButtonTemplate;

            public Text highlightLabel;

            public GameObject infoBox;

            private Vector3 cameraResetPos;
            private Quaternion cameraResetQuat;

            private TCP2_Demo_Interactive_Content[] contents;

            private bool coroutineActive;
            private TCP2_Demo_Interactive_Content currentContent;
            private Color envButtonColor;
            private Button[] envButtons;
            private int index = -1;
            private int lightingIndex;

            private TCP2_Demo_Interactive_Environment[] lightings;
            private Transform resetPivot;

            private void Awake()
            {
                contents = GetComponentsInChildren<TCP2_Demo_Interactive_Content>();
                lightings = GetComponentsInChildren<TCP2_Demo_Interactive_Environment>(true);

                if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
                {
                    foreach (TCP2_Demo_Interactive_Environment lighting in lightings)
                    {
                        Light[] lights = lighting.GetComponentsInChildren<Light>();
                        foreach (Light light in lights) light.intensity = light.intensity * 0.6f;
                    }

                    RenderSettings.ambientIntensity = 0.6f;
                    RenderSettings.reflectionIntensity = 0.6f;
                }

                envButtonColor = envButtonTemplate.GetComponent<Image>().color;
                envButtons = new Button[lightings.Length];
                for (int i = 0; i < lightings.Length; i++)
                {
                    GameObject btnGo = Instantiate(envButtonTemplate.gameObject);
                    btnGo.name = envButtonTemplate.name + "_" + i;
                    btnGo.transform.SetParent(envButtonTemplate.transform.parent);
                    btnGo.transform.SetSiblingIndex(envButtonTemplate.transform.GetSiblingIndex());

                    Text text = btnGo.GetComponentInChildren<Text>();
                    text.text = lightings[i].name;

                    Button btn = btnGo.GetComponent<Button>();
                    int ci = i;
                    btn.onClick.AddListener(() => { OnSelectLightingSettings(ci); });

                    envButtons[i] = btn;
                }

                envButtonTemplate.gameObject.SetActive(false);
                OnSelectLightingSettings(0);

                cameraResetPos = camera.transform.position;
                cameraResetQuat = camera.transform.rotation;
                resetPivot = demoCamera.Pivot;

            }

            private void LateUpdate()
            {
                HandleKeyboard();

                if (index >= 0 && !coroutineActive) UpdateViewToCurrentContent();
            }

            private void HandleKeyboard()
            {
                if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.H)) canvas.enabled = !canvas.enabled;

                if (Input.GetKeyDown(KeyCode.Escape)) ResetView();

                if (Input.GetKeyDown(KeyCode.RightArrow)) NextHighlight();

                if (Input.GetKeyDown(KeyCode.LeftArrow)) PrevHighlight();

                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        lightingIndex--;
                        if (lightingIndex < 0) lightingIndex = envButtons.Length - 1;
                    }
                    else
                    {
                        lightingIndex++;
                        if (lightingIndex >= envButtons.Length) lightingIndex = 0;
                    }

                    OnSelectLightingSettings(lightingIndex);
                }
            }

            public void PrevHighlight()
            {
                index--;
                if (index < 0) index = contents.Length - 1;

                StopAllCoroutines();
                StartCoroutine(CR_MoveToContent(contents[index]));

                highlightLabel.text = contents[index].name;
            }

            public void NextHighlight()
            {
                index++;
                if (index >= contents.Length) index = 0;

                StopAllCoroutines();
                StartCoroutine(CR_MoveToContent(contents[index]));

                highlightLabel.text = contents[index].name;
            }

            private void UpdateViewToCurrentContent(float lengthPercent = 1.0f)
            {
                Vector3 screenPos = camera.WorldToScreenPoint(currentContent.pivot.position);
                Vector3 endPos = camera.WorldToScreenPoint(currentContent.textBox.position);
                textBox.position = endPos;

                float w2 = textBox.rect.width / 2.0f;
                float h2 = textBox.rect.height / 2.0f;

                // check if text box exceeds screen bounds
                if (endPos.x - w2 < 0) endPos.x = w2;
                if (endPos.x + w2 > Screen.width) endPos.x = Screen.width - w2;

                if (endPos.y - h2 < 0) endPos.y = h2;
                if (endPos.y + h2 > Screen.height) endPos.y = Screen.height - h2;

                textBox.position = endPos;

                PlaceLine(endPos, screenPos, lengthPercent);
            }

            private void PlaceLine(Vector2 start, Vector2 end, float lengthPercentage)
            {
                line.rectTransform.position = start;

                start.y = -start.y;
                end.y = -end.y;
                float angle = Vector2.SignedAngle((start - end).normalized, Vector2.up);
                Vector3 r = line.rectTransform.localEulerAngles;
                r.z = angle;
                line.rectTransform.localEulerAngles = r;

                // Need to find canvas ratio according to resolution for proper distance calculation
                float ratio = canvasScaler.referenceResolution.x / Screen.width;
                float dist = Vector2.Distance(start, end) * lengthPercentage;
                dist *= ratio;
                Vector2 sd = line.rectTransform.sizeDelta;
                sd.y = dist;
                line.rectTransform.sizeDelta = sd;
            }

            private void ResetView()
            {
                canvas.enabled = false;
                StopAllCoroutines();
                StartCoroutine(CR_ResetCamPos());

                highlightLabel.text = "...";
            }

            private IEnumerator CR_ResetCamPos()
            {
                // --------------------------------
                // Animate Camera

                demoCamera.Pivot = resetPivot;
                demoCamera.pivotOffset = Vector3.zero;

                Vector3 startPos = camera.transform.position;
                Vector3 endPos = cameraResetPos;
                Quaternion startQuat = camera.transform.rotation;
                Quaternion endQuat = cameraResetQuat;

                float duration = camAnimBasedOnDistance
                    ? Vector3.Distance(startPos, endPos) * camAnimDuration
                    : camAnimDuration;
                duration = Mathf.Min(duration, maxCamAnimDuration);
                float time = duration;

                while (time > 0)
                {
                    time -= Time.deltaTime;
                    yield return null;

                    float delta = Mathf.SmoothStep(0, 1, 1 - Mathf.Clamp01(time / duration));
                    camera.transform.position = Vector3.Lerp(startPos, endPos, delta);
                    camera.transform.rotation = Quaternion.Slerp(startQuat, endQuat, delta);
                }
            }

            private IEnumerator CR_MoveToContent(TCP2_Demo_Interactive_Content content)
            {
                coroutineActive = true;

                // Hide UI
                canvas.enabled = false;

                // --------------------------------
                // Animate Camera

                Vector3 startPos = camera.transform.position;
                Vector3 endPos = content.transform.position;
                Quaternion startQuat = camera.transform.rotation;
                Quaternion endQuat = content.transform.rotation;

                float duration = camAnimBasedOnDistance
                    ? Vector3.Distance(startPos, endPos) * camAnimDuration
                    : camAnimDuration;
                duration = Mathf.Min(duration, maxCamAnimDuration);
                float time = duration;

                while (time > 0)
                {
                    time -= Time.deltaTime;
                    yield return null;

                    float delta = Mathf.SmoothStep(0, 1, 1 - Mathf.Clamp01(time / duration));
                    camera.transform.position = Vector3.Lerp(startPos, endPos, delta);
                    camera.transform.rotation = Quaternion.Slerp(startQuat, endQuat, delta);
                }

                currentContent = contents[index];
                camera.transform.position = currentContent.transform.position;
                camera.transform.rotation = currentContent.transform.rotation;
                demoCamera.Pivot = currentContent.pivot;
                demoCamera.pivotOffset = Vector3.zero;
                text.text = currentContent.Text;

                UpdateViewToCurrentContent(0f);
                yield return null;
                UpdateViewToCurrentContent(0f);
                yield return null;
                UpdateViewToCurrentContent(0f);

                // --------------------------------

                // Resize the text box according to its content
                layoutGroup.enabled = true;
                sizeFitter.enabled = true;

                yield return null;

                // Disable text box resizing as content won't change
                layoutGroup.enabled = false;
                sizeFitter.enabled = false;

                // Show UI
                canvas.enabled = true;

                // --------------------------------
                // Animate UI

                textBox.localScale = Vector3.zero;

                duration = uiAnimDuration;
                time = duration;
                while (time > 0)
                {
                    time -= Time.deltaTime;
                    yield return null;

                    float delta = Mathf.SmoothStep(0, 1, 1 - Mathf.Clamp01(time / duration));

                    // Text box
                    textBox.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, delta);
                    line.rectTransform.localScale = new Vector3(1 / textBox.localScale.x, 1 / textBox.localScale.y, 1);

                    // Line length + repositioning
                    UpdateViewToCurrentContent(delta);
                }

                // --------------------------------

                coroutineActive = false;
            }

            private void OnSelectLightingSettings(int index)
            {
                lightingIndex = index;
                lightings[index].ApplyEnvironment();

                for (int i = 0; i < envButtons.Length; i++)
                    envButtons[i].GetComponent<Image>().color =
                        i == index ? new Color(0.6f, 0.2f, 0.0f) : envButtonColor;
            }

            public void HideInfoBox()
            {
                infoBox.SetActive(false);
            }
        }
    }
}
