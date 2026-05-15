using UnityEngine;

namespace HanziZombieDefense.Core
{
    [DefaultExecutionOrder(-1000)]
    public class MobileSetup : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 60;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;
        }
    }
}
