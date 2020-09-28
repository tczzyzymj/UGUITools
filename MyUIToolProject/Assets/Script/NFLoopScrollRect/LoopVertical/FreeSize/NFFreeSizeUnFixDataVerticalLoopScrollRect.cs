using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RectTransform = UnityEngine.RectTransform;


/// <summary>
/// 先测试一下不改变内容是否能够确认位置
/// </summary>
public class NFFreeSizeUnFixDataVerticalLoopScrollRect : NFFreeSizeLoopScrollRectBase
{
    public float MinChildHeight = 100f;


    protected Dictionary<int, float> mChildHeightMap = new Dictionary<int, float>();


    protected Dictionary<int, float> mChildPositionMap = new Dictionary<int, float>();


    protected override void Awake()
    {
        base.Awake();

        horizontal = false;

        vertical = true;
    }


    protected override int CalculateMaxChildCount()
    {
        float _maxSize = 0;

        float _targetSize = 0;

        _targetSize = MinChildHeight;

        _maxSize = this.viewport.rect.height;

        if (_targetSize < 0 || Mathf.Approximately(_targetSize, 0))
        {
            ShowError("Child's height is less then 1, please check!");

            return 0;
        }

        var _count = Mathf.CeilToInt(_maxSize / _targetSize) + 1;

        return _count * ConstraintCount;
    }


    protected override void CheckChildSwap()
    {
        // if not move, then don't check
        if (velocity.Equals(Vector2.zero) && mScrollVelocity.Equals(Vector2.zero))
        {
            return;
        }

        // if data count is less then child count, then skip
        if (TotalCount < mChildIndexMap.Count)
        {
            return;
        }

        if (velocity.y > 0 || mScrollVelocity.y > 0)
        {
            InternalUpdateMoveToMore();
        }
        else
        {
            InternalUpdateForMoveToLess();
        }
    }


    private void InternalUpdateMoveToMore()
    {
        var _childCount = content.childCount;

        bool _isOverview = false;

        for (int i = 0; i < _childCount; i += ConstraintCount)
        {
            if (EndDataIndex >= TotalCount - 1)
            {
                break;
            }

            var _childRect = content.GetChild(0) as RectTransform;

            if (_childRect == null)
            {
                ShowError("子节点没有 RectTransform 请检查！");

                continue;
            }

            Vector3[] _childPointArray = new Vector3[4];

            _childRect.GetWorldCorners(_childPointArray);

            // only need 0 , as min.y
            var _minPos = viewport.InverseTransformPoint(_childPointArray[0]);

            var _viewPortMaxPosY = viewport.rect.max.y;

            if (_minPos.y > _viewPortMaxPosY)
            {
                for (int j = i; j < i + ConstraintCount; ++j)
                {
                    if (EndDataIndex >= TotalCount - 1)
                    {
                        break;
                    }

                    RectTransform _targetRectTrans = null;

                    if (j == i)
                    {
                        _targetRectTrans = _childRect;
                    }
                    else
                    {
                        _targetRectTrans = content.GetChild(0) as RectTransform;
                    }

                    if (_targetRectTrans == null)
                    {
                        continue;
                    }

                    _targetRectTrans.SetAsLastSibling();

                    EndDataIndex++;

                    StartDataIndex++;

                    if (EndDataIndex >= TotalCount)
                    {
                        _targetRectTrans.gameObject.SetActive(false);
                    }
                    else
                    {
                        _targetRectTrans.gameObject.SetActive(true);

                        var _gameIndex = mChildIndexMap[_targetRectTrans.gameObject];

                        RefreshChildData(
                            _targetRectTrans.gameObject,
                            _gameIndex,
                            EndDataIndex
                        );

                        UpdateContentSize();

                        var _preRect = content.GetChild(content.childCount - 2) as RectTransform;

                        // 这里更新一下单个的位置
                        UpdateSingleChildPosByDataIndex(
                            _targetRectTrans,
                            _preRect,
                            false,
                            EndDataIndex
                        );
                    }
                }
            }
            else
            {
                break;
            }
        }

        if (_isOverview)
        {
            UpdateAllChildPos();
        }
    }


