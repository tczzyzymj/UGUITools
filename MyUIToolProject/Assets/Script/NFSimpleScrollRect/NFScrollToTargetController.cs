using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class NFScrollToTargetController : MonoBehaviour
{
    public Button RefreshButton;


    public Button ScrollToButton;


    public InputField TotalCountInputField;


    public InputField ScrollToTargetInputField;


    public InputField SpeedInputField;


    public NFSimpleScrollRect ScrollRect;


    public GameObject Item;


    // Start is called before the first frame update
    void Start()
    {
        RefreshButton.onClick.AddListener(OnClickRefreshButton);

        ScrollToButton.onClick.AddListener(OnClickScrollToButton);

        SpeedInputField.text = "3000";
    }


    private void OnClickRefreshButton()
    {
        var _totalCountStr = TotalCountInputField.text;

        if (!int.TryParse(_totalCountStr, out var _totalCount))
        {
            UnityEngine.Debug.LogError("Please enter number!");

            return;
        }

        var _contentTrans = ScrollRect.content;

        for (int _i = _contentTrans.childCount; _i < _totalCount; ++_i)
        {
            var _newItem = GameObject.Instantiate(Item);

            _newItem.transform.SetParent(_contentTrans, false);
        }

        var _allTexts = _contentTrans.GetComponentsInChildren<Text>();

        for (int i = 0; i < _allTexts.Length; ++i)
        {
            _allTexts[i].text = (i + 1).ToString();
        }
    }


    private void OnClickScrollToButton()
    {
        var _indexStr = ScrollToTargetInputField.text;

        if (!int.TryParse(_indexStr, out var _targetIndex))
        {
            UnityEngine.Debug.LogError("Please enter number!");

            return;
        }

        var _speedStr = SpeedInputField.text;

        if (!int.TryParse(_speedStr, out var _speed))
        {
            UnityEngine.Debug.LogError("Please enter number!");

            return;
        }

        ScrollRect.ScrollToCell(_targetIndex, _speed);
    }
}
