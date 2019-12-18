using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 这里的 content 的长度是确定了的，提供简单的 scrollToTarget
/// </summary>
public class NFSimpleScrollRect : ScrollRect
{
    public bool IsScrolling
    {
        get;
        protected set;
    }


    private float mSpeed = 100f;


    private IEnumerator mCoroutine = null;


    private Bounds mViewBounds;


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

        if (!vertical && !horizontal)
        {
            UnityEngine.Debug.LogError("没有选择移动方向! 请选择!");

            return;
        }

        mSpeed = speed;

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
