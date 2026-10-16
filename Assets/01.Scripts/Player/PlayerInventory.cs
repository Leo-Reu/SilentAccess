using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public event Action OnKeycardCollected;

    public bool HasKeycard { get; private set; }

    public bool CollectKeycard()
    {
        if (HasKeycard)
        {
            return false;
        }

        HasKeycard = true;
        OnKeycardCollected?.Invoke();

        Debug.Log("카드키 획득");

        return true;
    }
}