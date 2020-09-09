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
public class NFSimpleLoopVerticle : ScrollRect
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


    private Bounds mViewBounds;


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
        if (index < 0 || index >= content.childCount)
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

        var _itemHeight = _rectTrans.rect.height;

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
            var _childGO = content.GetChild(i).gameObject;

            mChildIndexMap.Add(_childGO, i);

            _childGO.SetActive(!autoHideChildFirst);
        }

        mRefreshCallback = refreshCallback;

        mHasInit = true;

        return true;
    }


    protected override void LateUpdate()
    {
        base.LateUpdate();

        // if not move, then don't check
        if (velocity.Equals(Vector2.zero))
        {
            return;
        }

        // here we need to compare with viewport, and coordinate system should use viewport

        if (velocity.y > 0)
        {
            // down to top, check first one
            // test first acitve game object and last one
            var _first = content.GetChild(0);

            var _rectTrans = _first.transform as RectTransform;

            Vector3[] _childPointArray = new Vector3[4];

            _rectTrans.GetWorldCorners(_childPointArray);

            for (int i = 0; i < 4; ++i)
            {
                _childPointArray[i] = viewport.InverseTransformPoint(_childPointArray[i]);
            }

            Vector3[] _viewPortPointArray = new Vector3[4];

            viewport.GetLocalCorners(_viewPortPointArray);

            if (_childPointArray[0].y > viewport.rect.max.y)
            {
                if (mEndDataIndex + 1 >= -mTotalCount)
                {
                    return;
                }

                _rectTrans.SetAsLastSibling();

                mStartDataIndex++;

                mEndDataIndex++;

                mRefreshCallback(_first.gameObject, mChildIndexMap[_first.gameObject], mEndDataIndex);
            }
        }
        else
        {
            // top to down, check last one
            var _last = content.GetChild(0);

            var _rectTrans = _last.transform as RectTransform;

            if (_rectTrans.rect.min.y > content.rect.max.y)
            {
                _rectTrans.SetAsFirstSibling();

                if (mStartDataIndex - 1 <= 0)
                {
                    return;
                }

                mStartDataIndex--;

                mEndDataIndex--;

                mRefreshCallback(_last.gameObject, mChildIndexMap[_last.gameObject], mStartDataIndex);
            }
        }
    }


    private int mTotalCount = 0;


    public void SetTotalCount(int totalCount)
    {
        mTotalCount = totalCount;

        RefreshCells();
    }


    private bool mIsFirstRefresh = true;


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
