using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
    private Action<GameObject, int, int> mRefreshCallback = null;


    private IEnumerator mCoroutine = null;


    private int mStartDataIndex;


    private int mEndDataIndex;


    private Dictionary<GameObject, int> mChildIndexMap = new Dictionary<GameObject, int>();


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
    /// 默认是滑动到中间，如果超过了 Clanp 的范围则直接停止
    /// </summary>
    /// <param name="index"></param>
    /// <param name="speed"></param>
    public void ScrollToCell(int index, float speed)
    {
        if (index < 0 || index >= mTotalCount)
        {
            UnityEngine.Debug.LogError("越界了！");

            return;
        }

        if (content == null)
        {
            UnityEngine.Debug.LogError("没有 Content ，请检查！");

            return;
        }

        if (viewport == null)
        {
            UnityEngine.Debug.LogError("没有 ViewPort，请检查！");

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
        // first stop the scroll movement
        // 首先要停止一下 scroll 组件的运动
        this.StopMovement();

        yield return null;

        bool _needMoving = true;

        var _viewBound = GetViewBound();

        var _itemBound = GetItemBound(index);

        var _contentBound = GetContentBound();

        float _maxDistance = 0;

        bool _isPositiveDir = true; // 是否是正向的

        float _itemMoveDistance = 0;

        // 这里去计算一下目标相对中间的位置
        {
            if (vertical)
            {
                _itemMoveDistance = _viewBound.center.y - _itemBound.center.y;
            }
            else
            {
                _itemMoveDistance = _viewBound.center.x - _itemBound.center.x;
            }

            if (_itemMoveDistance < 0)
            {
                _isPositiveDir = false;

                _itemMoveDistance = -_itemMoveDistance;
            }
        }

        // END

        // 这里计算一下可以移动的实际位置
        float _contentMoveDistance = 0;

        if (_isPositiveDir)
        {
            if (vertical)
            {
                _contentMoveDistance = _viewBound.min.y - _contentBound.min.y;
            }
            else
            {
                _contentMoveDistance = _viewBound.min.x - _contentBound.min.x;
            }
        }
        else
        {
            if (vertical)
            {
                _contentMoveDistance = _contentBound.max.y - _viewBound.max.y;
            }
            else
            {
                _contentMoveDistance = _contentBound.max.x - _viewBound.max.x;
            }
        }

        if (_contentMoveDistance < 0)
        {
            _maxDistance = 0;
        }
        else
        {
            // 这里去默认 content 的宽度或者说高度是大于单个子物体的
            // 如果不是，请修改！

            if (_contentMoveDistance < _itemMoveDistance)
            {
                _maxDistance = _contentMoveDistance;
            }
            else
            {
                _maxDistance = _itemMoveDistance;
            }
        }

        if (Mathf.Approximately(_maxDistance, 0) || _maxDistance < 0)
        {
            // 不能移动，这里直接就不用去了
            _needMoving = false;
        }

        float _movedDistance = 0;

        while (_needMoving)
        {
            _itemBound = GetItemBound(index);

            float _offset = speed * Time.deltaTime;

            _movedDistance += _offset;

            if (_movedDistance > _maxDistance)
            {
                _offset -= (_movedDistance - _maxDistance);

                _needMoving = false;
            }

            if (!_isPositiveDir)
            {
                _offset = -_offset;
            }

            var _tempPos = content.anchoredPosition;

            if (vertical)
            {
                _tempPos.y += _offset;
            }
            else
            {
                _tempPos.x += _offset;
            }

            base.SetContentAnchoredPosition(_tempPos);

            yield return null;
        }

        IsScrolling = false;
    }


    private bool mHasInit = false;


    private int mMaxCount = 3;


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
            Debug.LogError("Init failed for paramater : refreshCallback is empty!");

            return false;
        }

        if (this.viewport == null)
        {
            Debug.LogError("Viewport is empty, please check!");

            return false;
        }

        if (this.content == null)
        {
            Debug.LogError("Scroll rect's content is empty! please check!");

            return false;
        }

        if (this.content.childCount < 1)
        {
            Debug.LogError("Make sure item sample is in content");

            return false;
        }

        var _viewPortHeight = this.viewport.rect.height;

        var _rectTrans = content.GetChild(0).transform as RectTransform;

        if (_rectTrans == null)
        {
            Debug.LogError("Item's rect transform is empty!");

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

        var _itemHeight = _rectTrans.rect.height;

        mItemSize = _itemHeight;

        mHalfItemSize = mItemSize * 0.5f;

        mMaxCount = Mathf.CeilToInt(_viewPortHeight / _itemHeight) + 1;

        if (content.childCount < mMaxCount)
        {
            for (int i = content.childCount; i < mMaxCount; ++i)
            {
                var _newGO = GameObject.Instantiate(_rectTrans.gameObject);

                if (_newGO != null)
                {
                    _newGO.transform.SetParent(content.transform, false);
                }
                else
                {
                    Debug.LogError("Create new item failed! please check!");

                    return false;
                }
            }

            createChildEndCallback?.Invoke();
        }

        for (int i = 0; i < content.childCount; ++i)
        {
            var _childTrans = content.GetChild(i);
            var _childGO = _childTrans.gameObject;

            mChildIndexMap.Add(_childGO, i);

            _childGO.SetActive(!autoHideChildFirst);

            var _childRectTransform = _childTrans as RectTransform;

            if (_childRectTransform == null)
            {
                Debug.LogError("There's no RectTransform in child! Please check!");

                return false;
            }

            _childRectTransform.anchorMax = new Vector2(0.5f, 1f);

            _childRectTransform.anchorMin = new Vector2(0.5f, 1f);

            _childRectTransform.pivot = new Vector2(0.5f, 1.0f);
        }

        mRefreshCallback = refreshCallback;

        mHasInit = true;

        return true;
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
            // down to top, check first one
            // test first acitve game object and last one
            var _first = content.GetChild(0);

            var _rectTrans = _first.transform as RectTransform;

            if (_rectTrans == null)
            {
                Debug.LogError("There's no RectTransform in child! Please check!");

                return;
            }

            Vector3[] _childPointArray = new Vector3[4];

            _rectTrans.GetWorldCorners(_childPointArray);

            // only need 0 , as min.y
            var _minPos = viewport.InverseTransformPoint(_childPointArray[0]);

            if (_minPos.y > viewport.rect.max.y)
            {
                if (mEndDataIndex + 1 >= mTotalCount)
                {
                    return;
                }

                _rectTrans.SetAsLastSibling();

                mStartDataIndex++;

                mEndDataIndex++;

                mRefreshCallback(_first.gameObject, mChildIndexMap[_first.gameObject], mEndDataIndex);

                UpdateChildPos();
            }
        }
        else
        {
            // top to down, check last one
            var _last = content.GetChild(content.childCount - 1);

            var _rectTrans = _last.transform as RectTransform;

            if (_rectTrans == null)
            {
                Debug.LogError("There's no RectTransform in child! Please check!");

                return;
            }

            Vector3[] _childPointArray = new Vector3[4];

            _rectTrans.GetWorldCorners(_childPointArray);

            // only need 0 , as min.y
            var _maxPos = viewport.InverseTransformPoint(_childPointArray[1]);

            if (_maxPos.y < viewport.rect.min.y)
            {
                if (mStartDataIndex - 1 < 0)
                {
                    return;
                }

                _rectTrans.SetAsFirstSibling();

                mStartDataIndex--;

                mEndDataIndex--;

                mRefreshCallback(_last.gameObject, mChildIndexMap[_last.gameObject], mStartDataIndex);

                UpdateChildPos();
            }
        }
    }


    private int mTotalCount = 0;


    public void SetTotalCount(int totalCount)
    {
        mTotalCount = totalCount;

        UpdateContentSize();

        RefreshCells();
    }


    private bool mIsFirstRefresh = true;


    private void UpdateContentSize()
    {
        var _sizeDelta = content.sizeDelta;

        var _height = mTotalCount * mItemSize + (mTotalCount - 1) * Spacing + Padding.top + Padding.bottom;

        _sizeDelta.y = _height;

        content.sizeDelta = _sizeDelta;
    }


    public void RefreshCells()
    {
        if (!mHasInit)
        {
            Debug.LogError("Please init first!");

            return;
        }

        if (mIsFirstRefresh)
        {
            mIsFirstRefresh = false;

            mStartDataIndex = 0;

            mEndDataIndex = Mathf.Min(mTotalCount - 1, mMaxCount - 1);
        }

        var _targetCount = Mathf.Min(mMaxCount, mTotalCount);

        for (int i = 0; i < _targetCount; ++i)
        {
            var _dataIndex = mStartDataIndex + i;

            var _childGO = content.GetChild(i).gameObject;

            _childGO.SetActive(true);

            mRefreshCallback.Invoke(_childGO, mChildIndexMap[_childGO], _dataIndex);
        }

        for (int i = _targetCount; i < mMaxCount; ++i)
        {
            content.GetChild(i).gameObject.SetActive(false);
        }

        UpdateChildPos();
    }


    private float mItemSize = 156;


    private float mHalfItemSize = 156;


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


    public virtual float Spacing
    {
        get
        {
            if (mLayoutGroup != null)
            {
                return mLayoutGroup.spacing;
            }

            if (mGridLayout != null)
            {
                return mGridLayout.spacing.y;
            }

            return 0;
        }
    }


    private HorizontalOrVerticalLayoutGroup mLayoutGroup;


    private GridLayoutGroup mGridLayout = null;


    /// <summary>
    /// update child position
    /// </summary>
    private void UpdateChildPos()
    {
        var _targetCount = Mathf.Min(mTotalCount, content.childCount);

        float _spacing = 0;

        if (mLayoutGroup != null)
        {
            _spacing = mLayoutGroup.spacing;
        }

        for (int i = 0; i < _targetCount; ++i)
        {
            var _child = content.GetChild(i) as RectTransform;

            if (_child == null)
            {
                Debug.LogError("There's no RectTransform in child! Please check!");

                continue;
            }

            var _posY = -(mStartDataIndex + i) * (mItemSize + _spacing) - Padding.top;

            _child.anchoredPosition = new Vector3(
                0,
                _posY,
                0
            );
        }
    }


    protected virtual Bounds GetViewBound()
    {
        return new Bounds(viewRect.rect.center, viewRect.rect.size);
    }


    private readonly Vector3[] m_Corners = new Vector3[4];


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
            Debug.LogError("No RectTransform, Please check!");

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


    private Bounds GetItemBound(int index)
    {
        if (content == null)
        {
            return new Bounds();
        }

        var _vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

        var _vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        var _toLocal = viewRect.worldToLocalMatrix;

        int _offset = index;

        if (_offset < 0 || _offset >= content.childCount)
        {
            return new Bounds();
        }

        var _rt = content.GetChild(_offset) as RectTransform;

        if (_rt == null)
        {
            return new Bounds();
        }

        _rt.GetWorldCorners(m_Corners);

        for (int j = 0; j < 4; j++)
        {
            Vector3 _v = _toLocal.MultiplyPoint3x4(m_Corners[j]);
            _vMin = Vector3.Min(_v, _vMin);
            _vMax = Vector3.Max(_v, _vMax);
        }

        var _bounds = new Bounds(_vMin, Vector3.zero);

        _bounds.Encapsulate(_vMax);

        return _bounds;
    }
}
