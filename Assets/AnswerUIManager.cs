using UnityEngine;

public class AnswerUIManager : MonoBehaviour
{
    [SerializeField] private GameObject correctUIPrefab; // Prefab���Z�b�g
    [SerializeField] private Transform canvasTransform;  // Canvas��Transform
    public void ShowAnswerUI(bool answer)//�������s��������UI�������Banswer��True�̎��ɐ���
    {
        GameObject ui = Instantiate(correctUIPrefab, canvasTransform);
        ui.transform.localScale = Vector3.zero; // �ŏ��͏�����
        if (answer)
        {
            ui.GetComponent<AnswerUI>().ShowCorrect(); // �����A�j���[�V�����J�n
        }
        else
        {
            ui.GetComponent<AnswerUI>().ShowIncorrect();//�s����
        }
    }
}
