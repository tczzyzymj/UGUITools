using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.UI;


[AddComponentMenu("NFFixsizeHorizontalLoopScrollRect")]
public class NFFixsizeHorizontalLoopScrollRect : NFFixsizeLoopScrollRectBase
{
    protected override void Awake()
    {
        base.Awake();

        horizontal = true;

        vertical = false;
    }


    protected override int CalculateMaxChildCount()
    {
        float _maxSize = 0;

        float _targetSize = 0;

        _targetSize = mItemSize.x;

        _maxSize = this.viewport.rect.width;

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

        if (velocity.x > 0 || mScrollVelocity.x > 0)
        {
            if (StartDataIndex - 1 < 0)
            {
                return;
            }

            RectTransform _rectTrans = null;

            for (int i = content.childCount - 1; i >= 0; --i)
            {
                // top to down, check last one
                var _last = content.GetChild(i);

                if (!_last.gameObject.activeInHierarchy)
                {
                    continue;
                }

                _rectTrans = _last.transform as RectTransform;

                break;
            }

            if (_rectTrans == null)
            {
                ShowError("There's no RectTransform in child! Please check!");

                return;
            }

            Vector3[] _childPointArray = new Vector3[4];

            _rectTrans.GetWorldCorners(_childPointArray);

            // only need 0 , as min.y
            var _min = viewport.InverseTransformPoint(_childPointArray[0]);

            if (_min.x > viewport.rect.max.x)
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

                UpdateChildPos(0, ConstraintCount);
            }
        }
        else
        {
            if (EndDataIndex + 1 >= TotalCount)
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
            var _max = viewport.InverseTransformPoint(_childPointArray[2]);

            if (_max.x < viewport.rect.min.x)
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

                    if (EndDataIndex >= TotalCount)
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

                UpdateChildPos(content.childCount - ConstraintCount, content.childCount);
            }
        }
    }


    protected override void UpdateChildPos(int childStartIndex = -1, int childEndIndex = -1)
    {
        int _startIndex = 0;

        int _endIndex = Mathf.Min(mMaxChildCount, TotalCount);

        if (childStartIndex > 0)
        {
            _startIndex = childStartIndex;
        }

        if (childEndIndex > 0)
        {
            _endIndex = childEndIndex;
        }

        var _finalEndIndex = _endIndex;

        for (int i = _startIndex; i < _finalEndIndex; ++i)
        {
            int _colIndex = 0;

            int _compareIndex = i + StartDataIndex;

            int _rowIndex = _compareIndex;

            while (_compareIndex >= ConstraintCount)
            {
                _compareIndex -= ConstraintCount;

                ++_colIndex;

                _rowIndex = _compareIndex;
            }

            var _child = content.GetChild(i) as RectTransform;

            if (_child == null)
            {
                ShowError("Get child RectTransform error! Please check!");

                continue;
            }

            _child.localPosition = new Vector3(
                CalculateChildPosX(_rowIndex, _colIndex, _child),
                CalculateChildPosY(_rowIndex, _colIndex, _child),
                0
            );
        }
    }


    /// <summary>
    /// 因为是固定大小的，所以输入一个下标可以获得是哪一行
    /// </summary>
    /// <returns></returns>
    protected override int GetRowIndexByIndex(int index)
    {
        return index % ConstraintCount;
    }


    /// <summary>
    /// 因为是固定大小的，所以输入一个 下标可以获得是那一列
    /// </summary>
    /// <returns></returns>
    protected override int GetColIndexByIndex(int index)
    {
        int _result = 0;

        while (index >= ConstraintCount)
        {
            index -= ConstraintCount;
            ++_result;
        }

        return _result;
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
            var _fromeColIndex = GetColIndexByIndex(StartDataIndex);

            var _endColIndex = GetColIndexByIndex(index);

            var _colSpan = _endColIndex - _fromeColIndex;

            var _tempDisX = _colSpan * (this.mItemSize.x + Spacing.x);

            _centerPos.x += _tempDisX;
        }

        _distance = viewport.rect.center.x - _centerPos.x;

        return _distance;
    }


    protected override IEnumerator InternalScrollToTarget(int index, float totalTime)
    {
        float _moveDistance = CalculateForMoveDistance(index);

        if (!Mathf.Approximately(_moveDistance, 0))
        {
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

                    _maxMoveDistance = viewport.rect.max.x - _contentMaxPoint.x;
                }
                else
                {
                    var _minPoint = viewport.InverseTransformPoint(_contentCorner[0]);

                    _maxMoveDistance = viewport.rect.min.x - _minPoint.x;
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

                    mScrollVelocity.x = -1;
                }
                else
                {
                    if (_moveDistance > _maxMoveDistance)
                    {
                        _moveDistance = _maxMoveDistance;
                    }

                    mScrollVelocity.x = 1;
                }

                mMoveDest = content.localPosition;

                var _originLocalPos = mMoveDest;

                mMoveDest.x += _moveDistance;

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

                    if (_progress >= 1.0f)
                    {
                        break;
                    }

                    yield return null;
                }
            }
        }

        mCanDrag = true;

        mScrollVelocity = Vector2.zero;
    }
}
