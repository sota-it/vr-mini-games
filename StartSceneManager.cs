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
        // SEを鳴らす
        PlayClickSE();
        // フェードアウトと遷移を開始
        StartCoroutine(StartGameSequence());
    }

    // --- チュートリアルボタン用 ---
    public void OnTutorialButtonClicked()
    {
        // SEを鳴らす
        PlayClickSE();
        // チュートリアルはすぐに遷移しても良いですが、
        // 少しだけ余韻（0.2秒ほど）を持たせるとSEが綺麗に聞こえます
        StartCoroutine(GoToTutorialSequence());
    }

    public void OnHnadTutorialButtonClicked()
    {
        // SEを鳴らす
        PlayClickSE();
        // チュートリアルはすぐに遷移しても良いですが、
        // 少しだけ余韻（0.2秒ほど）を持たせるとSEが綺麗に聞こえます
        StartCoroutine(GoToHandTutorialSequence());
    }

    public void OnDEBUGButtonClicked()
    {
        // SEを鳴らす
        PlayClickSE();
        // チュートリアルはすぐに遷移しても良いですが、
        // 少しだけ余韻（0.2秒ほど）を持たせるとSEが綺麗に聞こえます
        StartCoroutine(GoToDEBUGSequence());
    }

    // SEを鳴らす共通メソッド
    private void PlayClickSE()
    {
        if (bgmSource != null && buttonClickSE != null)
        {
            // PlayOneShotならBGMを止めずにSEを重ねて鳴らせる
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
        // SEが鳴る時間を少し待つ（一瞬でシーンが切り替わると音がブツ切れになるため）
        yield return new WaitForSeconds(0.2f);
        // チュートリアル用のシーン名に合わせて書き換えてください
        SceneManager.LoadScene("TutorialScene");
    }
    IEnumerator GoToHandTutorialSequence()
    {
        // SEが鳴る時間を少し待つ（一瞬でシーンが切り替わると音がブツ切れになるため）
        yield return new WaitForSeconds(0.2f);
        // チュートリアル用のシーン名に合わせて書き換えてください
        SceneManager.LoadScene("HandTutorialScene");
    }
    IEnumerator GoToDEBUGSequence()
    {
        // SEが鳴る時間を少し待つ（一瞬でシーンが切り替わると音がブツ切れになるため）
        yield return new WaitForSeconds(0.2f);
        // チュートリアル用のシーン名に合わせて書き換えてください
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