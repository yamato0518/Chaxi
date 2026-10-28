using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using TMPro;

public class ShaderOpeningManager : MonoBehaviour
{
    [Header("紙芝居画像リスト")]
    [SerializeField] private Sprite[] openingSprites;

    [Header("UIのImage")]
    [SerializeField] private Image targetImage;

    [Header("ページがめくれる時間")]
    [SerializeField] private float flipDuration = 0.6f;

    [Header("タイトルUI")]
    [SerializeField] private GameObject titleMainUI;
    [SerializeField] private TextMeshProUGUI titleText;

    private AudioManager audioManager;
    private Material paperMaterial;
    private int currentSpriteIndex = 0;
    private bool isFlipping = false;
    private bool isFinished = false;
    private int countTurnover = 0;

    void Start()
    {
        audioManager = FindFirstObjectByType<AudioManager>();

        if (titleMainUI != null)
        {
            titleMainUI.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = "茶碗を左に回してね";

        }

        if (targetImage != null && openingSprites.Length > 0)
        {
            paperMaterial = targetImage.material;

            UpdateTextures();
            paperMaterial.SetFloat("_Flip", -1f);
        }
    }

    void Update()
    {
        if (isFinished || isFlipping) return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(FlipPageRoutine());
        }
    }

    private IEnumerator FlipPageRoutine()
    {
        audioManager.PlaySE(12);

        isFlipping = true;
        float elapsedTime = 0f;

        UpdateTextByPageIndex(countTurnover);
        countTurnover++;

        while (elapsedTime < flipDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / flipDuration;

            float flipValue = Mathf.Lerp(-1f, 1f, t);
            paperMaterial.SetFloat("_Flip", flipValue);

            yield return null;
        }

        // めくり終わったらインデックスを進める
        currentSpriteIndex++;
        UpdateTextures();

        // シェーダーの数値を初期状態に戻す
        paperMaterial.SetFloat("_Flip", -1f);
        isFlipping = false;

        if (currentSpriteIndex == openingSprites.Length - 1)
        {
            FinishOpening();
        }
    }

    private void UpdateTextures()
    {
        if (paperMaterial == null || openingSprites.Length == 0) return;

        UpdateTextByPageIndex(currentSpriteIndex);

        targetImage.sprite = openingSprites[currentSpriteIndex];

        if (currentSpriteIndex + 1 < openingSprites.Length)
        {
            Texture2D nextTex = openingSprites[currentSpriteIndex + 1].texture;
            paperMaterial.SetTexture("_PageTex", nextTex);
        }
        else
        {
            paperMaterial.SetTexture("_PageTex", null);
        }
    }

    private void UpdateTextByPageIndex(int pageIndex)
    {
        if (titleText == null) return;

        Vector2 pos = titleText.rectTransform.anchoredPosition;

        switch (pageIndex)
        {
            case 0:
                titleText.text = "茶碗を左に、回してね";
                pos.x = 1131f;
                pos.y = -957f;
                break;
            case 1:
                titleText.text = "茶碗を左に、回してね";
                pos.x = 114f;
                pos.y = -13f;
                break;
            case 2:
            case 3:
            case 4:
            case 5:
                pos.x = 1143f;
                pos.y = -13f;
                titleText.text = "茶碗を左に、回してね";
                break;
            default:
                titleText.text = "";
                break;
        }
        titleText.rectTransform.anchoredPosition = pos;
    }

    private void FinishOpening()
    {
        isFinished = true;

        this.enabled = false;
        titleText.text = "";

        if (ChangeScene.Instance != null)
        {
            ChangeScene.Instance.CanCountKeyPress = true;
        }
    }
}
