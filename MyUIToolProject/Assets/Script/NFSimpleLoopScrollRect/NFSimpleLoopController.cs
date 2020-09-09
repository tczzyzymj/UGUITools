﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NFSimpleLoopController : MonoBehaviour
{
    public NFSimpleLoopVerticle SimpleLoopVerticle;


    public InputField CountInputField;


    public Button CountInputButton;


    private List<NFLoopVerticleSampleItem> mItemList = new List<NFLoopVerticleSampleItem>();


    private void Awake()
    {
        CountInputButton.onClick.AddListener(OnClickCountInputButton);
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

        SimpleLoopVerticle.SetTotalCount(_targetCount);
    }


    [ContextMenu("执行初始化")]
    public void Init()
    {
        SimpleLoopVerticle.InitData(InternalRefreshItem, OnInitDataFinish, true);
    }


    private void OnInitDataFinish()
    {
        var _contentTrans = SimpleLoopVerticle.content.transform;

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