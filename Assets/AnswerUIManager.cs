using UnityEngine;

public class AnswerUIManager : MonoBehaviour
{
    [SerializeField] private GameObject correctUIPrefab; // Prefab���Z�b�g
    [SerializeField] private Transform canvasTransform;  // Canvas��Transform
    public void ShowCorrectUI()
    {
        GameObject ui = Instantiate(correctUIPrefab, canvasTransform);
        ui.transform.localScale = Vector3.zero; // �ŏ��͏�����
        ui.GetComponent<AnswerUI>().ShowCorrect(); // �A�j���[�V�����J�n
    }
}
