using UnityEngine;
using UnityEngine.UI;

public class DialogueSwitcher : MonoBehaviour
{
    [Header("直接拖入切换按钮")]
    public Button switchButton;

    [Header("按顺序拖入对话物体")]
    public GameObject[] dialogueObjects;

    [Header("对话结束后关闭的整体物体（可不填）")]
    public GameObject objectToDisable;

    private int currentIndex = 0;
    private bool isFinished = false;

    private void Start()
    {
        if (dialogueObjects == null || dialogueObjects.Length == 0)
        {
            Debug.LogWarning("请添加对话物体。");
            return;
        }

        // 开始时只显示第一张对话
        for (int i = 0; i < dialogueObjects.Length; i++)
        {
            if (dialogueObjects[i] != null)
            {
                dialogueObjects[i].SetActive(i == 0);
            }
        }

        // 自动绑定按钮
        if (switchButton != null)
        {
            switchButton.onClick.AddListener(ShowNextDialogue);
        }
    }

    private void ShowNextDialogue()
    {
        if (isFinished ||
            dialogueObjects == null ||
            dialogueObjects.Length == 0)
        {
            return;
        }

        // 关闭当前对话
        if (dialogueObjects[currentIndex] != null)
        {
            dialogueObjects[currentIndex].SetActive(false);
        }

        // 还有下一张对话
        if (currentIndex < dialogueObjects.Length - 1)
        {
            currentIndex++;

            if (dialogueObjects[currentIndex] != null)
            {
                dialogueObjects[currentIndex].SetActive(true);
            }
        }
        else
        {
            // 最后一张显示完后，再点击关闭整体界面
            if (objectToDisable != null)
            {
                objectToDisable.SetActive(false);
            }

            isFinished = true;
        }
    }

    private void OnDestroy()
    {
        if (switchButton != null)
        {
            switchButton.onClick.RemoveListener(ShowNextDialogue);
        }
    }
}
