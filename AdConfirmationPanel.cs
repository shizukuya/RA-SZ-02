using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdConfirmationPanel : MonoBehaviour
{
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private TMP_Text messageText;

    private System.Action onConfirmed;
    private System.Action onCancelled;

    private void Start()
    {
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnYesClicked);
        }
        if (noButton != null)
        {
            noButton.onClick.AddListener(OnNoClicked);
        }
    }

    public void Show(System.Action onConfirmed, System.Action onCancelled)
    {
        this.onConfirmed = onConfirmed;
        this.onCancelled = onCancelled;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnYesClicked()
    {
        Hide();
        onConfirmed?.Invoke();
    }

    private void OnNoClicked()
    {
        Hide();
        onCancelled?.Invoke();
    }
}
