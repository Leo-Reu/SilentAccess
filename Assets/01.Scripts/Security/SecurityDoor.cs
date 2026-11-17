using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SecurityDoor : MonoBehaviour
{
    public enum AccessMode
    {
        Keycard,
        Terminal
    }

    [Header("Doors")]
    [SerializeField] private Transform doorL;
    [SerializeField] private Transform doorR;

    [Header("Movement")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float openDistance = 1.6f;

    [Header("Access")]
    [SerializeField] private AccessMode accessMode = AccessMode.Keycard;

    [Header("Navigation")]
    [SerializeField] private NavMeshObstacle lockedDoorObstacle;

    private Vector3 initialDoorL;
    private Vector3 initialDoorR;

    private AudioSource audioSource;
    private readonly HashSet<Collider> authorizedColliders = new HashSet<Collider>();

    private float point;
    private bool opening;
    private bool isUnlocked;

    public bool IsUnlocked => isUnlocked;
    public event Action OnUnlocked;

    private void Start()
    {
        initialDoorL = doorL.localPosition;
        initialDoorR = doorR.localPosition;

        audioSource = GetComponent<AudioSource>();
        isUnlocked = false;

        if (accessMode == AccessMode.Terminal && lockedDoorObstacle != null)
        {
            lockedDoorObstacle.enabled = !isUnlocked;
        }
    }

    private void Update()
    {
        point = Mathf.Lerp(point, opening ? 1f : 0f, Time.deltaTime * speed);

        doorL.localPosition = initialDoorL + Vector3.back * point * openDistance;
        doorR.localPosition = initialDoorR + Vector3.forward * point * openDistance;
    }

    private void OnTriggerEnter(Collider other)
    {
        UpdateOccupant(other);
    }

    private void OnTriggerStay(Collider other)
    {
        UpdateOccupant(other);
    }

    private void OnTriggerExit(Collider other)
    {
        authorizedColliders.Remove(other);
        SetOpening(authorizedColliders.Count > 0);
    }

    private void UpdateOccupant(Collider other)
    {
        if (HasAccess(other))
        {
            authorizedColliders.Add(other);
        }
        else
        {
            authorizedColliders.Remove(other);
        }

        SetOpening(authorizedColliders.Count > 0);
    }

    private bool HasAccess(Collider other)
    {
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        bool isGuard = other.GetComponentInParent<GuardController>() != null;

        if (accessMode == AccessMode.Terminal)
        {
            return isUnlocked && (inventory != null || isGuard);
        }

        return isGuard || (inventory != null && inventory.HasKeycard);
    }

    private void SetOpening(bool value)
    {
        if (opening == value)
        {
            return;
        }

        opening = value;

        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    [ContextMenu("Test Unlock (Play Mode)")]
    public void Unlock()
    {
        if (!Application.isPlaying || accessMode != AccessMode.Terminal || isUnlocked)
        {
            return;
        }

        isUnlocked = true;

        if (lockedDoorObstacle != null)
        {
            lockedDoorObstacle.enabled = false;
        }

        OnUnlocked?.Invoke();
    }
}