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
public abstract class NFFixSizeLoopScrollRectBase : NFLoopScrollRectBase
{
    protected Vector2 mItemSize = Vector2.zero;


    protected Vector2 mHalfItemSize = Vector2.zero;


    /// <summary>
    /// 横跨的最大行数或者列数
    /// </summary>
    protected int mMaxSpanCount = 0;


    public override void SetTotalCount(int targetCount)
    {
        base.SetTotalCount(targetCount);

        UpdateContentSize();
    }


    protected override bool InternalCreateChild()
    {
        if (mHasCreateChild)
        {
            return true;
        }

        if (mCreateNewChildCallback == null && content.childCount < 1)
        {
            ShowError("错误，无法创建 新的 Child，请检查！");

            return false;
        }

        if (mCreateNewChildCallback != null && content.childCount < 1)
        {
            var _newGO = mCreateNewChildCallback.Invoke();

            if (_newGO != null)
            {
                _newGO.transform.SetParent(content.transform, false);
            }
            else
            {
                ShowError("Create new item failed! please check!");

                return false;
            }
        }

        var _childRect = content.GetChild(0).transform as RectTransform;

        if (_childRect == null)
        {
            ShowError("子节点不包含 RectTransform，请检查！");

            return false;
        }

        CalculateItemSize(_childRect);

        mHalfItemSize = mItemSize * 0.5f;

        mMaxChildCount = CalculateMaxChildCount();

        mMaxSpanCount = mMaxChildCount / ConstraintCount;

        if (mCreateNewChildCallback != null)
        {
            if (content.childCount < mMaxChildCount)
            {
                for (int i = content.childCount; i < mMaxChildCount; ++i)
                {
                    var _newGO = mCreateNewChildCallback.Invoke();

                    if (_newGO != null)
                    {
                        _newGO.transform.SetParent(content.transform, false);
                    }
                    else
                    {
                        ShowError("Create new item failed! please check!");

                        return false;
                    }
                }
            }
        }
        else
        {
            if (content.childCount < mMaxChildCount)
            {
                var _firstChildRect = content.GetChild(0).transform;

                for (int i = content.childCount; i < mMaxChildCount; ++i)
                {
                    var _newGO = GameObject.Instantiate(_firstChildRect.gameObject);

                    if (_newGO != null)
                    {
                        _newGO.transform.SetParent(content.transform, false);
                    }
                    else
                    {
                        ShowError("Create new item failed! please check!");

                        return false;
                    }
                }
            }
        }

        mCreateChildEndCallback?.Invoke();

        for (int i = 0; i < mMaxChildCount; ++i)
        {
            var _childTrans = content.GetChild(i);
            var _childGO = _childTrans.gameObject;

            mChildIndexMap.Add(_childGO, i);

            _childGO.SetActive(false);

            var _childRectTransform = _childTrans as RectTransform;

            if (_childRectTransform == null)
            {
                ShowError("There's no RectTransform in child! Please check!");

                return false;
            }

            _childRectTransform.anchorMax = new Vector2(0, 1f);

            _childRectTransform.anchorMin = new Vector2(0, 1f);
        }

        if (!InternalInitAfterCreateChild())
        {
            return false;
        }

        mHasCreateChild = true;

        return true;
    }


    protected override bool InternalInitAfterCreateChild()
    {
        for (int i = 0; i < content.childCount; ++i)
        {
            var _child = content.GetChild(i) as RectTransform;

            if (_child != null)
            {
                _child.sizeDelta = mItemSize;

                _child.name = i.ToString();
            }
        }

        return true;
    }
}
