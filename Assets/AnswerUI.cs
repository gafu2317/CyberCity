using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AnswerUI : MonoBehaviour
{
    [SerializeField] private Image panelImage;         // パネルのImage
    [SerializeField] private TextMeshProUGUI message;  // テキスト
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayTime = 2.5f;

    void Start()
    {
        // 初期状態
        SetAlpha(0f);
        transform.localScale = Vector3.zero;
    }

    /// <summary>
    /// 正解の表示
    /// </summary>
    public void ShowCorrect()
    {
        if (message != null) message.text = "正解";
        StartCoroutine(AnimateAndDestroy());
    }

    /// <summary>
    /// 不正解の表示
    /// </summary>
    public void ShowIncorrect()
    {
        if (message != null) message.text = "不正解";
        StartCoroutine(AnimateAndDestroy());
    }

    private IEnumerator AnimateAndDestroy()
    {
        // --- フェードイン + 拡大 ---
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            SetAlpha(t);
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);

            yield return null;
        }
        SetAlpha(1f);
        transform.localScale = Vector3.one;

        // --- 表示時間 ---
        yield return new WaitForSeconds(displayTime);

        // --- フェードアウト + 縮小 ---
        time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            SetAlpha(1f - t);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);

            yield return null;
        }
        SetAlpha(0f);
        transform.localScale = Vector3.zero;

        Destroy(gameObject); // 自動で削除
    }

    // パネルとテキストの透明度を変更
    private void SetAlpha(float alpha)
    {
        if (panelImage != null)
        {
            Color c = panelImage.color;
            c.a = alpha;
            panelImage.color = c;
        }

        if (message != null)
        {
            Color c = message.color;
            c.a = alpha;
            message.color = c;
        }
    }
}
