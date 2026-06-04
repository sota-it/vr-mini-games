using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public partial class HomeManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI instructionText;
    public GameObject potionPrefab;
    public Transform lifeParent;

    [Header("Animation Frame Settings")]
    public RectTransform animationFrame;
    public float animationDuration = 0.5f;

    [Header("🤖 Pose Guide Settings (新規追加)")]
    [Tooltip("子オブジェクトに各ゲームのガイドを仕込んだ親オブジェクト（ReflectPoseGuideをアタッチしたもの）")]
    public ReflectPoseGuide poseGuideRoot;

    [Header("Animator Settings")]
    public Animator resultAnimator;

    [Header("Result Transformation")]
    public Transform resultCharacterTransform;
    public float rotationSpeed = 20f;

    [Header("Camera Control (VR)")]
    public Transform cameraRig;
    public Vector3 zoomOffset = new Vector3(0, 0.4f, -2.5f);
    private Vector3 originalCameraPos;

    [Header("Ground Loop Settings")]
    public GameObject[] groundSegments;
    public float scrollSpeed = 5.0f;
    public float groundLength = 10.0f;

    [Header("Balloon Settings")]
    public RectTransform scoreBalloon;
    public TextMeshProUGUI balloonScoreText;

    [Header("Audio Level Settings")]
    public AudioSource bgmAudioSource;
    [Range(0f, 1f)] public float bgmVolume = 0.3f;
    [Tooltip("3小節: 通常成功 (現在のBPMで完結)")]
    public AudioClip[] successClips;
    [Tooltip("3小節: 通常失敗 (現在のBPMで完結)")]
    public AudioClip[] failureClips;
    [Tooltip("5小節: 1小節目は旧BPM、2小節目から新BPM")]
    public AudioClip[] levelUpClips;

    [Header("Level Up UI Settings")]
    public GameObject levelUpRoot;
    public TextMeshProUGUI levelValueText;
    public RectTransform levelUpArrow;

    // 定数設定
    private const float BaseBPM = 140f;
    private const float BPMStep = 14f;
    private bool isRunning = true;

    void Start()
    {
        if (cameraRig != null) originalCameraPos = cameraRig.localPosition;

        // 初期スコア表示の計算
        int initialDisplayScore = GameData.SuccessCount;
        if (GameData.isLastGameSuccess && initialDisplayScore > 0)
        {
            initialDisplayScore -= 1;
        }

        scoreText.text = initialDisplayScore.ToString();
        instructionText.text = "";
        if (balloonScoreText != null) balloonScoreText.text = initialDisplayScore.ToString();
        if (animationFrame != null) animationFrame.gameObject.SetActive(false);
        if (levelUpRoot != null) levelUpRoot.SetActive(false);

        StartCoroutine(MainSequence());
    }

    void Update()
    {
        if (isRunning) MoveGround();
    }

    void MoveGround()
    {
        foreach (GameObject ground in groundSegments)
        {
            ground.transform.Translate(Vector3.back * scrollSpeed * Time.deltaTime);
            if (ground.transform.position.z <= -groundLength)
            {
                Vector3 newPos = ground.transform.position;
                newPos.z += groundLength * groundSegments.Length;
                ground.transform.position = newPos;
            }
        }
    }

    IEnumerator MainSequence()
    {
        if (GameData.SuccessCount == 0 && GameData.isLastGameSuccess==true)
        {
            yield return StartCoroutine(ShowGameStartSequence());
        }
        // 1. ライフ減少演出
        yield return StartCoroutine(LifeInitialSequence());

        // ゲームオーバー
        if (GameData.Lives <= 0)
        {
            levelUpRoot.SetActive(true);
            levelValueText.text = "GAME OVER";

           
            if (levelUpArrow != null) levelUpArrow.gameObject.SetActive(false);
            yield return new WaitForSeconds(1.0f); // 少し余韻を残す
            SceneManager.LoadScene("StartScene"); // スタート画面（タイトル）へ
            yield break; // 以降の処理（次のゲームの指示など）を中止する
        }

        // 2. リズミカル演出実行
        yield return StartCoroutine(ExecuteRhythmicSequence());
    }

    IEnumerator ShowGameStartSequence()
    {
        if (levelUpRoot == null || levelValueText == null) yield break;

        // 地面のスクロールを一時的に止める（お好みで有効/無効にしてください）
        isRunning = false;

        // UIを表示状態にしてテキストを書き換える
        levelUpRoot.SetActive(true);
        levelValueText.text = "Level 1\nGAME START!!";

        // 矢印のアニメーションが不要、もしくは止めておきたい場合は
        // ここで levelUpArrow の動きを止めるか、位置を中央に固定します。
        if (levelUpArrow != null) levelUpArrow.gameObject.SetActive(false);

        yield return new WaitForSeconds(2.0f);

        // 演出用UIを閉じて、地面の動きを再開
        levelUpArrow.gameObject.SetActive(true);
        levelUpRoot.SetActive(false);
        isRunning = true;
    }

    IEnumerator LifeInitialSequence()
    {
        int heartToSpawn = (GameData.isLastGameSuccess) ? GameData.Lives : GameData.Lives + 1;
        List<GameObject> activeHearts = new List<GameObject>();

        foreach (Transform child in lifeParent) Destroy(child.gameObject);
        for (int i = 0; i < heartToSpawn; i++)
        {
            GameObject h = Instantiate(potionPrefab, lifeParent);
            activeHearts.Add(h);
        }

        yield return new WaitForSeconds(0.5f);

        if (!GameData.isLastGameSuccess && activeHearts.Count > 0)
        {
            yield return StartCoroutine(AnimateHeartLoss(activeHearts[activeHearts.Count - 1]));
        }
    }

    IEnumerator ExecuteRhythmicSequence()
    {
        // レベル情報の取得
        int currentLevel = (GameData.SuccessCount / 5) + 1;
        bool isLevelUpMoment = GameData.isLastGameSuccess && GameData.SuccessCount > 0 && GameData.SuccessCount % 5 == 0;

        // --- BPM計算 ---
        float bpmPhase1 = isLevelUpMoment ? (BaseBPM + (currentLevel - 2) * BPMStep) : (BaseBPM + (currentLevel - 1) * BPMStep);
        float bpmPhaseNext = BaseBPM + (currentLevel - 1) * BPMStep;

        float mDurationPhase1 = (60f / bpmPhase1) * 4f;
        float mDurationPhaseNext = (60f / bpmPhaseNext) * 4f;

        // クリップの決定
        int clipIdx = Mathf.Clamp(currentLevel - 1, 0, 99);
        AudioClip clip;
        if (isLevelUpMoment) clip = levelUpClips[clipIdx];
        else if (GameData.isLastGameSuccess) clip = successClips[clipIdx];
        else clip = failureClips[clipIdx];

        // BGM開始
        bgmAudioSource.clip = clip;
        bgmAudioSource.volume = bgmVolume;
        bgmAudioSource.loop = false;
        bgmAudioSource.Play();

        // --- フェーズ1: リザルト反応 (1小節) ---
        isRunning = false;
        StartCoroutine(PhaseResultReaction(mDurationPhase1));
        yield return new WaitForSeconds(mDurationPhase1);

        // 次のシーン決定
        string nextScene = GetRandomScene();

        if (isLevelUpMoment)
        {
            if (lifeParent != null) lifeParent.gameObject.SetActive(false);
            if (scoreBalloon != null) scoreBalloon.gameObject.SetActive(false);
            // --- フェーズ2: レベルアップ演出 (2-3小節) ---
            StartCoroutine(PhaseLevelUpAnimation(mDurationPhaseNext * 2f, currentLevel));
            yield return new WaitForSeconds(mDurationPhaseNext * 2f);

            // --- フェーズ3: 次の指示 (4-5小節) ---
            if (lifeParent != null) lifeParent.gameObject.SetActive(true);
            if (scoreBalloon != null) scoreBalloon.gameObject.SetActive(true);
            isRunning = true;
            yield return StartCoroutine(PhaseInstruction(mDurationPhaseNext * 2f, nextScene));
        }
        else
        {
            // --- フェーズ2-3: 次の指示 (2-3小節) ---
            isRunning = true;
            yield return StartCoroutine(PhaseInstruction(mDurationPhaseNext * 2f, nextScene));
        }

        // ロード
        SceneManager.LoadScene(nextScene);
    }

    // --- 各フェーズの演出詳細 ---

    IEnumerator PhaseResultReaction(float duration)
    {
        if (GameData.isLastGameSuccess)
        {
            resultAnimator.SetTrigger("doSuccess");
            StartCoroutine(AnimateScoreFlip(GameData.SuccessCount, duration * 0.9f));
        }
        else
        {
            resultAnimator.SetTrigger("doFailure");
        }

        float half = duration * 0.5f;
        yield return StartCoroutine(MoveCamera(originalCameraPos, originalCameraPos + zoomOffset, half));
        yield return StartCoroutine(MoveCamera(originalCameraPos + zoomOffset, originalCameraPos, half));
    }

    IEnumerator PhaseLevelUpAnimation(float duration, int level)
    {
        if (levelUpRoot == null) yield break;
        levelUpRoot.SetActive(true);
        levelValueText.text = "Level UP !\n";
        levelValueText.text += "Level " + level;

        float elapsed = 0f;
        Vector2 startPos = new Vector2(0, -300);
        Vector2 endPos = new Vector2(0, 300);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            levelUpArrow.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        levelUpRoot.SetActive(false);
    }

    IEnumerator PhaseInstruction(float duration, string nextScene)
    {
        float animInTime = 0.4f;
        StartCoroutine(SlideInFrameRhythmic(animInTime));
        StartCoroutine(MoveBalloonToTopRightRhythmic(animInTime));

        // テキストと従来のテキスト用Animatorのトリガーを引く
        SetInstructionContent(nextScene);

        string poseType = GetPoseTypeFromScene(nextScene);

        if (poseGuideRoot != null)
        {
            poseGuideRoot.ShowPose(poseType);
        }

        yield return new WaitForSeconds(duration);
    }

    private string GetPoseTypeFromScene(string sceneName)
    {
        switch (sceneName)
        {
            case "CountingScene":
                return "Counting";
            case "ReflectScene":
                return "Reflect";

            case "ObstacleScene":
            case "MazeScene":
            case "WaveScene":
                return "Stick";       // スティックを動かすガイド

            case "WeightScene":
                return "T-Pose";      // 天秤のように傾くユニティちゃん

            case "FlyingScene":
                return "Flying";      // 羽ばたく（手をばたばたする）ユニティちゃん

            case "JankenScene":
            case "MagicScene":
                return "Hands";       // 手を動かす汎用ガイド

            case "BasketballScene":
            case "QuoitsScene":
                return "Grab";        // トリガーをカチカチ引くガイド
            case "RPGScene":
                return "RPG";
            case "BalloonScene":
                return "Swing";
            case "SearchScene":
            case "ShadowScene":
            default:
                return "Searching";   // 周りを見渡すなどの汎用ガイド

        }
    }

    // --- アニメーション・計算ヘルパー ---

    IEnumerator MoveCamera(Vector3 start, Vector3 end, float time)
    {
        float elapsed = 0f;
        Quaternion lookAtPlayerRot = Quaternion.Euler(0, 180, 0);
        Quaternion forwardRot = Quaternion.Euler(0, 0, 0);

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / time;
            float easedT = t * t * (3f - 2f * t);

            if (cameraRig != null) cameraRig.localPosition = Vector3.Lerp(start, end, easedT);

            Quaternion targetRot = (end != originalCameraPos) ? lookAtPlayerRot : forwardRot;
            resultCharacterTransform.localRotation = Quaternion.Slerp(resultCharacterTransform.localRotation, targetRot, easedT);

            yield return null;
        }
    }

    IEnumerator AnimateScoreFlip(int endValue, float duration)
    {
        if (balloonScoreText == null) yield break;
        float half = duration / 2f;
        RectTransform textRect = balloonScoreText.rectTransform;

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            textRect.localRotation = Quaternion.Euler((elapsed / half) * 90f, 0, 0);
            yield return null;
        }

        balloonScoreText.text = endValue.ToString();
        scoreText.text = endValue.ToString();

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            textRect.localRotation = Quaternion.Euler(270f + (elapsed / half) * 90f, 0, 0);
            yield return null;
        }
        textRect.localRotation = Quaternion.identity;
    }

    IEnumerator SlideInFrameRhythmic(float duration)
    {
        animationFrame.gameObject.SetActive(true);
        Vector2 startPos = new Vector2(-Screen.width, -Screen.height);
        animationFrame.anchoredPosition = startPos;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            animationFrame.anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, elapsed / duration);
            yield return null;
        }
        animationFrame.anchoredPosition = Vector2.zero;
    }

    IEnumerator MoveBalloonToTopRightRhythmic(float duration)
    {
        Vector2 startPos = Vector2.zero;
        Vector2 endPos = new Vector2(Screen.width * 0.35f, Screen.height * 0.35f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            scoreBalloon.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }
        scoreBalloon.anchoredPosition = endPos;
    }

    IEnumerator AnimateHeartLoss(GameObject heart)
    {
        CanvasGroup canvasGroup = heart.GetComponent<CanvasGroup>() ?? heart.AddComponent<CanvasGroup>();
        RectTransform rect = heart.GetComponent<RectTransform>();
        LayoutElement layout = heart.GetComponent<LayoutElement>() ?? heart.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;

        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startPos = rect.localPosition;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rect.localPosition = startPos + new Vector3(0, -100f * t, 0);
            canvasGroup.alpha = 1f - t;
            yield return null;
        }
        Destroy(heart);
    }

    private string GetRandomScene()
    {
        string[] scenes = { "JankenScene", "ObstacleScene", "WeightScene", "SearchScene", "BasketballScene", "MazeScene", "CountingScene", "ShadowScene", "FlyingScene", "RPGScene", "MagicScene", "WaveScene", "ReflectScene","QuoitsScene","BalloonScene" };

        string nextScene;
        if (GameData.SuccessCount == 0 && GameData.isLastGameSuccess == true)
        {
            nextScene = "WaveScene";
        }
        else
        {
            do
            {
                nextScene = scenes[Random.Range(0, scenes.Length)];
            }
            while (GameData.SceneHistory.Contains(nextScene));
        }

        GameData.SceneHistory.Add(nextScene);

        if (GameData.SceneHistory.Count > scenes.Length - 3)
        {
            GameData.SceneHistory.RemoveAt(0);
        }

        return nextScene;
    }

    private void SetInstructionContent(string sceneName)
    {
        switch (sceneName)
        {
            case "JankenScene":
                instructionText.text = "Hands";
                break;
            case "ObstacleScene":
                instructionText.text = "Stick";
                break;
            case "WeightScene":
                instructionText.text = "T-Pose";
                break;
            case "SearchScene":
                instructionText.text = "Searching";
                break;
            case "BasketballScene":
                instructionText.text = "Grab";
                break;
            case "MazeScene":
                instructionText.text = "Stick";
                break;
            case "CountingScene":
                instructionText.text = "Searching";
                break;
            case "ShadowScene":
                instructionText.text = "Searching";
                break;
            case "FlyingScene":
                instructionText.text = "Swing";
                break;
            case "RPGScene":
                instructionText.text = "Stick&Swing";
                break;
            case "MagicScene":
                instructionText.text = "Hands";
                break;
            case "WaveScene":
                instructionText.text = "Stick";
                break;
            case "ReflectScene":
                instructionText.text = "Reflect";
                break;
            case "QuoitsScene":
                instructionText.text = "Grab";
                break;
            case "BalloonScene":
                instructionText.text = "Swing";
                break;
        }
    }
}