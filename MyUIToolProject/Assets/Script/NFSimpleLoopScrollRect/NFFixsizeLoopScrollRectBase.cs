using System;
using System.Collections;
using System.Collections.Generic;
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
public abstract class NFFixsizeLoopScrollRectBase : ScrollRect
{
    protected Vector2 mScrollVelocity;


    /// <summary>
    /// first for GameObject, second for gameObject index, third for data index
    /// </summary>
    protected Action<GameObject, int, int> mRefreshDataCallback = null;


    /// <summary>
    /// 如果 content 下面没有内容，则需要传入创建函数
    /// </summary>
    private Func<GameObject> mCreateNewChildCallback = null;


    private Action mCreateChildEndCallback = null;


    private IEnumerator mCoroutine = null;


    protected int StartDataIndex
    {
        get;
        set;
    }


    protected int EndDataIndex
    {
        get;
        set;
    }


    protected Dictionary<GameObject, int> mChildIndexMap = new Dictionary<GameObject, int>();


    protected Vector2 mItemSize = Vector2.zero;


    protected Vector2 mHalfItemSize = Vector2.zero;


    public int ConstraintCount
    {
        get
        {
            if (mGridLayout != null)
            {
                // be careful, flexible is not allowed!
                if (mGridLayout.constraint == GridLayoutGroup.Constraint.Flexible)
                {
                    return 1;
                }

                return mGridLayout.constraintCount;
            }

            return 1;
        }
    }


    private RectOffset mEmptyPadding = null;


    public RectOffset Padding
    {
        get
        {
            if (mLayoutGroup != null)
            {
                return mLayoutGroup.padding;
            }

            if (mGridLayout != null)
            {
                return mGridLayout.padding;
            }

            if (mEmptyPadding == null)
            {
                mEmptyPadding = new RectOffset(0, 0, 0, 0);
            }

            return mEmptyPadding;
        }
    }


    public virtual Vector2 Spacing
    {
        get
        {
            if (mLayoutGroup != null)
            {
                // don't care if it is vertical or horizontal
                return new Vector2(mLayoutGroup.spacing, mLayoutGroup.spacing);
            }

            if (mGridLayout != null)
            {
                return mGridLayout.spacing;
            }

            return Vector2.zero;
        }
    }


    public TextAnchor ChildAlignment
    {
        get
        {
            if (mLayoutGroup != null)
            {
                return mLayoutGroup.childAlignment;
            }

            if (mGridLayout != null)
            {
                return mGridLayout.childAlignment;
            }

            return TextAnchor.UpperCenter;
        }
    }


    protected HorizontalOrVerticalLayoutGroup mLayoutGroup;


    protected GridLayoutGroup mGridLayout = null;


    private bool mHasInit = false;


    protected int mMaxChildCount = 3;


    private bool mIsFirstRefresh = true;


    protected int TotalCount
    {
        get;
        set;
    }


    private readonly Vector3[] m_Corners = new Vector3[4];


    protected override void OnDisable()
    {
        base.OnDisable();

        if (mCoroutine != null)
        {
            StopCoroutine(mCoroutine);

            mCoroutine = null;
        }
    }


    /// <summary>
    /// scroll to target, make target move to center of viewport
    /// 默认是滑动到中间，如果超过了 Clamp 的范围则直接停止
    /// </summary>
    /// <param name="index"></param>
    /// <param name="effectTime"></param>
    public void CenterOnCell(int index, float effectTime)
    {
        if (index < 0 || index >= TotalCount)
        {
            ShowError($"越界了！下标是：{index}，总数是：{TotalCount}");

            return;
        }

        if (content == null)
        {
            ShowError("没有 Content ，请检查！");

            return;
        }

        if (viewport == null)
        {
            ShowError("没有 ViewPort，请检查！");

            return;
        }

        if (mCoroutine != null)
        {
            StopCoroutine(mCoroutine);

            mCoroutine = null;
        }

        StopMovement();

        mScrollVelocity = Vector2.zero;

        mCanDrag = false;

        StartCoroutine(InternalScrollToTarget(index, effectTime));
    }


    protected bool mCanDrag = true;


    public override void OnDrag(PointerEventData eventData)
    {
        if (!mCanDrag)
        {
            return;
        }

        base.OnDrag(eventData);
    }


    protected Vector3 mMoveDest = Vector3.zero;


    protected float mEffectPlayedTime = 0;


    /// <summary>
    /// 横跨的最大行数或者列数
    /// </summary>
    protected int mMaxSpanCount = 0;


    /// <summary>
    /// 因为是固定大小的，所以输入一个下标可以获得是哪一行
    /// </summary>
    /// <returns></returns>
    protected abstract int GetRowIndexByIndex(int index);


    /// <summary>
    /// 因为是固定大小的，所以输入一个 下标可以获得是那一列
    /// </summary>
    /// <returns></returns>
    protected abstract int GetColIndexByIndex(int index);


    protected abstract IEnumerator InternalScrollToTarget(int index, float totalTime);


    private bool mHasCreateChild = false;


