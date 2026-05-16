using System.Collections;
using UnityEngine;
using AIRA.Character;
using AIRA.Voice;

namespace AIRA.UI
{
public class ExitGameManager : MonoBehaviour
{
    // Tombol exit diklik
    public void OnExitClicked()
    {
        StartCoroutine(ExitRoutine());
    }

    // Ucap goodbye lalu quit
    private IEnumerator ExitRoutine()
    {
        AiraController.Instance?.SetExpression("[SAD]");
        TTSManager.Instance?.Speak(
            "Goodbye! Come back and visit me soon, okay?",
            "SAD");
        yield return new WaitUntil(() =>
            TTSManager.Instance == null || !TTSManager.Instance.IsSpeaking);
        Application.Quit();
    }
}
}
