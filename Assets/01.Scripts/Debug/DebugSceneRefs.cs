using UnityEngine;

public class DebugSceneRefs : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Mission")]
    [SerializeField] private MainServer mainServer;
    [SerializeField] private SecurityDoor serverDoor;
    [SerializeField] private MissionController missionController;

    [Header("Teleport Points")]
    [SerializeField] private Transform lobbyPoint;
    [SerializeField] private Transform securityPoint;
    [SerializeField] private Transform serverPoint;

    public Transform Player => player;
    public MainServer MainServer => mainServer;
    public SecurityDoor ServerDoor => serverDoor;
    public MissionController MissionController => missionController;

    public Transform LobbyPoint => lobbyPoint;
    public Transform SecurityPoint => securityPoint;
    public Transform ServerPoint => serverPoint;
}
