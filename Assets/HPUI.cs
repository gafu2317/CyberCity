using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HPUI : MonoBehaviour
{
    [SerializeField] private Image[] barriers; // �E��̃o���A�摜��3�Z�b�g
    [SerializeField] private int maxHP = 3;

    private int currentHP;
    private Coroutine redCoroutine; // �ԃt�F�[�h�p�̃R���[�`���Ǘ�

    void Start()
    {
        currentHP = maxHP;
        UpdateUI();
    }

    public void TakeDamage(int damage = 1)
    {
        if (currentHP <= 0) return;

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        // �������o���A���t�F�[�h�A�E�g
        if (currentHP < barriers.Length)
        {
            StartCoroutine(FadeOut(barriers[currentHP]));
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        // �c��1�̂Ƃ��Ԃ֏��X�Ƀt�F�[�h
        if (currentHP == 1)
        {
            if (redCoroutine == null) // �܂��ԃt�F�[�h���n�܂��Ă��Ȃ����
            {
                redCoroutine = StartCoroutine(FadeToRed(barriers[0]));
            }
        }
    }

    // �t�F�[�h�A�E�g����
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


    // �Ԃփt�F�[�h����
    private IEnumerator FadeToRed(Image img)
    {
        float duration = 1.0f; // �Ԃɕς��܂ł̎���
        float time = 0f;
        Color start = img.color;
        Color end = Color.red;

        while (time < duration)
        {
            time += Time.deltaTime;
            img.color = Color.Lerp(start, end, time / duration);
            yield return null;
        }

        img.color = end; // �ŏI�I�ɐԌŒ�
    }
}
