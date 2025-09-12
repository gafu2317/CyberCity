using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Linq;
using System.Runtime.CompilerServices;

public class UIControlller : MonoBehaviour
{
    [Header("対象パネル")]
    [SerializeField] private RectTransform[] targetPanels;
    [SerializeField] private int targetPanelNum;//どのパネルを選ぶか
    //[SerializeField] private RectTransform[] targetButtons;
    [Header("問題文")]
    [SerializeField] private RectTransform question; //問題文を表示するところ

    [Header("アニメーション設定")]
    [SerializeField] private float slideDuration = 0.5f;       // スライド時間
    [SerializeField] private Vector3 targetScale = new Vector3(2f, 2f, 1f); // 最終的な拡大倍率
    [SerializeField] private float slideDistance = 1000f;       // スライドアウト距離
    [SerializeField] float fadeDuration = 3.0f;     //フェードアウトの時間

    private RectTransform[] targetButtons;//参照するボタンの配列
    private Vector2[] originalPositions;//ボタンの元の位置
    private Vector2 originalPositionQue; //問題文の元の位置
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        targetButtons = SetButton(targetPanels[targetPanelNum]);//指定したパネルのボタンをセット

        int len = targetButtons.Length;
        originalPositions = new Vector2[len];

        for (int i = 0; i < len; i++)
        {
            originalPositions[i] = targetButtons[i].anchoredPosition;
        }
        originalPositionQue = question.anchoredPosition;
    }

 
    public void OnButtonClicked(RectTransform clickedButton)   //どのボタンが押されたのかを参照して、そのボタンと他のボタンで処理を分ける
    {
        //ボタンが押されたらまず全ボタンを無効化
        foreach (var btn in targetButtons)
        {
            var buttonComp = btn.GetComponent<Button>();
            if (buttonComp != null)
            {
                buttonComp.interactable = false;
            }
        }

        StopAllCoroutines();
        for (int i = 0; i < targetButtons.Length; i++)
        {
            RectTransform btn = targetButtons[i];

            if (btn == clickedButton)
            {
                // 押されたボタン：中央にスライドして拡大
                StartCoroutine(SlideAndScaleToCenter(btn));

            }
            else
            {
                // 他のボタン：元の位置から画面外へスライド
                Vector2 targetPos = originalPositions[i];
                if (originalPositions[i].y > 0) targetPos.y -= slideDistance;
                else targetPos.y += slideDistance;

                StartCoroutine(SlideAnimation(btn, btn.anchoredPosition, targetPos, slideDuration));
            }

            //問題文を移動
            Vector2 targetPosQuestion = originalPositionQue;
            targetPosQuestion.y = slideDistance * 2;//移動距離を上げる
            StartCoroutine(SlideAnimation(question, question.anchoredPosition, targetPosQuestion, slideDuration * 1.5f));
        }
    }

    public RectTransform[] SetButton(RectTransform panel)//指定したパネルの中のボタンを返す。
    {
        Button[] buttons = panel.GetComponentsInChildren<Button>();
        RectTransform[] targetButtons = buttons.Select(b => b.GetComponent<RectTransform>()).ToArray();
        return targetButtons;
    }

    /// <summary>
    /// 押されたボタンを中央にスライドし、サイズを拡大
    /// </summary>
    private IEnumerator SlideAndScaleToCenter(RectTransform btn)
    {
        Transform parent = btn.transform.parent;//ボタンの親オブジェクトを取得
        // Layout Group を一時無効化
        var layoutGroup = btn.parent.GetComponent<LayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;

        // 中央にスライド + 同時にサイズ拡大
        Vector2 startPos = btn.anchoredPosition;

        //画面の中心座標
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        // Overlayならカメラ不要
        Vector3 worldCenter = screenCenter;

        // 開始位置と終了位置（ワールド座標）
        Vector3 startWorldPos = btn.position;
        Vector3 endWorldPos = worldCenter;
        endWorldPos.y += 50;//少しだけ中心より上へ移動 

        Vector2 endPos = Vector2.zero;

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float ease = Mathf.Sin(t * Mathf.PI * 0.5f);

            btn.position = Vector3.Lerp(startWorldPos, endWorldPos, ease);
            //btn.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
            parent.localScale = Vector2.Lerp(Vector3.one, targetScale, ease);

            yield return null;
        }

        btn.position = endWorldPos;
        // btn.anchoredPosition = endPos;
        parent.localScale = targetScale;

        yield return new WaitForSeconds(2f); //２秒待つ

        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);

            cg.alpha = 1f - t; // 徐々に透明に

            yield return null;
        }

        cg.alpha = 0f;
    }

    /// <summary>
    /// ボタンをスライド
    /// </summary>
    private IEnumerator SlideAnimation(RectTransform btn, Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
            btn.anchoredPosition = Vector2.Lerp(from, to, ease);
            yield return null;
        }

        btn.anchoredPosition = to;
    }
}
