using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ChangeScene : MonoBehaviour
{
    public static ChangeScene Instance { get; private set; }

    [Header("遷移先のシーン名")]
    [SerializeField] private string gameSceneName = "Main";
    [SerializeField] private string titleSceneName = "Title";

    private int aKeyPressCount = 0; //Aキーの押された回数

    public bool CanCountKeyPress { get; set; } = false;
    public bool IsResultBackMode { get; private set; } = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!CanCountKeyPress && !IsResultBackMode) return;

        if (IsResultBackMode)
        {
            if (IsAnyReturnKeyPressed())
            {
                resetCount();
                backToTitle();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            string currentScenename = SceneManager.GetActiveScene().name;

            if (CanCountKeyPress && currentScenename == titleSceneName)
            {
                aKeyPressCount++;

                if (aKeyPressCount >= 2)
                {
                    resetCount();
                    LoadScene(gameSceneName);
                }
            }
        }
    }

    private bool IsAnyReturnKeyPressed()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }

    public void backToTitle()
    {
        LoadScene(titleSceneName);
    }

    private void LoadScene(string targetSceneName)
    {
        SceneManager.LoadScene(targetSceneName);
    }

    public void resetCount()
    {
        aKeyPressCount = 0;
        CanCountKeyPress = false;
        IsResultBackMode = false;
    }

    public void BeginResultBackToTitleMode()
    {
        IsResultBackMode = true;
        CanCountKeyPress = false;
    }


}
