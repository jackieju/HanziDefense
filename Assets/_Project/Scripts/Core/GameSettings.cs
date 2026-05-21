using UnityEngine;
using HanziZombieDefense.Hanzi.Recognition;

namespace HanziZombieDefense.Core
{
    public class GameSettings : MonoBehaviour
    {
        private const string RecognitionModeKey = "RecognitionMode";

        private static GameSettings _instance;
        public static GameSettings Instance => _instance;

        [SerializeField] private RecognitionMode recognitionMode = RecognitionMode.StrokeType;

        public RecognitionMode RecognitionMode
        {
            get => recognitionMode;
            set
            {
                recognitionMode = value;
                PlayerPrefs.SetInt(RecognitionModeKey, (int)value);
                PlayerPrefs.Save();
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (PlayerPrefs.HasKey(RecognitionModeKey))
                recognitionMode = (RecognitionMode)PlayerPrefs.GetInt(RecognitionModeKey);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
