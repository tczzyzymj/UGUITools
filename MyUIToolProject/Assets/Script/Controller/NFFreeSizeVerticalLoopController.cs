using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class NFFreeSizeVerticalLoopController : MonoBehaviour
{
    public NFLoopScrollRectBase FixsizeLoopScrollRectBase;


    public InputField CountInputField;


    public Button CountInputButton;


    public InputField ScrollToIndexInputField;


    public Button ScrollToIndexButton;


    private List<NFFreeSizeFixDataSampleItem> mItemList =
        new List<NFFreeSizeFixDataSampleItem>();


    private static string mStr = "啊";


    private static System.Random mRandom = new System.Random((int) DateTime.UtcNow.Ticks);


    private List<string> mTargetDataList = new List<string>();


    private void Awake()
    {
        CountInputButton.onClick.AddListener(OnClickCountInputButton);

        ScrollToIndexButton.onClick.AddListener(OnClickScrollToTargetButton);

        Init();
    }


    private void OnClickCountInputButton()
    {
        if (CountInputField == null)
        {
            return;
        }

        if (!int.TryParse(CountInputField.text, out var _targetCount))
        {
            Debug.LogError("Error, can's parse string to number!");

            return;
        }

        FixsizeLoopScrollRectBase.SetTotalCount(_targetCount);

        // 这里初始化一下数据
        {
            mTargetDataList.Clear();

            for (int i = 0; i < _targetCount; ++i)
            {
                var _tempCount = mRandom.Next(20, 150);

                string _str = string.Empty;

                for (int _j = 0; _j < _tempCount; ++_j)
                {
                    _str += mStr;
                }

                mTargetDataList.Add(_str);
            }
        }

        FixsizeLoopScrollRectBase.RefreshCells();
    }


    private void OnClickScrollToTargetButton()
    {
        if (!int.TryParse(ScrollToIndexInputField.text, out var _targetIndex))
        {
            return;
        }

        this.FixsizeLoopScrollRectBase.CenterOnCell(_targetIndex, 0.3f);
    }


    [ContextMenu("执行初始化")]
    public void Init()
    {
        FixsizeLoopScrollRectBase.InitData(
            InternalRefreshItem,
            null,
            OnInitDataFinish,
            true
        );
    }


    [ContextMenu("刷新数据")]
    public void RefreshData()
    {
        FixsizeLoopScrollRectBase.RefreshCells();
    }


    private void OnInitDataFinish()
    {
        var _contentTrans = FixsizeLoopScrollRectBase.content.transform;

        for (int i = 0; i < _contentTrans.childCount; ++i)
        {
            var _child = _contentTrans.GetChild(i);

            var _targetCO = _child.gameObject.GetComponent<NFFreeSizeFixDataSampleItem>();

            mItemList.Add(_targetCO);
        }
    }


    private void InternalRefreshItem(GameObject targetGO, int gameObjectIndex, int dataIndex)
    {
        if (dataIndex < 0 || dataIndex >= FixsizeLoopScrollRectBase.TotalCount)
        {
            Debug.LogError($"越界了，下标是：{dataIndex}，总数：{FixsizeLoopScrollRectBase.TotalCount}");

            return;
        }

        mItemList[gameObjectIndex].RefreshData(dataIndex, mTargetDataList[dataIndex]);
    }
}
