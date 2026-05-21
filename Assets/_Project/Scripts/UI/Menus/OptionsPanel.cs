using UnityEngine;
using UnityEngine.UI;
using HanziZombieDefense.Core;
using HanziZombieDefense.Hanzi.Recognition;

namespace HanziZombieDefense.UI.Menus
{
    public sealed class OptionsPanel : MonoBehaviour
    {
        [SerializeField] private Toggle strokeTypeToggle;
        [SerializeField] private Toggle shapeMatchToggle;

        private void OnEnable()
        {
            var mode = GameSettings.Instance != null
                ? GameSettings.Instance.RecognitionMode
                : RecognitionMode.StrokeType;

            if (strokeTypeToggle != null) strokeTypeToggle.isOn = (mode == RecognitionMode.StrokeType);
            if (shapeMatchToggle != null) shapeMatchToggle.isOn = (mode == RecognitionMode.ShapeMatch);

            if (strokeTypeToggle != null) strokeTypeToggle.onValueChanged.AddListener(OnStrokeTypeToggled);
            if (shapeMatchToggle != null) shapeMatchToggle.onValueChanged.AddListener(OnShapeMatchToggled);
        }

        private void OnDisable()
        {
            if (strokeTypeToggle != null) strokeTypeToggle.onValueChanged.RemoveListener(OnStrokeTypeToggled);
            if (shapeMatchToggle != null) shapeMatchToggle.onValueChanged.RemoveListener(OnShapeMatchToggled);
        }

        private void OnStrokeTypeToggled(bool isOn)
        {
            if (!isOn) return;
            if (GameSettings.Instance != null)
                GameSettings.Instance.RecognitionMode = RecognitionMode.StrokeType;
            if (shapeMatchToggle != null) shapeMatchToggle.isOn = false;
        }

        private void OnShapeMatchToggled(bool isOn)
        {
            if (!isOn) return;
            if (GameSettings.Instance != null)
                GameSettings.Instance.RecognitionMode = RecognitionMode.ShapeMatch;
            if (strokeTypeToggle != null) strokeTypeToggle.isOn = false;
        }
    }
}
