using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChroMapper_UIPluginDesigner.Components
{
    public class InputNumberAdjuster : MonoBehaviour, IScrollHandler
    {
        public TMP_InputField InputField;
        public float Increment = 1.0f;

        private float _nextRepeatTime;
        private const float RepeatDelay = 0.5f;
        private const float RepeatRate = 0.1f;

        private void Update()
        {
            if (InputField == null || !InputField.isFocused) return;

            // Arrow Keys
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                AdjustValue(Increment);
                _nextRepeatTime = Time.unscaledTime + RepeatDelay;
            }
            else if (Input.GetKey(KeyCode.UpArrow) && Time.unscaledTime > _nextRepeatTime)
            {
                AdjustValue(Increment);
                _nextRepeatTime = Time.unscaledTime + RepeatRate;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                AdjustValue(-Increment);
                _nextRepeatTime = Time.unscaledTime + RepeatDelay;
            }
            else if (Input.GetKey(KeyCode.DownArrow) && Time.unscaledTime > _nextRepeatTime)
            {
                AdjustValue(-Increment);
                _nextRepeatTime = Time.unscaledTime + RepeatRate;
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (InputField == null || !InputField.isFocused) return;
            AdjustValue(eventData.scrollDelta.y * Increment);
        }

        private void AdjustValue(float delta)
        {
            if (float.TryParse(InputField.text, out float value))
            {
                value += delta;
                InputField.text = value.ToString();
                InputField.onValueChanged.Invoke(InputField.text);
            }
        }
    }
}
