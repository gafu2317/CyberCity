using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AnswerUI : MonoBehaviour
{
    [SerializeField] private Image panelImage;         // �p�l����Image
    [SerializeField] private TextMeshProUGUI message;  // �e�L�X�g
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayTime = 2.5f;

    void Start()
    {
        // �������
        SetAlpha(0f);
        transform.localScale = Vector3.zero;
    }

    /// <summary>
    /// �����̕\��
    /// </summary>
    public void ShowCorrect()
    {
        if (message != null) message.text = "����";
        StartCoroutine(AnimateAndDestroy());
    }

    /// <summary>
    /// �s�����̕\��
    /// </summary>
    public void ShowIncorrect()
    {
        if (message != null) message.text = "�s����";
        StartCoroutine(AnimateAndDestroy());
    }

    private IEnumerator AnimateAndDestroy()
    {
        // --- �t�F�[�h�C�� + �g�� ---
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

        // --- �\������ ---
        yield return new WaitForSeconds(displayTime);

        // --- �t�F�[�h�A�E�g + �k�� ---
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

        Destroy(gameObject); // �����ō폜
    }

    // �p�l���ƃe�L�X�g�̓����x��ύX
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
