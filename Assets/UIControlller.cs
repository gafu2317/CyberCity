using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System;
using TMPro;

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
    
    private void Start()
    {
        // ボタンの元の位置を保存する配列を初期化
        // 選択肢の最大数が4なので4に固定（将来的に変更する場合は要修正）
        originalPositions = new Vector2[4];

        // パネルボタンの初期化（デバッグ用）
        targetButtons = SetButton(targetPanels[targetPanelNum]); // 指定したパネルのボタンをセット
        targetPanels[targetPanelNum].gameObject.SetActive(true); // パネル自体の有効化
        InitializeButtons();

        // 問題文の初期位置を設定
        originalPositionQue = question.anchoredPosition;
        Vector2 startPosQue = originalPositionQue;
        startPosQue.y += slideDistance * 2;
        question.anchoredPosition = startPosQue;
    }

    /// <summary>
    /// 使用するパネルのボタンの初期化を行う
    /// </summary>
    private void InitializeButtons()
    {
        int len = targetButtons.Length;
        for (int i = 0; i < len; i++)
        {
            originalPositions[i] = targetButtons[i].anchoredPosition;

            Vector2 startPos = originalPositions[i];

            if (originalPositions[i].y > 0) // 上半分なら上から
                startPos.y -= slideDistance;
            else                            // 下半分なら下から
                startPos.y += slideDistance;

            targetButtons[i].anchoredPosition = startPos;//初期位置を変更

            // スケールをリセット
            targetButtons[i].transform.parent.localScale = Vector3.one;

            // ボタンを有効化
            var buttonComp = targetButtons[i].GetComponent<Button>();
            if (buttonComp != null)
            {
                buttonComp.interactable = true;
            }
        }
    }

    /// <summary>
    /// 使用するパネルボタンの変更を行う
    /// </summary>
    public void ChangeTargetPanel(int panelNum)
    {
        // 現在使用中のボタンの位置を初期位置に戻し、パネル自体を無効化
        for (int i = 0; i < targetButtons.Length; i++)
        {
            targetButtons[i].anchoredPosition = originalPositions[i];
        }
        targetPanels[targetPanelNum].gameObject.SetActive(false);

        // 使用パネルの変更
        targetPanelNum = panelNum;
        targetButtons = SetButton(targetPanels[targetPanelNum]); // 指定したパネルのボタンをセット
        targetPanels[targetPanelNum].gameObject.SetActive(true); // パネル自体の有効化
        InitializeButtons();
    }

    private RectTransform[] SetButton(RectTransform panel)//指定したパネルの中のボタンを返す。
    {
        Button[] buttons = panel.GetComponentsInChildren<Button>();
        RectTransform[] targetButtons = buttons.Select(b => b.GetComponent<RectTransform>()).ToArray();
        return targetButtons;
    }

    /// <summary>
    /// 問題文と選択肢のテキストを設定する
    /// </summary>
    public void SetQuestionAndChoices(string questionText, string[] choicesTexts)
    {
        // 問題文の設定
        TextMeshProUGUI questionTextComp = question.GetComponentInChildren<TextMeshProUGUI>();
        if (questionTextComp != null)
        {
            questionTextComp.text = questionText;
        }

        // 選択肢の設定
        int len = Math.Min(choicesTexts.Length, targetButtons.Length);
        for (int i = 0; i < len; i++)
        {
            TextMeshProUGUI buttonTextComp = targetButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (buttonTextComp != null)
            {
                buttonTextComp.text = choicesTexts[i];
            }
        }
    }

    /// <summary>
    /// クイズのスライドインアニメーション
    /// </summary>
    public IEnumerator SlideInCoroutine()
    {
        // ※ここでは StopAllCoroutines() を呼ばない（呼ぶとこのコルーチン自体も停止されるため）
        // 何個のコルーチンを開始するかを数える（各ターゲットで Slide + Fade の2つ、question も2つ）
        int totalCoroutinesToStart = targetButtons.Length * 2 + 2;
        int remaining = totalCoroutinesToStart;

        // 開始するたびに RunAndSignal でラップしておくと、完了時に remaining-- される
        for (int i = 0; i < targetButtons.Length; i++)
        {
            var btnRect = targetButtons[i];
            var buttonComp = btnRect.GetComponent<Button>();
            if (buttonComp != null) buttonComp.interactable = true;

            // SlideAnimation を開始（完了で remaining--）
            StartCoroutine(RunAndSignal(SlideAnimation(btnRect, btnRect.anchoredPosition, originalPositions[i], slideDuration),
                () => remaining--));

            // FadeIn を開始（完了で remaining--）
            StartCoroutine(RunAndSignal(FadeIn(btnRect), () => remaining--));
        }

        // question の Slide + Fade
        StartCoroutine(RunAndSignal(SlideAnimation(question, question.anchoredPosition, originalPositionQue, slideDuration),
            () => remaining--));
        StartCoroutine(RunAndSignal(FadeIn(question), () => remaining--));

        // 全部終わるまで待つ
        yield return new WaitUntil(() => remaining <= 0);
    }

    /// <summary>
    /// 任意の IEnumerator を実行し、完了時に onComplete を呼ぶラッパー
    /// </summary>
    private IEnumerator RunAndSignal(IEnumerator routine, Action onComplete)
    {
        yield return StartCoroutine(routine);
        onComplete?.Invoke();
    }
 
    /// <summary>
    /// ボタンが押されたときの処理
    /// どのボタンが押されたのかを参照して、そのボタンと他のボタンで処理を分ける
    /// </summary>
    public void OnButtonClicked(RectTransform clickedButton)
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

    /// <summary>
    /// キーボード入力による選択肢入力の処理
    /// </summary>
    public IEnumerator AnswerSelectCoroutine(int choiceNum)
    {
        // 各ボタンにつき 1 本（押されたボタン：SlideAndScaleToCenter、他は SlideAnimation）
        // ＋ question の SlideAnimation が 1 本
        int remaining = targetButtons.Length + 1;

        // 各ボタンの処理開始
        for (int i = 0; i < targetButtons.Length; i++)
        {
            RectTransform btn = targetButtons[i];

            if (i == choiceNum - 1)
            {
                // 押されたボタン：中央にスライドして拡大（IEnumerator を想定）
                StartCoroutine(RunAndSignal(SlideAndScaleToCenter(btn), () => remaining--));
            }
            else
            {
                // 他のボタン：元の位置から画面外へスライド
                Vector2 targetPos = originalPositions[i];
                if (originalPositions[i].y > 0) targetPos.y -= slideDistance;
                else targetPos.y += slideDistance;

                StartCoroutine(RunAndSignal(SlideAnimation(btn, btn.anchoredPosition, targetPos, slideDuration),
                    () => remaining--));
            }

            // 押されたボタンの interactable 等を切る／設定したい場合ここで行う
            var btnComp = btn.GetComponent<Button>();
            if (btnComp != null) btnComp.interactable = false;
        }

        // 問題文（question）を移動 — 元コードはループ内で何度も呼んでいたが、
        // 多重起動は不要と判断してここで1回だけ実行するよう整理
        Vector2 targetPosQuestion = originalPositionQue;
        targetPosQuestion.y = slideDistance * 2; // 移動距離を上げる
        StartCoroutine(RunAndSignal(SlideAnimation(question, question.anchoredPosition, targetPosQuestion, slideDuration * 1.5f),
            () => remaining--));

        // 全て終わるまで待機
        yield return new WaitUntil(() => remaining <= 0);
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

        //画面の中心座標
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        // Overlayならカメラ不要
        Vector3 worldCenter = screenCenter;

        // 開始位置と終了位置（ワールド座標）
        Vector3 startWorldPos = btn.position;
        Vector3 endWorldPos = worldCenter;
        endWorldPos.y += 50;//少しだけ中心より上へ移動 

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

    /// <summary>
    /// フェードイン
    /// </summary>
    private IEnumerator FadeIn(RectTransform btn)
    {
        float duration = 1.0f;//フェードの時間
        CanvasGroup cg = btn.GetComponent<CanvasGroup>();//キャンバスグループの追加
        if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
            cg.alpha = t;
            yield return null;
        }
        cg.alpha = 1f;
    }

    public void StopAnimations()//外部からのコルーチンを止めるためのメソッド
    {
        StopAllCoroutines();
        Debug.Log("アニメーション停止！");
    }
}
