using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum UIButtonSoundType
{
    Menu,
    Terminal
}

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, ISubmitHandler
{
    [Header("Sound Type")]
    [SerializeField] private UIButtonSoundType soundType = UIButtonSoundType.Menu;

    [Header("Sound Settings")]
    [SerializeField] private bool enableHoverSound = true;
    [SerializeField] private bool enableClickSound = true;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enableHoverSound || !button.IsActive() || !button.IsInteractable())
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            if (soundType == UIButtonSoundType.Terminal)
            {
                SoundManager.Instance.PlayTerminalHover();
            }
            else
            {
                SoundManager.Instance.PlayHover();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enableClickSound || eventData.button != PointerEventData.InputButton.Left || !button.IsActive() || !button.IsInteractable())
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            if (soundType == UIButtonSoundType.Terminal)
            {
                SoundManager.Instance.PlayTerminalClick();
            }
            else
            {
                SoundManager.Instance.PlayClick();
            }
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (!enableClickSound || button == null || !button.IsActive() || !button.IsInteractable())
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            if (soundType == UIButtonSoundType.Terminal)
            {
                SoundManager.Instance.PlayTerminalClick();
            }
            else
            {
                SoundManager.Instance.PlayClick();
            }
        }
    }
}
