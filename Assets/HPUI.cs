using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class HPUI : MonoBehaviour
{
    [SerializeField] private Image[] barriers; // 右上のバリア画像を3つセット
    [SerializeField] private Image warningPanel; // 赤い警告用のパネル
    [SerializeField] private Image[] cracks;//ダメージを受けた時の画面のひび割れ
    [SerializeField] private UIShake uiShake;//画面を揺らすよう
    [SerializeField] private VideoPlayer[] smokeVideoPlayers; // 煙動画用の VideoPlayer 配列
    [SerializeField] private RawImage[] smokeRawImages;       // 煙表示用の RawImage 配列
    private int smokeIndex = 0;                                // 次に出す煙の番号
    private int maxHP = 3;

    private int currentHP;
    private Coroutine redCoroutine; // 赤フェード用のコルーチン管理

    void Start()
    {
        currentHP = maxHP;
        warningPanel.color = new Color(1, 0, 0, 0); // 完全透明
        for (int i = 0; i < cracks.Length; i++)
        {
            cracks[i].gameObject.SetActive(false);
        }

        // 煙動画を非表示に
        for (int i = 0; i < smokeRawImages.Length; i++)
            smokeRawImages[i].gameObject.SetActive(false);

        UpdateUI();
    }

    public void TakeDamage(int damage = 1)
    {
        if (currentHP <= 0) return;

        // ダメージ時にUIを揺らす
        if (uiShake != null)
            uiShake.Shake();

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        
        // HP に応じてひびを表示
        if (maxHP - currentHP-1 >= 0 && maxHP - currentHP-1 < cracks.Length)
        {
            cracks[maxHP - currentHP-1].gameObject.SetActive(true);
        }

        // 減ったバリアをフェードアウト
        if (currentHP < barriers.Length)
        {
            StartCoroutine(FadeOut(barriers[currentHP]));
        }

        // 煙動画を順番に再生
        if (smokeIndex < smokeRawImages.Length)
        {
            smokeRawImages[smokeIndex].gameObject.SetActive(true);
            smokeVideoPlayers[smokeIndex].Play();
            smokeIndex++;
        }


        UpdateUI();
    }

    void UpdateUI()
    {
        // 残り1つのとき赤へ徐々にフェード
        if (currentHP == 1)
        {


            StartCoroutine(WarningEffect()); // 警告開始

            if (redCoroutine == null) // まだ赤フェードが始まっていなければ
            {
                redCoroutine = StartCoroutine(FadeToRed(barriers[0]));
            }
        }
    }

    // フェードアウト処理
    private IEnumerator FadeOut(Image img)
    {
        float duration = 0.5f;
        float time = 0f;
        Color start = img.color;
        Color end = start;
        end.a = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            img.color = Color.Lerp(start, end, time / duration);
            yield return null;
        }

        img.enabled = false;
    }


    // 赤へフェード処理
    private IEnumerator FadeToRed(Image img)
    {
        float duration = 1.0f; // 赤に変わるまでの時間
        float time = 0f;
        Color start = img.color;
        Color end = Color.red;

        while (time < duration)
        {
            time += Time.deltaTime;
            img.color = Color.Lerp(start, end, time / duration);
            yield return null;
        }

        img.color = end; // 最終的に赤固定
    }

    //警報がなってるみたいに画面を赤く点滅させる
    private IEnumerator WarningEffect()
    {
        yield return new WaitForSeconds(1f); //２秒待つ

        float speed = 2f; // 点滅スピード
        while (currentHP == 1) // HPが1の間ずっと繰り返す
        {
            float t = (Mathf.Cos(Time.time * speed) + 1f) / 2f;
            warningPanel.color = new Color(1, 0, 0, t * 0.5f);
            yield return null;
        }

        // HPが1から変わったら終了
        warningPanel.color = new Color(1, 0, 0, 0);
    }
}
