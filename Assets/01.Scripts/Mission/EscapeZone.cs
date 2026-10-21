using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    [SerializeField] private MissionController missionController;

    private void OnTriggerEnter(Collider other)
    {
        if (missionController == null)
        {
            return;
        }

        PlayerInventory player = other.GetComponentInParent<PlayerInventory>();

        if (player == null)
        {
            return;
        }

        missionController.CompleteMission();
    }
}
