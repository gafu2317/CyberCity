using UnityEngine;
using System.Collections;

public class UIShake : MonoBehaviour
{
    [SerializeField] private RectTransform targetUI; // �h�炵����Canvas
    [SerializeField] private float duration = 0.2f;  // �h��鎞��
    [SerializeField] private float magnitude = 5f;   // �h�ꕝ�i�s�N�Z���j

    private Vector3 originalPos;

    private void Awake()
    {
        if (targetUI == null)
            targetUI = GetComponent<RectTransform>(); // �������g��Ώۂɂ���
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

        targetUI.localPosition = originalPos; // ���̈ʒu�ɖ߂�
    }
}
