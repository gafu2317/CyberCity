using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Linq;
using System.Runtime.CompilerServices;

public class UIControlller : MonoBehaviour
{
    [Header("�Ώۃp�l��")]
    [SerializeField] private RectTransform[] targetPanels;
    [SerializeField] private int targetPanelNum;//�ǂ̃p�l����I�Ԃ�
    //[SerializeField] private RectTransform[] targetButtons;
    [Header("��蕶")]
    [SerializeField] private RectTransform question; //��蕶��\������Ƃ���

    [Header("�A�j���[�V�����ݒ�")]
    [SerializeField] private float slideDuration = 0.5f;       // �X���C�h����
    [SerializeField] private Vector3 targetScale = new Vector3(2f, 2f, 1f); // �ŏI�I�Ȋg��{��
    [SerializeField] private float slideDistance = 1000f;       // �X���C�h�A�E�g����
    [SerializeField] float fadeDuration = 3.0f;     //�t�F�[�h�A�E�g�̎���

    private RectTransform[] targetButtons;//�Q�Ƃ���{�^���̔z��
    private Vector2[] originalPositions;//�{�^���̌��̈ʒu
    private Vector2 originalPositionQue; //��蕶�̌��̈ʒu
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        targetButtons = SetButton(targetPanels[targetPanelNum]);//�w�肵���p�l���̃{�^�����Z�b�g

        int len = targetButtons.Length;
        originalPositions = new Vector2[len];

        for (int i = 0; i < len; i++)
        {
            originalPositions[i] = targetButtons[i].anchoredPosition;
        }
        originalPositionQue = question.anchoredPosition;
    }

 
    public void OnButtonClicked(RectTransform clickedButton)   //�ǂ̃{�^���������ꂽ�̂����Q�Ƃ��āA���̃{�^���Ƒ��̃{�^���ŏ����𕪂���
    {
        //�{�^���������ꂽ��܂��S�{�^���𖳌���
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
                // �����ꂽ�{�^���F�����ɃX���C�h���Ċg��
                StartCoroutine(SlideAndScaleToCenter(btn));

            }
            else
            {
                // ���̃{�^���F���̈ʒu�����ʊO�փX���C�h
                Vector2 targetPos = originalPositions[i];
                if (originalPositions[i].y > 0) targetPos.y -= slideDistance;
                else targetPos.y += slideDistance;

                StartCoroutine(SlideAnimation(btn, btn.anchoredPosition, targetPos, slideDuration));
            }

            //��蕶���ړ�
            Vector2 targetPosQuestion = originalPositionQue;
            targetPosQuestion.y = slideDistance * 2;//�ړ��������グ��
            StartCoroutine(SlideAnimation(question, question.anchoredPosition, targetPosQuestion, slideDuration * 1.5f));
        }
    }

    public RectTransform[] SetButton(RectTransform panel)//�w�肵���p�l���̒��̃{�^����Ԃ��B
    {
        Button[] buttons = panel.GetComponentsInChildren<Button>();
        RectTransform[] targetButtons = buttons.Select(b => b.GetComponent<RectTransform>()).ToArray();
        return targetButtons;
    }

    /// <summary>
    /// �����ꂽ�{�^���𒆉��ɃX���C�h���A�T�C�Y���g��
    /// </summary>
    private IEnumerator SlideAndScaleToCenter(RectTransform btn)
    {
        Transform parent = btn.transform.parent;//�{�^���̐e�I�u�W�F�N�g���擾
        // Layout Group ���ꎞ������
        var layoutGroup = btn.parent.GetComponent<LayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;

        // �����ɃX���C�h + �����ɃT�C�Y�g��
        Vector2 startPos = btn.anchoredPosition;

        //��ʂ̒��S���W
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        // Overlay�Ȃ�J�����s�v
        Vector3 worldCenter = screenCenter;

        // �J�n�ʒu�ƏI���ʒu�i���[���h���W�j
        Vector3 startWorldPos = btn.position;
        Vector3 endWorldPos = worldCenter;
        endWorldPos.y += 50;//�����������S����ֈړ� 

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

        yield return new WaitForSeconds(2f); //�Q�b�҂�

        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);

            cg.alpha = 1f - t; // ���X�ɓ�����

            yield return null;
        }

        cg.alpha = 0f;
    }

    /// <summary>
    /// �{�^�����X���C�h
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
