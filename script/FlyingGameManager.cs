using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class FlyingGameManager : MonoBehaviour
{
    [Header("Game Elements")]
    public Rigidbody playerRigidbody;
    public TextMeshProUGUI statusText;
    public Transform leftHand;
    public Transform rightHand;
    public Transform playerHead;

    [Header("Flight Settings (Jump Only)")]
    public float flapStrength = 18.0f;
    public float maxUpwardVelocity = 5.0f;
    public float maxDownwardVelocity = -8.0f;
    public float constantForwardSpeed = 8.0f;
    public float minFlapVelocity = 0.6f;

    [Header("Audio Settings (BGM)")]
    public AudioSource bgmSource;
    public AudioClip flightBGM;
    [Range(0f, 1f)] public float bgmVolume = 0.3f;

    [Header("Audio Settings (SE/Voice)")]
    public AudioSource seSource;
    public AudioClip voice_Start;    // 「はばたけなのだ！」
    public AudioClip se_Success;     // シャキーン！などの正解音
    public AudioClip se_Failure;     // ドボーン！などの失敗音

    private bool isGameOver = false;
    private bool isReady = false;
    private Vector3 prevLeftPos;
    private Vector3 prevRightPos;

    void Start()
    {
        // BGMの設定と再生
        if (bgmSource != null && flightBGM != null)
        {
            bgmSource.clip = flightBGM;
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }

        // SE用Sourceがない場合の自動生成
        if (seSource == null) seSource = gameObject.AddComponent<AudioSource>();

        SetupGame();
        StartCoroutine(StartSequence());
    }

    void SetupGame()
    {
        isGameOver = false;
        isReady = false;
        if (playerRigidbody == null) playerRigidbody = GetComponent<Rigidbody>();

        playerRigidbody.isKinematic = true;
        playerRigidbody.useGravity = true;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        prevLeftPos = leftHand.localPosition;
        prevRightPos = rightHand.localPosition;
    }

    IEnumerator StartSequence()
    {
        // --- 1. 開始ボイス演出 ---
        if (statusText != null) statusText.text = "はばたけ！";

        if (seSource != null && voice_Start != null)
        {
            seSource.PlayOneShot(voice_Start);
            // ボイスが終わるまで少し待機（ボイスの長さ + 少しの余裕）
            yield return new WaitForSeconds(voice_Start.length);
        }

        // --- 2. 準備カウントダウン ---
        float timer = 0;
        while (timer < 2.0f)
        {
            prevLeftPos = leftHand.localPosition;
            prevRightPos = rightHand.localPosition;
            timer += Time.deltaTime;
            if (statusText != null) statusText.text = $"READY... {(2.0f - timer).ToString("F1")}";
            yield return null;
        }

        playerRigidbody.isKinematic = false;
        playerRigidbody.linearVelocity = Vector3.zero;
        isReady = true;
        if (statusText != null) statusText.text = "GO!!";

        // 1秒後に文字を消す、あるいは飛行を邪魔しない程度に薄くする
        yield return new WaitForSeconds(1.0f);
        if (!isGameOver && statusText != null) statusText.text = "";
    }

    void FixedUpdate()
    {
        if (isGameOver || !isReady) return;
        HandleJumpPhysics();
    }

    void HandleJumpPhysics()
    {
        float leftV = (prevLeftPos.y - leftHand.localPosition.y) / Time.fixedDeltaTime;
        float rightV = (prevRightPos.y - rightHand.localPosition.y) / Time.fixedDeltaTime;

        if (leftV > minFlapVelocity && rightV > minFlapVelocity)
        {
            if (playerRigidbody.linearVelocity.y < maxUpwardVelocity)
            {
                if (playerRigidbody.linearVelocity.y < 0)
                {
                    Vector3 v = playerRigidbody.linearVelocity;
                    v.y = 0;
                    playerRigidbody.linearVelocity = v;
                }
                playerRigidbody.AddForce(Vector3.up * flapStrength, ForceMode.VelocityChange);
            }
        }

        Vector3 forwardVel = Vector3.forward * constantForwardSpeed;
        forwardVel.y = playerRigidbody.linearVelocity.y;
        playerRigidbody.linearVelocity = forwardVel;

        Vector3 clampedVel = playerRigidbody.linearVelocity;
        clampedVel.y = Mathf.Clamp(clampedVel.y, maxDownwardVelocity, maxUpwardVelocity);
        playerRigidbody.linearVelocity = clampedVel;

        prevLeftPos = leftHand.localPosition;
        prevRightPos = rightHand.localPosition;
    }

    void OnCollisionEnter(Collision collision) => ProcessHit(collision.gameObject);
    void OnTriggerEnter(Collider other) => ProcessHit(other.gameObject);

    void ProcessHit(GameObject hitObject)
    {
        if (isGameOver) return;

        if (hitObject.CompareTag("Sea") || hitObject.CompareTag("Obstacle"))
        {
            StartCoroutine(EndGameCoroutine(false));
        }
        else if (hitObject.CompareTag("Goal"))
        {
            StartCoroutine(EndGameCoroutine(true));
        }
    }

    // コルーチンに変更してBGMフェードアウトやボイス待機を可能にする
    IEnumerator EndGameCoroutine(bool isSuccess)
    {
        isGameOver = true;
        playerRigidbody.isKinematic = true;
        playerRigidbody.linearVelocity = Vector3.zero;

        if (isSuccess)
        {
            if (seSource != null && se_Success != null) seSource.PlayOneShot(se_Success);

            GameData.SuccessCount++;
            GameData.isLastGameSuccess = true;
            if (statusText != null) statusText.text = "SUCCESS!";
        }
        else
        {
            if (seSource != null && se_Failure != null) seSource.PlayOneShot(se_Failure);

            GameData.Lives--;
            GameData.isLastGameSuccess = false;
            if (statusText != null) statusText.text = "FAILED...";
        }

        // BGMをフェードアウトさせて余韻を作る
        yield return StartCoroutine(FadeOutBGM(1.5f));

        GoToHome();
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

    void GoToHome() => SceneManager.LoadScene("HomeScene");
}
