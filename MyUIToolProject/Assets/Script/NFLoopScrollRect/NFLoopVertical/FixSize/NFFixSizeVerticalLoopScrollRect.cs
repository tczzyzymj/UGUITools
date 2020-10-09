using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class NFFixSizeVerticalLoopScrollRect : NFFixSizeLoopScrollRectBase
{
    protected override void Awake()
    {
        base.Awake();

        horizontal = false;

        vertical = true;
    }


    protected override void CalculateItemSize(RectTransform childRect)
    {
        if (childRect == null)
        {
            return;
        }

        if (mGridLayout != null)
        {
            mItemSize = mGridLayout.cellSize;

            return;
        }

        if (mLayoutGroup != null && mLayoutGroup.childForceExpandWidth)
        {
            mItemSize = new Vector2(content.rect.width, childRect.rect.height);
        }
        else
        {
            mItemSize = new Vector2(childRect.rect.width, childRect.rect.height);
        }
    }


    protected override int CalculateMaxChildCount()
    {
        float _maxSize = 0;

        float _targetSize = 0;

        _targetSize = mItemSize.y;

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

        bool _updatePos = false;

        bool _isOverview = false;

        int _changeCount = 0;

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
                _updatePos = true;

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

                    if (!Mathf.Approximately(
                        _targetRectTrans.anchoredPosition.y,
                        _childRect.anchoredPosition.y
                    ))
                    {
                        break;
                    }

                    _targetRectTrans.SetAsLastSibling();

                    ++_changeCount;

                    EndDataIndex++;

                    StartDataIndex++;

                    if (j == i && !_isOverview)
                    {
                        var _targetSpan = mMaxSpanCount - 1;

                        var _tempPosY = _minPos.y;

                        // 这里要检测一下，如果 StartDataIndex-- 的地方还是不能显示，那么继续减少
                        do
                        {
                            ++_targetSpan;

                            _tempPosY = _minPos.y - (_targetSpan * mItemSize.y + (_targetSpan - 1) * Spacing.y);
                        } while (_tempPosY > viewport.rect.max.y);

                        var _span = _targetSpan - mMaxSpanCount;

                        if (_span > 0)
                        {
                            _isOverview = true;

                            StartDataIndex += (ConstraintCount * _span);

                            EndDataIndex += (ConstraintCount * _span);
                        }
                    }

                    if (EndDataIndex >= TotalCount)
                    {
                        _targetRectTrans.gameObject.SetActive(false);
                    }
                    else
                    {
                        _targetRectTrans.gameObject.SetActive(true);

                        RefreshChildData(
                            _targetRectTrans.gameObject,
                            mChildIndexMap[_targetRectTrans.gameObject],
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

        if (_updatePos)
        {
            if (_isOverview)
            {
                UpdateAllChildPos();
            }
            else
            {
                UpdateAllChildPos();
            }
        }
    }


    protected override void UpdateContentSize()
    {
        base.UpdateContentSize();

        var _sizeDelta = content.sizeDelta;

        var _totalCount = TotalCount;

        if (ConstraintCount > 1)
        {
            _totalCount = Mathf.CeilToInt((float) TotalCount / ConstraintCount);
        }

        var _height = _totalCount * mItemSize.y + (_totalCount - 1) * Spacing.y + Padding.top + Padding.bottom;

        _sizeDelta.y = _height;

        content.sizeDelta = _sizeDelta;
    }


    private void InternalUpdateForMoveToLess()
    {
        var _childCount = content.childCount;

        bool _updatePos = false;

        bool _isOverview = false;

        int _moveCount = 0;

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
                _updatePos = true;

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

                    if (!Mathf.Approximately(
                        _targetRectTrans.anchoredPosition.y,
                        _childRect.anchoredPosition.y
                    ))
                    {
                        break;
                    }

                    ++_moveCount;

                    _targetRectTrans.SetAsFirstSibling();

                    StartDataIndex--;

                    EndDataIndex--;

                    if (j == i && !_isOverview)
                    {
                        var _targetSpan = mMaxSpanCount - 1;

                        var _tempMaxY = _maxPos.y;

                        // 这里要检测一下，如果 StartDataIndex-- 的地方还是不能显示，那么继续减少
                        do
                        {
                            ++_targetSpan;
                            _tempMaxY = _maxPos.y + (_targetSpan * mItemSize.y + (_targetSpan - 1) * Spacing.y);
                        } while (_tempMaxY < _viewPortMinPosY);

                        var _span = _targetSpan - mMaxSpanCount;

                        if (_span > 0)
                        {
                            _isOverview = true;

                            StartDataIndex -= (ConstraintCount * _span);

                            EndDataIndex -= (ConstraintCount * _span);
                        }
                    }

                    _targetRectTrans.gameObject.SetActive(true);

                    RefreshChildData(
                        _targetRectTrans.gameObject,
                        mChildIndexMap[_targetRectTrans.gameObject],
                        StartDataIndex
                    );
                }
            }
            else
            {
                break;
            }
        }

        if (_updatePos)
        {
            if (_isOverview)
            {
                UpdateAllChildPos();
            }
            else
            {
                UpdateAllChildPos();
            }
        }
    }


    protected override void UpdateAllChildPos()
    {
        int _startIndex = 0;

        int _endIndex = Mathf.Min(mMaxChildCount, TotalCount);

        var _finalEndIndex = _endIndex;

        for (int i = _startIndex; i < _finalEndIndex; ++i)
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

            var _targetPos = new Vector3(
                CalculateChildPosX(_rowIndex, _colIndex, _child),
                CalculateChildPosY(_rowIndex, _colIndex, _child),
                0
            );

            _child.anchoredPosition = _targetPos;
        }
    }


    protected float CalculateChildPosY(int rowIndex, int colIndex, RectTransform childRectTransform)
    {
        var _pivot = childRectTransform.pivot;

        var _childHeight = childRectTransform.rect.height;

        var _posY = -(Padding.top +
                      rowIndex * (Spacing.y + _childHeight) +
                      _childHeight * (1 - _pivot.y)
            );

        return _posY;
    }


    protected float CalculateChildPosX(int rowIndex, int colIndex, RectTransform childRectTransform)
    {
        if (mGridLayout != null)
        {
            var _tempPosX = childRectTransform.pivot.x * mItemSize.x +
                            colIndex * (mItemSize.x + Spacing.x) +
                            Padding.left;

            return _tempPosX;
        }

        switch (ChildAlignment)
        {
            case TextAnchor.UpperLeft :
            {
                var _tempPosX = childRectTransform.pivot.x * mItemSize.x +
                                colIndex * (mItemSize.x + Spacing.x) +
                                Padding.left;

                return _tempPosX;
            }
            case TextAnchor.UpperRight :
            {
                var _tempX = content.rect.width -
                             (1 - childRectTransform.pivot.x) * mItemSize.x +
                             Padding.left;

                return _tempX;
            }
            case TextAnchor.UpperCenter :
            default :
            {
                var _tempX = content.rect.width * 0.5f +
                             (childRectTransform.pivot.x - 0.5f) * mItemSize.x +
                             Padding.left;

                return _tempX;
            }
        }
    }


    private float CalculateForMoveDistance(int index)
    {
        float _distance = 0;

        int _childIndex = 0;

        bool _isOverview = false;

        if (index >= StartDataIndex && index <= EndDataIndex)
        {
            _childIndex = index - StartDataIndex;
        }
        else
        {
            _isOverview = true;
        }

        var _child = content.GetChild(_childIndex) as RectTransform;

        Vector3[] _childCorner = new Vector3[4];

        _child.GetWorldCorners(_childCorner);

        Vector3 _centerPos = Vector3.zero;

        var _tempCorner_0 = viewport.InverseTransformPoint(_childCorner[0]);

        var _tempCorner_1 = viewport.InverseTransformPoint(_childCorner[2]);

        _centerPos = Vector3.Lerp(_tempCorner_0, _tempCorner_1, 0.5f);

        if (_isOverview)
        {
            // 如果是超出了范围的，那么就需要增加相对的距离
            var _fromRowIndex = GetRowIndexByIndex(StartDataIndex);

            var _endRowIndex = GetRowIndexByIndex(index);

            var _rowSpan = _fromRowIndex - _endRowIndex;

            var _tempDisY = _rowSpan * (this.mItemSize.y + Spacing.y);

            _centerPos.y += _tempDisY;
        }

        _distance = viewport.rect.center.y - _centerPos.y;

        return _distance;
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


    protected override IEnumerator InternalScrollToTarget(int index, float totalTime)
    {
        float _moveDistance = CalculateForMoveDistance(index);

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
