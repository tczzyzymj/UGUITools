using System;
using UnityEngine;
using UnityEditor;


public class NFEditorTools
{
    [MenuItem("GameObject/显示选中目标的 LocalPosition", false, -1)]
    public static void ShowSelectTargetLocalPosition()
    {
        var _selectTarget = Selection.activeGameObject;

        if (_selectTarget == null)
        {
            return;
        }

        Debug.LogError("Local position is : " + _selectTarget.transform.localPosition);
    }
}
