using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button closeButton2;

    private void Start()
    {
        // 初期値設定
        if (SimpleGameManager.Instance != null)
        {
            if (bgmSlider != null)
            {
                bgmSlider.value = SimpleGameManager.Instance.GetBGMVolume();
                bgmSlider.onValueChanged.AddListener(OnBGMChanged);
            }

            if (seSlider != null)
            {
                seSlider.value = SimpleGameManager.Instance.GetSEVolume();
                seSlider.onValueChanged.AddListener(OnSEChanged);
            }
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
            closeButton2.onClick.AddListener(Close);
        }
    }

    private void OnBGMChanged(float value)
    {
        if (SimpleGameManager.Instance != null)
        {
            SimpleGameManager.Instance.SetBGMVolume(value);
        }
    }

    private void OnSEChanged(float value)
    {
        if (SimpleGameManager.Instance != null)
        {
            SimpleGameManager.Instance.SetSEVolume(value);
        }
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f; // ゲーム一時停止
        SimpleGameManager.Instance.PlayButtonSound();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f; // ゲーム再開
        SimpleGameManager.Instance.PlayButtonSound();
    }
}
