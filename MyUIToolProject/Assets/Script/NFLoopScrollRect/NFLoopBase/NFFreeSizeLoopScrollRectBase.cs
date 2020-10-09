using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.UI;


/// <summary>
/// size of item is fixed! create fixed count when awake
/// 内部 item 的长度是确定的
///
/// there should not be any layout group component or content size filter component on content game object node
/// content节点上面不要有任何 layoutgroup 组件
///
/// if there is , it will be disabled
/// 如果有，那么会被禁用掉
/// </summary>
[AddComponentMenu("")]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public abstract class NFFreeSizeLoopScrollRectBase : NFLoopScrollRectBase
{
    protected float ItemWidth
    {
        get;
        set;
    }


    /// <summary>
    /// 这里不能计算
    /// </summary>
    /// <param name="childRect"></param>
    protected override void CalculateItemSize(RectTransform childRect)
    {
        if (childRect == null)
        {
            return;
        }

        ItemWidth = childRect.rect.width;
    }
}