    public override void RefreshCells()
    {
        base.RefreshCells();

        UpdateContentSize();
    }


    protected override void UpdateContentSize()
    {
        base.UpdateContentSize();

        var _sizeDelta = content.sizeDelta;

        var _totalHeight = (TotalCount - 1) * Spacing.y + Padding.top + Padding.bottom;

        foreach (var _data in mChildHeightMap)
        {
            _totalHeight += _data.Value;
        }

        _sizeDelta.y = _totalHeight;

        content.sizeDelta = _sizeDelta;
    }


    private void InternalUpdateForMoveToLess()
    {
        var _childCount = content.childCount;

        bool _isOverview = false;

        for (int i = _childCount - 1; i >= 0; i -= ConstraintCount)
        {
            if (StartDataIndex <= 0)
            {
                break;
            }

            var _childRect = content.GetChild(_childCount - 1) as RectTransform;

            if (_childRect == null)
            {
                ShowError("子节点没有 RectTransform 请检查！");

                continue;
            }

            Vector3[] _childPointArray = new Vector3[4];

            _childRect.GetWorldCorners(_childPointArray);

            // only need 0 , as min.y
            var _maxPos = viewport.InverseTransformPoint(_childPointArray[2]);

            var _viewPortMinPosY = viewport.rect.min.y;

            if (_maxPos.y < _viewPortMinPosY)
            {
                for (int j = i; j > i - ConstraintCount; --j)
                {
                    if (StartDataIndex <= 0)
                    {
                        break;
                    }

                    RectTransform _targetRectTrans = null;

                    if (j == i)
                    {
                        _targetRectTrans = _childRect;
                    }
                    else
                    {
                        _targetRectTrans = content.GetChild(_childCount - 1) as RectTransform;
                    }

                    if (_targetRectTrans == null)
                    {
                        continue;
                    }

                    _targetRectTrans.SetAsFirstSibling();

                    StartDataIndex--;

                    EndDataIndex--;

                    _targetRectTrans.gameObject.SetActive(true);

                    var _gameIndex = mChildIndexMap[_targetRectTrans.gameObject];

                    RefreshChildData(
                        _targetRectTrans.gameObject,
                        _gameIndex,
                        StartDataIndex
                    );

                    UpdateContentSize();

                    var _preChildRect = content.GetChild(1) as RectTransform;

                    UpdateSingleChildPosByDataIndex(
                        _targetRectTrans,
                        _preChildRect,
                        true,
                        StartDataIndex
                    );
                }
            }
            else
            {
                break;
            }
        }

        if (_isOverview)
        {
            UpdateAllChildPos();
        }
    }


    protected override void RefreshChildData(GameObject go, int goIndex, int dataIndex)
    {
        base.RefreshChildData(go, goIndex, dataIndex);

        var _rectTrans = go.transform as RectTransform;

        if (_rectTrans == null)
        {
            ShowError("无法获得 RectTransform，请检查!");

            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTrans);

        mChildHeightMap[dataIndex] = _rectTrans.rect.height;
    }


    public override void RefillCells()
    {
        mChildHeightMap.Clear();

        mChildPositionMap.Clear();

        base.RefillCells();

        UpdateContentSize();
    }


    public override void RefillCellsFromEnd()
    {
        mChildHeightMap.Clear();

        mChildPositionMap.Clear();

        base.RefillCellsFromEnd();

        UpdateContentSize();
    }


