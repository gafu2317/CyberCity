using UnityEngine;
using System.Collections;

public class UIShake : MonoBehaviour
{
    [SerializeField] private RectTransform targetUI; // 揺らしたいCanvas
    [SerializeField] private float duration = 0.2f;  // 揺れる時間
    [SerializeField] private float magnitude = 5f;   // 揺れ幅（ピクセル）

    private Vector3 originalPos;

    private void Awake()
    {
        if (targetUI == null)
            targetUI = GetComponent<RectTransform>(); // 自分自身を対象にする
        originalPos = targetUI.localPosition;
    }

    public void Shake()
    {
        Debug.Log("shake");
        StopAllCoroutines();
        StartCoroutine(DoShake());
    }

    private IEnumerator DoShake()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            targetUI.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        targetUI.localPosition = originalPos; // 元の位置に戻す
    }
}
