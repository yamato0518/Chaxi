using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TurtrialManager : MonoBehaviour
{
    private Image image;
    private int currentSpriteIndex = 0;

    [SerializeField] private Sprite[] sprites;
    [SerializeField] private float changeInterval = 2f;

    void Start()
    {
        image = GetComponent<Image>();

        if (image == null || sprites == null || sprites.Length == 0)
        {
            return;
        }

        if(AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayStartBGM();
        }

        currentSpriteIndex = 0;
        image.sprite = sprites[currentSpriteIndex];
        StartCoroutine(ChangeSpriteRoutine());
    }

    void Update()
    {
        
    }

    private IEnumerator ChangeSpriteRoutine()
    {
        while (currentSpriteIndex < sprites.Length - 1)
        {
            yield return new WaitForSeconds(changeInterval);

            currentSpriteIndex = (currentSpriteIndex + 1) % sprites.Length;
            image.sprite = sprites[currentSpriteIndex];
        }

        yield return new WaitForSeconds(2f);
        //gameObject.SetActive(false);
    }
}
