using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[CustomEditor(typeof(GuardPatrolRoute))]
public class GuardPatrolRouteEditor : Editor
{
    private GuardPatrolRoute patrolRoute;
    private SerializedProperty patrolPoints;

    private void OnEnable()
    {
        patrolRoute = (GuardPatrolRoute)target;
        patrolPoints = serializedObject.FindProperty("patrolPoints");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Scene View에서 Shift + 좌클릭으로 Waypoint를 추가하세요.\n" +
            "Waypoint를 선택하면 기존 Move Tool로 이동할 수 있습니다.",
            MessageType.Info
        );

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Patrol Waypoints", EditorStyles.boldLabel);

        int moveFrom = -1;
        int moveTo = -1;
        int removeIndex = -1;

        for (int i = 0; i < patrolPoints.arraySize; i++)
        {
            SerializedProperty point = patrolPoints.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"WP {i:00}", GUILayout.Width(48));
            EditorGUILayout.PropertyField(point, GUIContent.none);

            using (new EditorGUI.DisabledScope(i == 0))
            {
                if (GUILayout.Button("▲", GUILayout.Width(26)))
                {
                    moveFrom = i;
                    moveTo = i - 1;
                }
            }

            using (new EditorGUI.DisabledScope(i == patrolPoints.arraySize - 1))
            {
                if (GUILayout.Button("▼", GUILayout.Width(26)))
                {
                    moveFrom = i;
                    moveTo = i + 1;
                }
            }

            if (GUILayout.Button("X", GUILayout.Width(26)))
            {
                removeIndex = i;
            }

            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();

        if (removeIndex >= 0)
        {
            RemoveWaypoint(removeIndex);
        }
        else if (moveFrom >= 0)
        {
            serializedObject.Update();
            patrolPoints.MoveArrayElement(moveFrom, moveTo);
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Patrol Type: Loop");

        if (patrolPoints.arraySize == 0)
        {
            EditorGUILayout.HelpBox("등록된 Waypoint가 없습니다.", MessageType.Warning);
        }
    }

    private void OnSceneGUI()
    {
        DrawWaypoints();

        Event e = Event.current;

        if (!e.shift || e.alt)
        {
            return;
        }

        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        if (e.type != EventType.MouseDown || e.button != 0)
        {
            return;
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3 targetPos;

        if (Physics.Raycast(ray, out RaycastHit rayHit, 1000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            targetPos = rayHit.point;
        }
        else
        {
            Plane plane = new Plane(Vector3.up, patrolRoute.transform.position);

            if (!plane.Raycast(ray, out float enter))
            {
                e.Use();
                return;
            }

            targetPos = ray.GetPoint(enter);
        }

        CreateWaypoint(targetPos);
        e.Use();
    }

    private void CreateWaypoint(Vector3 targetPos)
    {
        if (!NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
        {
            Debug.LogWarning("[Waypoint Editor] 주변에 NavMesh가 없습니다.");
            return;
        }

        serializedObject.Update();

        int index = patrolPoints.arraySize;

        GameObject waypoint = new GameObject($"WP_{index:00}");
        waypoint.transform.position = hit.position;

        Undo.RegisterCreatedObjectUndo(waypoint, "Create Patrol Waypoint");
        Undo.SetTransformParent(waypoint.transform, patrolRoute.transform, "Parent Patrol Waypoint");

        patrolPoints.InsertArrayElementAtIndex(index);
        patrolPoints.GetArrayElementAtIndex(index).objectReferenceValue = waypoint.transform;

        serializedObject.ApplyModifiedProperties();

        SceneView.RepaintAll();
    }

    private void RemoveWaypoint(int index)
    {
        serializedObject.Update();

        Transform point = patrolPoints.GetArrayElementAtIndex(index).objectReferenceValue as Transform;
        int previousCount = patrolPoints.arraySize;

        patrolPoints.DeleteArrayElementAtIndex(index);

        if (patrolPoints.arraySize == previousCount)
        {
            patrolPoints.DeleteArrayElementAtIndex(index);
        }

        serializedObject.ApplyModifiedProperties();

        if (point != null && point.parent == patrolRoute.transform)
        {
            Undo.DestroyObjectImmediate(point.gameObject);
        }

        SceneView.RepaintAll();
    }

    private void DrawWaypoints()
    {
        serializedObject.Update();

        int count = patrolPoints.arraySize;

        if (count == 0)
        {
            return;
        }

        Color previousColor = Handles.color;

        for (int i = 0; i < count; i++)
        {
            Transform point = patrolPoints.GetArrayElementAtIndex(i).objectReferenceValue as Transform;

            if (point == null)
            {
                continue;
            }

            bool valid = NavMesh.SamplePosition(point.position, out NavMeshHit hit, 0.5f, NavMesh.AllAreas)
                && Vector3.Distance(point.position, hit.position) < 0.5f;

            Handles.color = valid ? Color.cyan : Color.red;

            Handles.SphereHandleCap(0, point.position, Quaternion.identity, 0.4f, EventType.Repaint);

            Handles.Label(
                point.position + Vector3.up * 0.4f,
                $"WP {i:00}" + (valid ? "" : " - Off NavMesh")
            );
        }


        if (count >= 2)
        {
            for (int i = 0; i < count; i++)
            {
                Transform current = patrolPoints.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                Transform next = patrolPoints.GetArrayElementAtIndex((i + 1) % count).objectReferenceValue as Transform;

                if (current == null || next == null)
                {
                    continue;
                }

                Vector3 direction = next.position - current.position;

                if (direction.sqrMagnitude < 0.01f)
                {
                    continue;
                }

                bool currentValid = NavMesh.SamplePosition(current.position, out NavMeshHit currentHit, 0.5f, NavMesh.AllAreas)
                    && Vector3.Distance(current.position, currentHit.position) < 0.5f;

                bool nextValid = NavMesh.SamplePosition(next.position, out NavMeshHit nextHit, 0.5f, NavMesh.AllAreas)
                    && Vector3.Distance(next.position, nextHit.position) < 0.5f;

                Handles.color = currentValid && nextValid ? Color.cyan : Color.red;

                Handles.DrawAAPolyLine(3f, current.position, next.position);

                Vector3 arrowPos = Vector3.Lerp(current.position, next.position, 0.7f);
                Quaternion rotation = Quaternion.LookRotation(direction);

                Handles.ArrowHandleCap(0, arrowPos, rotation, 0.6f, EventType.Repaint);
            }
        }


        Handles.color = previousColor;
    }
}