    /// <summary>
    /// init function
    /// 初始化函数
    /// </summary>
    /// <param name="refreshCallback">to refresh item callback</param>
    /// <param name="createChildCallback">create child callback</param>
    /// <param name="createChildEndCallback">for make cache of gameobject of items, can be null</param>
    /// <param name="createChildNow">是否直接创建子节点，有一些需求可能不需要一开始就创建</param>
    public bool InitData(
        Action<GameObject, int, int> refreshCallback,
        Func<GameObject> createChildCallback,
        Action createChildEndCallback,
        bool createChildNow
    )
    {
        if (mHasInit)
        {
            return true;
        }

        if (refreshCallback == null)
        {
            ShowError("Init failed for paramater : refreshCallback is empty!");

            return false;
        }

        if (this.viewport == null)
        {
            ShowError("Viewport is empty, please check!");

            return false;
        }

        if (this.content == null)
        {
            ShowError("Scroll rect's content is empty! please check!");

            return false;
        }

        mCreateChildEndCallback = createChildEndCallback;

        mCreateNewChildCallback = createChildCallback;

        mRefreshDataCallback = refreshCallback;

        if (mCreateNewChildCallback == null && content.childCount < 1)
        {
            ShowError("当前条件下无法创建新的 Child，请检查！");

            return false;
        }

        // 不要用这个组件，该组件会增加消耗，因为单个 item 的大小是确定的，那么总的大小可以根据数量算出来
        {
            var _contentSizeFitter = content.GetComponent<ContentSizeFitter>();

            if (_contentSizeFitter != null)
            {
                _contentSizeFitter.enabled = false;
            }
        }

        mLayoutGroup = content.GetComponent<HorizontalOrVerticalLayoutGroup>();

        if (mLayoutGroup != null)
        {
            mLayoutGroup.enabled = false;
        }
        else
        {
            mGridLayout = content.GetComponent<GridLayoutGroup>();

            if (mGridLayout != null)
            {
                mGridLayout.enabled = false;
            }
        }

        if (createChildNow)
        {
            InternalCreateChild();
        }

        mHasInit = true;

        return true;
    }


    private bool InternalInitAfterCreateChild()
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


    private bool InternalCreateChild()
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

        if (mGridLayout == null)
        {
            mItemSize = new Vector2(_childRect.rect.width, _childRect.rect.height);
        }
        else
        {
            mGridLayout.enabled = false;
            mItemSize = mGridLayout.cellSize;
        }

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


    protected virtual float CalculateChildPosX(int rowIndex, int colIndex, RectTransform childRectTransform)
    {
        var _tempPosX = childRectTransform.pivot.x * mItemSize.x +
                        colIndex * (mItemSize.x + Spacing.x) +
                        Padding.left;

        return _tempPosX;
    }


    protected virtual float CalculateChildPosY(int rowIndex, int colIndex, RectTransform childRectTransform)
    {
        var _pivot = childRectTransform.pivot;

        var _childHeight = childRectTransform.rect.height;

        var _posY = -(Padding.top +
                      rowIndex * (Spacing.y + _childHeight) +
                      _childHeight * (1 - _pivot.y)
            );

        return _posY;
    }


    protected abstract int CalculateMaxChildCount();


    protected void ShowError(string content)
    {
        Debug.LogError(content);
    }


    protected override void LateUpdate()
    {
        base.LateUpdate();

        CheckChildSwap();
    }


    /// <summary>
    /// check if child need set pos and refresh
    /// </summary>
    protected abstract void CheckChildSwap();


    public void SetTotalCount(int targetCount)
    {
        if (TotalCount > targetCount)
        {
            if (EndDataIndex >= targetCount)
            {
                EndDataIndex = targetCount - 1;

                StartDataIndex = EndDataIndex + 1 - mMaxChildCount;

                if (StartDataIndex < 0)
                {
                    StartDataIndex = 0;
                }
            }
        }
        else if (targetCount > TotalCount)
        {
            EndDataIndex = StartDataIndex + mMaxChildCount - 1;

            if (EndDataIndex >= targetCount)
            {
                EndDataIndex = targetCount - 1;
            }
        }

        TotalCount = targetCount;

        UpdateContentSize();

        RefreshCells();
    }


    protected virtual void UpdateContentSize()
    {
        if (!mHasCreateChild)
        {
            InternalCreateChild();
        }
    }


    public void RefillCells()
    {
        StopMovement();

        if (!mHasCreateChild)
        {
            InternalCreateChild();
        }

        var _pos = content.anchoredPosition;

        if (vertical)
        {
            _pos.y = 0;
        }
        else if (horizontal)
        {
            _pos.x = 0;
        }

        content.anchoredPosition = _pos;

        StartDataIndex = 0;

        EndDataIndex = Mathf.Min(TotalCount - 1, mMaxChildCount - 1);

        RefreshCells();
    }


    public void RefreshCells()
    {
        if (!mHasInit)
        {
            ShowError("Please init first!");

            return;
        }

        if (!mHasCreateChild)
        {
            InternalCreateChild();
        }

        if (mIsFirstRefresh)
        {
            mIsFirstRefresh = false;

            StartDataIndex = 0;

            EndDataIndex = Mathf.Min(TotalCount - 1, mMaxChildCount - 1);
        }

        var _targetCount = Mathf.Min(mMaxChildCount, TotalCount);

        int _validCount = 0;

        for (int i = 0; i < _targetCount; ++i)
        {
            var _dataIndex = StartDataIndex + i;

            if (_dataIndex >= TotalCount)
            {
                break;
            }

            ++_validCount;

            var _childGO = content.GetChild(i).gameObject;

            _childGO.SetActive(true);

            mRefreshDataCallback.Invoke(_childGO, mChildIndexMap[_childGO], _dataIndex);
        }

        for (int i = _validCount; i < mMaxChildCount; ++i)
        {
            var _childTrans = content.GetChild(i);

            _childTrans.gameObject.SetActive(false);
        }

        UpdateAllChildPos();
    }


    /// <summary>
    /// 更新所有子节点的位置
    /// </summary>
    protected abstract void UpdateAllChildPos();


    /// <summary>
    /// 更新单个
    /// </summary>
    protected abstract void UpdateSingleChildPosByDataIndex();
}
