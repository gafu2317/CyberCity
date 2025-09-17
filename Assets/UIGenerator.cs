using UnityEngine;

public class UIGenerator : MonoBehaviour
{
    [SerializeField] private GameObject quizeUIPrefab; // Prefab���Z�b�g
    [SerializeField] private Transform canvasTransform;  //UI��\������Canvas���w��
    public UIControlller uIControlller;
    public void ShowQuizUI(int panelNum)
    {
        uIControlller.SetTargetPanelNum(panelNum);//������panel�̔ԍ����擾
        Instantiate(quizeUIPrefab, canvasTransform);
        
    }
}
