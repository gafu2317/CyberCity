using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// GAME CLEAR / GAME OVER 両対応のアニメーションUI
/// </summary>
public class GameResultUI : MonoBehaviour
{
    [Header("GAME CLEAR の文字画像 (順に設定)")]
    [SerializeField] private Image[] gameClearLetters;

    [Header("GAME OVER の文字画像 (順に設定)")]
    [SerializeField] private Image[] gameOverLetters;

    [Header("アニメーション設定")]
    [SerializeField] private float dropDuration = 1f;     // 落ちる時間
    [SerializeField] private float delayBetween = 0.2f;   // 文字ごとの遅延
    [SerializeField] private float dropHeight = 300f;     // 落下開始の高さ
    [SerializeField] private float bounceAmplitude = 5f;  // 振動の大きさ
    [SerializeField] private float bounceSpeed = 2f;      // 振動の速さ
    [SerializeField] private bool loopBounce = true;      // 永遠に揺れ続けるか

    private Vector3[] targetPositions;
    private Coroutine animationCoroutine;
    private Image[] currentLetters;

    void Awake()
    {
        // 最初は両方非表示
        SetActiveLetters(gameClearLetters, false);
        SetActiveLetters(gameOverLetters, false);
    }

    /// <summary>
    /// GAME CLEAR アニメーションを開始
    /// </summary>
    public void PlayGameClear()
    {
        PlayAnimation(gameClearLetters);
    }

    /// <summary>
    /// GAME OVER アニメーションを開始
    /// </summary>
    public void PlayGameOver()
    {
        PlayAnimation(gameOverLetters);
    }

    private void PlayAnimation(Image[] letters)
    {
        // すでに動いていたら停止
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        // 前の文字を隠す
        SetActiveLetters(gameClearLetters, false);
        SetActiveLetters(gameOverLetters, false);

        // 今回使用する文字を設定
        currentLetters = letters;
        PrepareLetters(currentLetters);

        // 再生
        animationCoroutine = StartCoroutine(AnimateLetters());
    }

    private void PrepareLetters(Image[] letters)
    {
        targetPositions = new Vector3[letters.Length];

        for (int i = 0; i < letters.Length; i++)
        {
            RectTransform rect = letters[i].rectTransform;
            targetPositions[i] = rect.anchoredPosition;
            rect.anchoredPosition += new Vector2(0, dropHeight);
            letters[i].gameObject.SetActive(false);
        }
    }

    IEnumerator AnimateLetters()
    {
        for (int i = 0; i < currentLetters.Length; i++)
        {
            currentLetters[i].gameObject.SetActive(true);
            StartCoroutine(DropLetter(currentLetters[i].rectTransform, targetPositions[i]));
            yield return new WaitForSeconds(delayBetween);
        }
    }

    IEnumerator DropLetter(RectTransform letter, Vector3 targetPos)
    {
        Vector3 startPos = letter.anchoredPosition;
        float time = 0f;

        // 落下アニメーション
        while (time < dropDuration)
        {
            time += Time.deltaTime;
            float t = time / dropDuration;
            t = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic
            letter.anchoredPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        letter.anchoredPosition = targetPos;

        // 上下振動（必要なら）
        if (loopBounce)
        {
            float offset = Random.Range(0f, Mathf.PI * 2f);
            while (true)
            {
                float y = Mathf.Sin(Time.time * bounceSpeed + offset) * bounceAmplitude;
                letter.anchoredPosition = targetPos + new Vector3(0, y, 0);
                yield return null;
            }
        }
    }

    private void SetActiveLetters(Image[] letters, bool active)
    {
        foreach (var img in letters)
            if (img != null)
                img.gameObject.SetActive(active);
    }
}
