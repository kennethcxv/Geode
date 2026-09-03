using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeodeEmpire.Core
{
    /// <summary>
    /// Verification hook for standalone builds: `-geode-capture <png>` writes a screenshot after `-geode-delay <s>`
    /// (default 6); `-geode-continue` presses Continue first so the workshop loads from the real save; `-geode-quit`
    /// exits afterwards. Without the arguments nothing is created.
    /// </summary>
    public sealed class BootCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            string path = Arg(args, "-geode-capture");
            if (string.IsNullOrEmpty(path)) return;
            var go = new GameObject("_BootCapture");
            DontDestroyOnLoad(go);
            var bc = go.AddComponent<BootCapture>();
            bc._path = path;
            bc._delay = float.TryParse(Arg(args, "-geode-delay"), out var d) ? d : 6f;
            bc._continue = Array.IndexOf(args, "-geode-continue") >= 0;
            bc._quit = Array.IndexOf(args, "-geode-quit") >= 0;
        }

        private static string Arg(string[] args, string key)
        {
            int i = Array.IndexOf(args, key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private string _path;
        private float _delay;
        private bool _continue, _quit;

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(3f);
            if (_continue && Save.SaveSystem.Exists())
            {
                GameSession.PendingStart = SessionStartMode.Continue;
                SceneManager.LoadScene("Workshop");
            }
            yield return new WaitForSecondsRealtime(_delay);
            string dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            ScreenCapture.CaptureScreenshot(_path);
            Debug.Log($"[BootCapture] scene={SceneManager.GetActiveScene().name} -> {_path} screen={Screen.width}x{Screen.height} {Screen.fullScreenMode} applied={GameSettings.DisplayApplied}");
            yield return new WaitForSecondsRealtime(1.5f);
            if (_quit) Application.Quit();
        }
    }
}
