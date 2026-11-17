using Michsky.UI.Shift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AddScriptToButton : Editor
{
    [MenuItem("Tools/Sound/UIButtonSound 일괄 전환")]
    private static void ReplaceButtonSounds()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Play Mode에서는 버튼 사운드를 변경할 수 없습니다.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        Button[] allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int buttonCount = 0;
        int addedCount = 0;
        int removedCount = 0;

        for (int i = 0; i < allButtons.Length; i++)
        {
            GameObject obj = allButtons[i].gameObject;

            if (obj.scene != activeScene)
            {
                continue;
            }

            buttonCount++;

            if (obj.GetComponent<UIButtonSound>() == null)
            {
                Undo.AddComponent<UIButtonSound>(obj);
                addedCount++;
            }

            UIElementSound[] oldSounds = obj.GetComponents<UIElementSound>();

            for (int j = 0; j < oldSounds.Length; j++)
            {
                Undo.DestroyObjectImmediate(oldSounds[j]);
                removedCount++;
            }
        }

        if (addedCount > 0 || removedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }

        Debug.Log($"[{activeScene.name}] 버튼 {buttonCount}개 검사 / UIButtonSound {addedCount}개 추가 / UIElementSound {removedCount}개 제거");
    }
}