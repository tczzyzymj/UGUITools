using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class NFSimpleLoopController : MonoBehaviour
{
    public NFFixsizeLoopScrollRect FixsizeLoopScrollRect;


    public InputField CountInputField;


    public Button CountInputButton;


    public InputField ScrollToIndexInputField;


    public Button ScrollToIndexButton;


    private List<NFLoopVerticleSampleItem> mItemList = new List<NFLoopVerticleSampleItem>();


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

        FixsizeLoopScrollRect.SetTotalCount(_targetCount);
    }


    private void OnClickScrollToTargetButton()
    {
        if (!int.TryParse(ScrollToIndexInputField.text, out var _targetIndex))
        {
            return;
        }

        this.FixsizeLoopScrollRect.ScrollToCell(_targetIndex, 3000);
    }


    [ContextMenu("执行初始化")]
    public void Init()
    {
        FixsizeLoopScrollRect.InitData(
            InternalRefreshItem,
            null,
            OnInitDataFinish
        );
    }


    [ContextMenu("刷新数据")]
    public void RefreshData()
    {
        FixsizeLoopScrollRect.RefreshCells();
    }


    private void OnInitDataFinish()
    {
        var _contentTrans = FixsizeLoopScrollRect.content.transform;

        for (int i = 0; i < _contentTrans.childCount; ++i)
        {
            var _child = _contentTrans.GetChild(i);

            var _targetCO = _child.gameObject.GetComponent<NFLoopVerticleSampleItem>();

            mItemList.Add(_targetCO);
        }
    }


    private void InternalRefreshItem(GameObject targetGO, int gameObjectIndex, int dataIndex)
    {
        mItemList[gameObjectIndex].RefreshData(dataIndex);
    }
}
