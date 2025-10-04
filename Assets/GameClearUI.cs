using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameClearUI : MonoBehaviour
{
    [SerializeField] private Image[] letters;      // G, A, M, E, C, L, E, A, R ��Image�z��
    [SerializeField] private float dropDuration = 1f;   // �~������
    [SerializeField] private float delayBetween = 0.2f; // �������Ƃ̒x��
    [SerializeField] private float dropHeight = 500f;   // �ォ�痎���Ă��鋗��
    [SerializeField] private float bounceAmplitude = 5f; // �㉺�U��
    [SerializeField] private float bounceSpeed = 2f;    // �㉺�X�s�[�h

    private Vector3[] targetPositions;

    void Start()
    {
        // �e�����̒�ʒu��ۑ� & �����ʒu����ɂ��炷
        targetPositions = new Vector3[letters.Length];
        for (int i = 0; i < letters.Length; i++)
        {
            targetPositions[i] = letters[i].rectTransform.anchoredPosition;
            letters[i].rectTransform.anchoredPosition += new Vector2(0, dropHeight);
            letters[i].gameObject.SetActive(false); // �ŏ��͔�\��
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

        // �~���A�j���[�V����
        while (time < dropDuration)
        {
            time += Time.deltaTime;
            float t = time / dropDuration;
            // �C�[�W���O�iEaseOutCubic�j
            t = 1f - Mathf.Pow(1f - t, 3f);
            letter.anchoredPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        letter.anchoredPosition = targetPos;

        // ��ʒu�ŏ㉺�U��
        float offset = Random.Range(0f, Mathf.PI * 2f); // �ʑ����炵
        while (true)
        {
            float y = Mathf.Sin(Time.time * bounceSpeed + offset) * bounceAmplitude;
            letter.anchoredPosition = targetPos + new Vector3(0, y, 0);
            yield return null;
        }
    }
}