    /// <summary>
    /// 更新单个数据
    /// </summary>
    /// <param name="targetRect"></param>
    /// <param name="preRectTransform"></param>
    /// <param name="updateFromFront"></param>
    /// <param name="dataIndex">数据下标</param>
    protected void UpdateSingleChildPosByDataIndex(
        RectTransform targetRect,
        RectTransform preRectTransform,
        bool updateFromFront,
        int dataIndex
    )
    {
        if (targetRect == null)
        {
            ShowError("错误，传入的 RectTransform 为空，请检查！");

            return;
        }

        if (mRefreshFromFront && dataIndex == 0)
        {
            preRectTransform = null;
        }

        Vector2 _targetPos = new Vector2(
            CalculateChildPosX(targetRect),
            CalculateChildPosY(
                targetRect,
                preRectTransform,
                updateFromFront
            )
        );

        if (mChildPositionMap.ContainsKey(dataIndex) && mChildHeightMap.ContainsKey(dataIndex))
        {
            var _oldPos = mChildPositionMap[dataIndex];

            var _posSpan = _oldPos - _targetPos.y;

            ShowError(
                string.Format(
                    "Index is :{0},_posSpan is : {1}",
                    dataIndex,
                    _posSpan
                )
            );

            if (!Mathf.Approximately(_posSpan, 0))
            {
                // 如果发生了大小的变化，那么需要做调整了
                if (updateFromFront)
                {
                    if (dataIndex != 0)
                    {
                        _targetPos.y = _oldPos;
                    }

                    for (int i = 1; i < content.childCount; ++i)
                    {
                        var _child = content.GetChild(i) as RectTransform;

                        var _tempPosition = _child.anchoredPosition;

                        _tempPosition.y += _posSpan;

                        _child.anchoredPosition = _tempPosition;

                        mChildPositionMap[dataIndex + i] = _tempPosition.y;
                    }

                    //{
                    //    var _tempPosition = content.localPosition;

                    //    _tempPosition.y += _posSpan;

                    //    content.localPosition = _tempPosition;
                    //}
                }
            }
        }

        targetRect.anchoredPosition = _targetPos;

        mChildPositionMap[dataIndex] = _targetPos.y;
    }


    protected override void UpdateAllChildPos()
    {
        int _endIndex = Mathf.Min(mMaxChildCount, TotalCount);

        if (mRefreshFromFront)
        {
            for (int i = 0; i < _endIndex; ++i)
            {
                var _child = content.GetChild(i) as RectTransform;

                if (_child == null)
                {
                    ShowError("Get child RectTransform error! Please check!");

                    continue;
                }

                RectTransform _preChild = null;

                if (i > 0)
                {
                    _preChild = content.GetChild(i - 1) as RectTransform;
                }

                UpdateSingleChildPosByDataIndex(
                    _child,
                    _preChild,
                    false,
                    i
                );
            }
        }
        else
        {
            for (int i = _endIndex - 1; i >= 0; --i)
            {
                var _child = content.GetChild(i) as RectTransform;

                if (_child == null)
                {
                    ShowError("Get child RectTransform error! Please check!");

                    continue;
                }

                RectTransform _preChild = null;

                if (i != _endIndex - 1)
                {
                    _preChild = content.GetChild(i + 1) as RectTransform;
                }

                UpdateSingleChildPosByDataIndex(
                    _child,
                    _preChild,
                    true,
                    i
                );
            }
        }
    }


    protected float CalculateChildPosY(
        RectTransform childRectTransform,
        RectTransform preChildRectTransform,
        bool updateForward
    )
    {
        var _childHeight = childRectTransform.rect.height;

        float _posY = 0;

        if (updateForward)
        {
            if (preChildRectTransform == null)
            {
                // 这里认为是第一个
                _posY = -(Padding.top + _childHeight * (1 - childRectTransform.pivot.y));
            }
            else
            {
                _posY = preChildRectTransform.localPosition.y +
                        (Spacing.y +
                         _childHeight * (1 - childRectTransform.pivot.y) +
                         preChildRectTransform.rect.height * (1 - preChildRectTransform.pivot.y));
            }
        }
        else
        {
            if (preChildRectTransform == null)
            {
                // 这里认为是第一个
                _posY = -(Padding.top + _childHeight * (1 - childRectTransform.pivot.y));
            }
            else
            {
                _posY = preChildRectTransform.localPosition.y -
                        (Spacing.y +
                         _childHeight * (1 - childRectTransform.pivot.y) +
                         preChildRectTransform.rect.height * (1 - preChildRectTransform.pivot.y));
            }
        }

        return _posY;
    }


