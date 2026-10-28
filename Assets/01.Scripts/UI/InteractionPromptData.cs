using UnityEngine;

public class InteractionPromptData : MonoBehaviour
{
    [SerializeField] private string actionText = "상호작용";

    public string ActionText => actionText;
}
