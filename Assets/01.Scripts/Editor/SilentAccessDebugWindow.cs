using UnityEditor;
using UnityEngine;

public class SilentAccessDebugWindow : EditorWindow
{
    private Vector2 scrollPos;
    private double nextRefreshTime;

    [MenuItem("Tools/Silent Access/Debug Tool")]
    public static void ShowWindow()
    {
        GetWindow<SilentAccessDebugWindow>("Silent Access Debug");
    }

    private void OnEnable()
    {
        EditorApplication.update += RefreshWindow;
    }

    private void OnDisable()
    {
        EditorApplication.update -= RefreshWindow;
    }

    private void RefreshWindow()
    {
        if (!Application.isPlaying || EditorApplication.timeSinceStartup < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = EditorApplication.timeSinceStartup + 0.2d;
        Repaint();
    }

    private void OnGUI()
    {
        GUILayout.Label("Silent Access Debug Tool", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play 모드에서만 사용할 수 있습니다.", MessageType.Info);
            return;
        }

        DebugSceneRefs refs = Object.FindFirstObjectByType<DebugSceneRefs>();

        if (refs == null)
        {
            EditorGUILayout.HelpBox("DebugSceneRefs를 가진 오브젝트가 없습니다.", MessageType.Warning);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawStatus(refs);

        EditorGUILayout.Space(15);

        DrawTeleport(refs);

        EditorGUILayout.Space(15);

        DrawPlayer(refs);

        EditorGUILayout.Space(15);

        DrawMission(refs);

        EditorGUILayout.EndScrollView();
    }

    private void DrawStatus(DebugSceneRefs refs)
    {
        GUILayout.Label("Mission Status", EditorStyles.boldLabel);

        MissionController mission = refs.MissionController;
        MainServer server = refs.MainServer;

        string phase = mission != null ? mission.CurrentPhase.ToString() : "Not Assigned";
        string alert = mission != null ? (mission.IsSecurityAlertActive ? "ON" : "OFF") : "Not Assigned";
        string stolen = server != null ? (server.IsDataStolen ? "YES" : "NO") : "Not Assigned";

        EditorGUILayout.LabelField("Current Phase", phase);
        EditorGUILayout.LabelField("Security Alert", alert);
        EditorGUILayout.LabelField("Data Stolen", stolen);
    }

    private void DrawTeleport(DebugSceneRefs refs)
    {
        GUILayout.Label("Teleport", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawTeleportButton("Lobby", refs.Player, refs.LobbyPoint);
            DrawTeleportButton("Security", refs.Player, refs.SecurityPoint);
            DrawTeleportButton("Server", refs.Player, refs.ServerPoint);
        }
    }

    private void DrawTeleportButton(string label, Transform player, Transform destination)
    {
        using (new EditorGUI.DisabledScope(player == null || destination == null))
        {
            if (GUILayout.Button(label, GUILayout.Height(30)))
            {
                Teleport(player, destination);
            }
        }
    }

    private void Teleport(Transform player, Transform destination)
    {
        CharacterController controller = player.GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        player.SetPositionAndRotation(destination.position, destination.rotation);
        Physics.SyncTransforms();

        if (controller != null)
        {
            controller.enabled = true;
        }

        Debug.Log($"[Debug Tool] Teleported to {destination.name}");
    }

    private void DrawPlayer(DebugSceneRefs refs)
    {
        GUILayout.Label("Player", EditorStyles.boldLabel);

        PlayerInventory inventory = refs.Player != null ? refs.Player.GetComponent<PlayerInventory>() : null;

        using (new EditorGUI.DisabledScope(inventory == null || inventory.HasKeycard))
        {
            if (GUILayout.Button("Give Keycard"))
            {
                inventory.CollectKeycard();
            }
        }

        SecurityDoor door = refs.ServerDoor;

        using (new EditorGUI.DisabledScope(door == null || door.IsUnlocked))
        {
            if (GUILayout.Button("Unlock Server Door"))
            {
                door.Unlock();
            }
        }
    }

    private void DrawMission(DebugSceneRefs refs)
    {
        GUILayout.Label("Mission Control", EditorStyles.boldLabel);

        MainServer server = refs.MainServer;

        using (new EditorGUI.DisabledScope(server == null || server.IsDataStolen))
        {
            if (GUILayout.Button("Force Data Theft"))
            {
                server.DebugCompleteDownload();
            }
        }
    }
}
