using UnityEngine;
using UnityEngine.UI;

public class GoalImageManager : MonoBehaviour
{
    private Image image;

    [SerializeField] private Sprite[] sprites;
    [SerializeField] private float changeInterval = 2f;

    private void Awake()
    {
        EnsureImage();
    }

    private void EnsureImage()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }
    }

    public void ShowResultImage(int index)
    {
        EnsureImage();

        if (image == null || sprites == null || index < 0 || index >= sprites.Length)
        {
            return;
        }

        image.sprite = sprites[index];
    }

    
}
