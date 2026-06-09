using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartSceneManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource bgmSource;
    public AudioClip startBGM;
    public AudioClip buttonClickSE; // ★追加：ボタンを押した時の効果音
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    void Start()
    {
        if (bgmSource != null && startBGM != null)
        {
            bgmSource.clip = startBGM;
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }
    }

    // --- STARTボタン用 ---
    public void OnStartButtonClicked()
    {
        PlayClickSE();
        StartCoroutine(StartGameSequence());
    }

    // --- チュートリアルボタン用 ---
    public void OnTutorialButtonClicked()
    {
        PlayClickSE();
        StartCoroutine(GoToTutorialSequence());
    }

    public void OnHnadTutorialButtonClicked()
    {
        PlayClickSE();
        StartCoroutine(GoToHandTutorialSequence());
    }

    public void OnDEBUGButtonClicked()
    {
        PlayClickSE();
        StartCoroutine(GoToDEBUGSequence());
    }

    // SEを鳴らす共通メソッド
    private void PlayClickSE()
    {
        if (bgmSource != null && buttonClickSE != null)
        {
            bgmSource.PlayOneShot(buttonClickSE);
        }
    }

    IEnumerator StartGameSequence()
    {
        GameData.SuccessCount = 0;
        GameData.Lives = 3;
        GameData.isLastGameSuccess = true;

        // BGMをフェードアウト
        yield return StartCoroutine(FadeOutBGM(1.0f));
        SceneManager.LoadScene("HomeScene");
    }

    IEnumerator GoToTutorialSequence()
    {
        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadScene("TutorialScene");
    }
    IEnumerator GoToHandTutorialSequence()
    {
        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadScene("HandTutorialScene");
    }
    IEnumerator GoToDEBUGSequence()
    {
        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadScene("BalloonScene");
    }

    IEnumerator FadeOutBGM(float duration)
    {
        if (bgmSource == null) yield break;
        float startVolume = bgmSource.volume;
        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= startVolume * Time.deltaTime / duration;
            yield return null;
        }
        bgmSource.Stop();
    }
}
