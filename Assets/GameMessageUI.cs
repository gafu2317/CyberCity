using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameClearUI : MonoBehaviour
{
    [SerializeField] private Image[] letters;      // G, A, M, E, C, L, E, A, R のImage配列
    [SerializeField] private float dropDuration = 1f;   // 降下時間
    [SerializeField] private float delayBetween = 0.2f; // 文字ごとの遅延
    [SerializeField] private float dropHeight = 500f;   // 上から落ちてくる距離
    [SerializeField] private float bounceAmplitude = 5f; // 上下振幅
    [SerializeField] private float bounceSpeed = 2f;    // 上下スピード

    private Vector3[] targetPositions;

    void Start()
    {
        // 各文字の定位置を保存 & 初期位置を上にずらす
        targetPositions = new Vector3[letters.Length];
        for (int i = 0; i < letters.Length; i++)
        {
            targetPositions[i] = letters[i].rectTransform.anchoredPosition;
            letters[i].rectTransform.anchoredPosition += new Vector2(0, dropHeight);
            letters[i].gameObject.SetActive(false); // 最初は非表示
        }

        
    }

    public void PlayGAMECLEARAnimation()
    {
        StartCoroutine(PlayAnimation());
    }

    IEnumerator PlayAnimation()
    {
        for (int i = 0; i < letters.Length; i++)
        {
            letters[i].gameObject.SetActive(true);
            StartCoroutine(DropLetter(letters[i].rectTransform, targetPositions[i]));
            yield return new WaitForSeconds(delayBetween);
        }
    }

    IEnumerator DropLetter(RectTransform letter, Vector3 targetPos)
    {
        Vector3 startPos = letter.anchoredPosition;
        float time = 0f;

        // 降下アニメーション
        while (time < dropDuration)
        {
            time += Time.deltaTime;
            float t = time / dropDuration;
            // イージング（EaseOutCubic）
            t = 1f - Mathf.Pow(1f - t, 3f);
            letter.anchoredPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        letter.anchoredPosition = targetPos;

        // 定位置で上下振動
        float offset = Random.Range(0f, Mathf.PI * 2f); // 位相ずらし
        while (true)
        {
            float y = Mathf.Sin(Time.time * bounceSpeed + offset) * bounceAmplitude;
            letter.anchoredPosition = targetPos + new Vector3(0, y, 0);
            yield return null;
        }
    }
}
