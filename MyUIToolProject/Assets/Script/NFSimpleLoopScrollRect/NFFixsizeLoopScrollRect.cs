using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Rendering;
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
public class NFFixsizeLoopScrollRect : ScrollRect
{
    public bool IsScrolling
    {
        get;
        protected set;
    }


    /// <summary>
    /// first for GameObject, second for gameObject index, third for data index
    /// </summary>
    private Action<GameObject, int, int> mRefreshDataCallback = null;


    private IEnumerator mCoroutine = null;


    private int StartDataIndex
    {
        get;
        set;
    }


    private int EndDataIndex
    {
        get;
        set;
    }


    private Dictionary<GameObject, int> mChildIndexMap = new Dictionary<GameObject, int>();


    private Vector2 mItemSize = Vector2.zero;


    private Vector2 mHalfItemSize = Vector2.zero;


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


    private HorizontalOrVerticalLayoutGroup mLayoutGroup;


    private GridLayoutGroup mGridLayout = null;


    private bool mHasInit = false;


    private int mMaxChildCount = 3;


    private bool mIsFirstRefresh = true;


    private int mTotalCount = 0;


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


    protected override void Awake()
    {
        base.Awake();

        horizontal = false;

        vertical = true;
    }


    /// <summary>
    /// scroll to target, make target move to center of viewport
    /// 默认是滑动到中间，如果超过了 Clamp 的范围则直接停止
    /// </summary>
    /// <param name="index"></param>
    /// <param name="speed"></param>
    public void ScrollToCell(int index, float speed)
    {
        if (index < 0 || index >= mTotalCount)
        {
            ShowError("越界了！");

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

        IsScrolling = true;

        StartCoroutine(InternalScrollToTarget(index, speed));
    }


    private IEnumerator InternalScrollToTarget(int index, float speed)
    {
        yield break;
    }


    /// <summary>
    /// init function
    /// 初始化函数
    /// </summary>
    /// <param name="refreshCallback">to refresh item callback</param>
    /// <param name="createChildEndCallback">for make cache of gameobject of items, can be null</param>
    /// <param name="autoHideChildFirst">hide new child or just show it</param>
    public bool InitData(
        Action<GameObject, int, int> refreshCallback,
        Action createChildEndCallback,
        bool autoHideChildFirst
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

        if (this.content.childCount < 1)
        {
            ShowError("Make sure item sample is in content");

            return false;
        }

        var _childRect = content.GetChild(0).transform as RectTransform;

        if (_childRect == null)
        {
            ShowError("Item's rect transform is empty!");

            return false;
        }

        var _contentSizeFitter = content.GetComponent<ContentSizeFitter>();

        if (_contentSizeFitter != null)
        {
            _contentSizeFitter.enabled = false;
        }

        mLayoutGroup = content.GetComponent<HorizontalOrVerticalLayoutGroup>();

        if (mLayoutGroup != null)
        {
            mLayoutGroup.enabled = false;
        }
        else
        {
            mGridLayout = content.GetComponent<GridLayoutGroup>();
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

        mMaxChildCount = GetMaxCount();

        if (content.childCount < mMaxChildCount)
        {
            for (int i = content.childCount; i < mMaxChildCount; ++i)
            {
                var _newGO = GameObject.Instantiate(_childRect.gameObject);

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

            createChildEndCallback?.Invoke();
        }

        for (int i = 0; i < mMaxChildCount; ++i)
        {
            var _childTrans = content.GetChild(i);
            var _childGO = _childTrans.gameObject;

            mChildIndexMap.Add(_childGO, i);

            _childGO.SetActive(!autoHideChildFirst);

            var _childRectTransform = _childTrans as RectTransform;

            if (_childRectTransform == null)
            {
                ShowError("There's no RectTransform in child! Please check!");

                return false;
            }

            _childRectTransform.anchorMax = new Vector2(0f, 1f);

            _childRectTransform.anchorMin = new Vector2(0f, 1f);

            _childRectTransform.pivot = new Vector2(0f, 1.0f);
        }

        mRefreshDataCallback = refreshCallback;

        // set child size, grid is not good
        if (mGridLayout != null)
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
        }

        mHasInit = true;

        return true;
    }


    private void InitForChildAlignment(RectTransform childRect)
    {
        if (childRect == null)
        {
            ShowError("Child rect transform is empty! Please check!");

            return;
        }

        childRect.anchorMin = new Vector2(0, 1);

        childRect.anchorMax = new Vector2(0, 1);
    }


    private int GetMaxCount()
    {
        float _maxSize = 0;

        float _targetSize = 0;

        if (vertical)
        {
            _targetSize = mItemSize.y;

            _maxSize = this.viewport.rect.height;
        }
        else if (horizontal)
        {
            _targetSize = mItemSize.x;

            _maxSize = this.viewport.rect.width;
        }
        else
        {
            ShowError("Please choose way of scroll direction!");
        }

        if (_targetSize < 0 || Mathf.Approximately(_targetSize, 0))
        {
            ShowError("Child's height is less then 1, please check!");

            return 0;
        }

        var _count = Mathf.CeilToInt(_maxSize / _targetSize) + 1;

        return _count * ConstraintCount;
    }


    private void ShowError(string content)
    {
        Debug.LogError(content);
    }


    protected override void LateUpdate()
    {
        base.LateUpdate();

        LateUpadateForRefreshChild();
    }


    /// <summary>
    /// check if child need set pos and refresh
    /// </summary>
    private void LateUpadateForRefreshChild()
    {
        // if not move, then don't check
        if (velocity.Equals(Vector2.zero))
        {
            return;
        }

        // if data count is less then child count, then skip
        if (mTotalCount < mChildIndexMap.Count)
        {
            return;
        }

        // here we need to compare with viewport, and coordinate system should use viewport

        if (Mathf.Approximately(velocity.y, 0))
        {
            return;
        }

        if (velocity.y > 0)
        {
            if (EndDataIndex + 1 >= mTotalCount)
            {
                return;
            }

            // down to top, check first one
            // test first acitve game object and last one
            var _first = content.GetChild(0);

            var _rectTrans = _first.transform as RectTransform;

            if (_rectTrans == null)
            {
                ShowError("There's no RectTransform in child! Please check!");

                return;
            }

            Vector3[] _childPointArray = new Vector3[4];

            _rectTrans.GetWorldCorners(_childPointArray);

            // only need 0 , as min.y
            var _minPos = viewport.InverseTransformPoint(_childPointArray[0]);

            if (_minPos.y > viewport.rect.max.y)
            {
                for (int i = 0; i < ConstraintCount; ++i)
                {
                    var _childTrans = content.GetChild(0) as RectTransform;

                    if (_childTrans == null)
                    {
                        continue;
                    }

                    _childTrans.SetAsLastSibling();

                    EndDataIndex++;

                    StartDataIndex++;

                    if (EndDataIndex >= mTotalCount)
                    {
                        _childTrans.gameObject.SetActive(false);
                    }
                    else
                    {
                        _childTrans.gameObject.SetActive(true);

                        mRefreshDataCallback(
                            _childTrans.gameObject,
                            mChildIndexMap[_childTrans.gameObject],
                            EndDataIndex
                        );
                    }
                }

                UpdateChildPos();
            }
        }
        else
        {
            if (StartDataIndex - 1 < 0)
            {
                return;
            }

            // top to down, check last one
            var _last = content.GetChild(mMaxChildCount - 1);

            var _rectTrans = _last.transform as RectTransform;

            if (_rectTrans == null)
            {
                ShowError("There's no RectTransform in child! Please check!");

                return;
            }

            Vector3[] _childPointArray = new Vector3[4];

            _rectTrans.GetWorldCorners(_childPointArray);

            // only need 0 , as min.y
            var _maxPos = viewport.InverseTransformPoint(_childPointArray[1]);

            if (_maxPos.y < viewport.rect.min.y)
            {
                for (int i = 0; i < ConstraintCount; ++i)
                {
                    var _childTrans = content.GetChild(content.childCount - 1) as RectTransform;

                    if (_childTrans == null)
                    {
                        continue;
                    }

                    _childTrans.SetAsFirstSibling();

                    StartDataIndex--;

                    EndDataIndex--;

                    if (StartDataIndex < 0)
                    {
                        _childTrans.gameObject.SetActive(false);
                    }
                    else
                    {
                        _childTrans.gameObject.SetActive(true);

                        mRefreshDataCallback(
                            _childTrans.gameObject,
                            mChildIndexMap[_childTrans.gameObject],
                            StartDataIndex
                        );
                    }
                }

                UpdateChildPos();
            }
        }
    }


    public void SetTotalCount(int targetCount)
    {
        if (mTotalCount > targetCount)
        {
            EndDataIndex = targetCount - 1;

            StartDataIndex = EndDataIndex + 1 - mMaxChildCount;

            if (StartDataIndex < 0)
            {
                StartDataIndex = 0;
            }
        }
        else if (targetCount > mTotalCount)
        {
            EndDataIndex = StartDataIndex + mMaxChildCount - 1;

            if (EndDataIndex >= targetCount)
            {
                EndDataIndex = targetCount - 1;
            }
        }

        mTotalCount = targetCount;

        UpdateContentSize();

        RefreshCells();
    }


    private void UpdateContentSize()
    {
        var _sizeDelta = content.sizeDelta;

        var _totalCount = mTotalCount;

        if (ConstraintCount > 1)
        {
            _totalCount = Mathf.CeilToInt((float) mTotalCount / ConstraintCount);
        }

        if (vertical)
        {
            var _height = _totalCount * mItemSize.y + (_totalCount - 1) * Spacing.y + Padding.top + Padding.bottom;

            _sizeDelta.y = _height;
        }
        else if (horizontal)
        {
            var _width = _totalCount * mItemSize.x + (_totalCount - 1) * Spacing.x + Padding.left + Padding.right;

            _sizeDelta.x = _width;
        }
        else
        {
            ShowError("Please choose way of scroll direction!");
        }


        content.sizeDelta = _sizeDelta;
    }


    public void RefreshCells()
    {
        if (!mHasInit)
        {
            ShowError("Please init first!");

            return;
        }

        if (mIsFirstRefresh)
        {
            mIsFirstRefresh = false;

            StartDataIndex = 0;

            EndDataIndex = Mathf.Min(mTotalCount - 1, mMaxChildCount - 1);
        }

        var _targetCount = Mathf.Min(mMaxChildCount, mTotalCount);

        int _validCount = 0;

        for (int i = 0; i < _targetCount; ++i)
        {
            var _dataIndex = StartDataIndex + i;

            if (_dataIndex > mTotalCount)
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

        UpdateChildPos();
    }


    /// <summary>
    /// update child position
    /// </summary>
    private void UpdateChildPos()
    {
        if ((mGridLayout != null && mGridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount) ||
            (mLayoutGroup != null && mLayoutGroup is VerticalLayoutGroup)
        )
        {
            var _targetCount = Mathf.Min(mMaxChildCount, mTotalCount);

            for (int i = 0; i < _targetCount; ++i)
            {
                int _rowIndex = 0;

                int _compareIndex = i + StartDataIndex;

                int _colIndex = _compareIndex;

                while (_compareIndex >= ConstraintCount)
                {
                    _compareIndex -= ConstraintCount;

                    ++_rowIndex;

                    _colIndex = _compareIndex;
                }

                var _child = content.GetChild(i) as RectTransform;

                if (_child == null)
                {
                    ShowError("Get child RectTransform error! Please check!");

                    continue;
                }

                _child.anchoredPosition = new Vector3(
                    CalculateChildPosX(_rowIndex, _colIndex, _child),
                    CalculateChildPosY(_rowIndex, _colIndex, _child),
                    0
                );
            }
        }
        else if ((mGridLayout != null && mGridLayout.constraint == GridLayoutGroup.Constraint.FixedRowCount) ||
                 (mLayoutGroup != null && mLayoutGroup is HorizontalLayoutGroup))
        {
        }
    }


    private float CalculateChildPosX(
        int rowIndex,
        int colIndex,
        RectTransform childRectTransform
    )
    {
        var _pivot = childRectTransform.pivot;

        var _childWidth = childRectTransform.rect.width;

        switch (ChildAlignment)
        {
            case TextAnchor.UpperCenter :
            {
                return Padding.left -
                       Padding.right +
                       (content.rect.width * 0.5f -
                        _childWidth * 0.5f +
                        _childWidth * _pivot.x);
            }
            case TextAnchor.UpperLeft :
            {
                return Padding.left - Padding.right + _childWidth * _pivot.x + _childWidth * colIndex + Spacing.x * colIndex;
            }
            case TextAnchor.UpperRight :
            {
                return Padding.left - Padding.right + (content.rect.width - _childWidth * (1 - _pivot.x));
            }
            default :
            {
                // don't deal other

                break;
            }
        }

        return 0;
    }


    private float CalculateChildPosY(
        int rowIndex,
        int colIndex,
        RectTransform childRectTransform
    )
    {
        var _pivot = childRectTransform.pivot;

        var _childHeight = childRectTransform.rect.height;

        return -(Padding.top +
                 rowIndex * Spacing.y +
                 rowIndex * _childHeight +
                 _childHeight * (1 - _pivot.y)
            );
    }


    protected virtual Bounds GetViewBound()
    {
        return new Bounds(viewRect.rect.center, viewRect.rect.size);
    }


    private Bounds GetContentBound()
    {
        if (content == null)
        {
            return new Bounds();
        }

        var _vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

        var _vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        var _toLocal = viewRect.worldToLocalMatrix;

        Vector3[] _worldCorners = new Vector3[4];

        var _rectTrans = (content.transform as RectTransform);

        if (_rectTrans == null)
        {
            ShowError("No RectTransform, Please check!");

            return new Bounds();
        }

        _rectTrans.GetWorldCorners(_worldCorners);

        for (int i = 0; i < 4; ++i)
        {
            Vector3 _v = _toLocal.MultiplyPoint3x4(_worldCorners[i]);
            _vMin = Vector3.Min(_v, _vMin);
            _vMax = Vector3.Max(_v, _vMax);
        }

        var _bounds = new Bounds(_vMin, Vector3.zero);

        _bounds.Encapsulate(_vMax);

        return _bounds;
    }
}
