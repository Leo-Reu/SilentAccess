using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[RequireComponent(typeof(ScrollRect))]
public class DropdownAutoScroll : MonoBehaviour
{
    private ScrollRect scrollRect;
    private Toggle lastSelected;

    private void Start()
    {
        scrollRect = GetComponent<ScrollRect>();

        if (scrollRect.content == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        Toggle[] items = scrollRect.content.GetComponentsInChildren<Toggle>(false);

        foreach (Toggle item in items)
        {
            if (!item.isOn)
            {
                continue;
            }

            lastSelected = item;
            ScrollToItem(item.transform as RectTransform, true);
            break;
        }
    }

    private void LateUpdate()
    {
        if (scrollRect == null || scrollRect.content == null || EventSystem.current == null)
        {
            return;
        }

        InputSystemUIInputModule uiInputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;

        if (uiInputModule == null || uiInputModule.move == null || uiInputModule.move.action == null)
        {
            return;
        }

        Vector2 navigate = uiInputModule.move.action.ReadValue<Vector2>();

        if (navigate.sqrMagnitude < 0.25f)
        {
            return;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected == null)
        {
            return;
        }

        Toggle item = selected.GetComponent<Toggle>();

        if (item == null || !item.transform.IsChildOf(scrollRect.content) || item == lastSelected)
        {
            return;
        }

        lastSelected = item;
        ScrollToItem(item.transform as RectTransform, false);
    }

    private void ScrollToItem(RectTransform item, bool center)
    {
        if (item == null)
        {
            return;
        }

        RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : transform as RectTransform;

        if (viewport == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        float hiddenHeight = scrollRect.content.rect.height - viewport.rect.height;

        if (hiddenHeight <= 0f)
        {
            return;
        }

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);

        float offset = 0f;

        if (center)
        {
            offset = viewport.rect.center.y - bounds.center.y;
        }
        else if (bounds.max.y > viewport.rect.yMax)
        {
            offset = viewport.rect.yMax - bounds.max.y;
        }
        else if (bounds.min.y < viewport.rect.yMin)
        {
            offset = viewport.rect.yMin - bounds.min.y;
        }

        if (Mathf.Abs(offset) <= 0.01f)
        {
            return;
        }

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition - offset / hiddenHeight);
    }
}