    protected float CalculateChildPosX(
        RectTransform childRectTransform
    )
    {
        if (mGridLayout != null)
        {
            var _tempPosX = childRectTransform.pivot.x * ItemWidth +
                            Padding.left;

            return _tempPosX;
        }

        switch (ChildAlignment)
        {
            case TextAnchor.UpperLeft :
            {
                var _tempPosX = childRectTransform.pivot.x * ItemWidth +
                                Padding.left;

                return _tempPosX;
            }
            case TextAnchor.UpperRight :
            {
                var _tempX = content.rect.width -
                             (1 - childRectTransform.pivot.x) * ItemWidth +
                             Padding.left;

                return _tempX;
            }
            case TextAnchor.UpperCenter :
            default :
            {
                var _tempX = content.rect.width * 0.5f +
                             (childRectTransform.pivot.x - 0.5f) * ItemWidth +
                             Padding.left;

                return _tempX;
            }
        }
    }


    /// <summary>
    /// 因为是固定大小的，所以输入一个下标可以获得是哪一行
    /// </summary>
    /// <returns></returns>
    protected override int GetRowIndexByIndex(int index)
    {
        int _result = 0;

        while (index >= ConstraintCount)
        {
            index -= ConstraintCount;
            ++_result;
        }

        return _result;
    }


    /// <summary>
    /// 因为是固定大小的，所以输入一个 下标可以获得是那一列
    /// </summary>
    /// <returns></returns>
    protected override int GetColIndexByIndex(int index)
    {
        return index % ConstraintCount;
    }


    /// <summary>
    /// TODO ： 这里要重新写
    /// </summary>
    /// <param name="index"></param>
    /// <param name="totalTime"></param>
    /// <returns></returns>
    protected override IEnumerator InternalScrollToTarget(int index, float totalTime)
    {
        float _moveDistance = 0;

        if (!Mathf.Approximately(_moveDistance, 0))
        {
            // 如果距离是负数，表示 content 要往下移动
            bool _moveToLess = (_moveDistance < 0);

            mMoveDest = Vector3.zero;

            float _maxMoveDistance = 0;

            mEffectPlayedTime = 0;

            if (this.movementType != MovementType.Unrestricted)
            {
                Vector3[] _contentCorner = new Vector3[4];

                content.GetWorldCorners(_contentCorner);

                // 这里检测一下，看content能移动的最远距离是多少
                if (_moveToLess)
                {
                    var _contentMaxPoint = viewport.InverseTransformPoint(_contentCorner[2]);

                    _maxMoveDistance = viewport.rect.max.y - _contentMaxPoint.y;
                }
                else
                {
                    var _minPoint = viewport.InverseTransformPoint(_contentCorner[0]);

                    _maxMoveDistance = viewport.rect.min.y - _minPoint.y;
                }
            }

            if (!Mathf.Approximately(_maxMoveDistance, 0))
            {
                if (_moveToLess)
                {
                    if (_moveDistance < _maxMoveDistance)
                    {
                        _moveDistance = _maxMoveDistance;
                    }

                    mScrollVelocity.y = -1;
                }
                else
                {
                    if (_moveDistance > _maxMoveDistance)
                    {
                        _moveDistance = _maxMoveDistance;
                    }

                    mScrollVelocity.y = 1;
                }

                mMoveDest = content.localPosition;

                var _originLocalPos = mMoveDest;

                mMoveDest.y += _moveDistance;

                while (true)
                {
                    mEffectPlayedTime += Time.deltaTime;

                    float _progress = mEffectPlayedTime / totalTime;

                    if (_progress > 1.0f)
                    {
                        _progress = 1.0f;
                    }

                    var _currentPos = Vector3.Lerp(_originLocalPos, mMoveDest, _progress);

                    content.localPosition = _currentPos;

                    yield return null;

                    if (_progress >= 1.0f)
                    {
                        break;
                    }
                }
            }
        }

        yield return null;

        mCanDrag = true;

        mScrollVelocity = Vector2.zero;
    }
}
