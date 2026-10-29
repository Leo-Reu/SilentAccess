using System.Collections;
using UnityEngine;

public class SecurityAlertFeedback : MonoBehaviour
{
    [Header("Mission")]
    [SerializeField] private MissionController missionController;

    [Header("UI")]
    [SerializeField] private GameObject alertPanel;
    [SerializeField] private float displayDuration = 5f;

    [Header("Audio")]
    [SerializeField] private AudioSource alertAudioSource;
    [SerializeField] private AudioClip alertClip;

    private Coroutine alertCoroutine;

    private void Awake()
    {
        if (alertPanel != null)
        {
            alertPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (missionController != null)
        {
            missionController.OnSecurityAlertTriggered += ShowAlert;
        }
    }

    private void OnDisable()
    {
        if (missionController != null)
        {
            missionController.OnSecurityAlertTriggered -= ShowAlert;
        }

        if (alertCoroutine != null)
        {
            StopCoroutine(alertCoroutine);
            alertCoroutine = null;
        }

        if (alertPanel != null)
        {
            alertPanel.SetActive(false);
        }
    }

    private void ShowAlert()
    {
        if (alertPanel == null)
        {
            return;
        }

        if (alertCoroutine != null)
        {
            StopCoroutine(alertCoroutine);
        }

        alertPanel.SetActive(true);

        if (alertAudioSource != null && alertClip != null)
        {
            alertAudioSource.PlayOneShot(alertClip);
        }

        alertCoroutine = StartCoroutine(HideAlert());
    }

    private IEnumerator HideAlert()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, displayDuration));

        alertPanel.SetActive(false);
        alertCoroutine = null;
    }
}
