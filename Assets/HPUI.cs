using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HPUI : MonoBehaviour
{
    [SerializeField] private Image[] barriers; // �E��̃o���A�摜��3�Z�b�g
    [SerializeField] private Image warningPanel; // �Ԃ��x���p�̃p�l��
    [SerializeField] private Image[] cracks;//�_���[�W���󂯂����̉�ʂ̂Ђъ���
    [SerializeField] private UIShake uiShake;//��ʂ�h�炷�悤
    private int maxHP = 3;

    private int currentHP;
    private Coroutine redCoroutine; // �ԃt�F�[�h�p�̃R���[�`���Ǘ�

    void Start()
    {
        currentHP = maxHP;
        warningPanel.color = new Color(1, 0, 0, 0); // ���S����
        for (int i = 0; i < cracks.Length; i++)
        {
            cracks[i].gameObject.SetActive(false);
        }
        UpdateUI();
    }

    public void TakeDamage(int damage = 1)
    {
        if (currentHP <= 0) return;

        // �_���[�W����UI��h�炷
        if (uiShake != null)
            uiShake.Shake();

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        Debug.Log(currentHP);
        // HP �ɉ����ĂЂт�\��
        if (maxHP - currentHP-1 >= 0 && maxHP - currentHP-1 < cracks.Length)
        {
            cracks[maxHP - currentHP-1].gameObject.SetActive(true);
        }

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


            StartCoroutine(WarningEffect()); // �x���J�n

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

    //�x�񂪂Ȃ��Ă�݂����ɉ�ʂ�Ԃ��_�ł�����
    private IEnumerator WarningEffect()
    {
        yield return new WaitForSeconds(1f); //�Q�b�҂�

        float speed = 2f; // �_�ŃX�s�[�h
        while (currentHP == 1) // HP��1�̊Ԃ����ƌJ��Ԃ�
        {
            float t = (Mathf.Cos(Time.time * speed) + 1f) / 2f;
            warningPanel.color = new Color(1, 0, 0, t * 0.5f);
            yield return null;
        }

        // HP��1����ς������I��
        warningPanel.color = new Color(1, 0, 0, 0);
    }
}
