using System;
using UnityEngine.UI;
using UnityEngine;
using Random = System.Random;


public class NFFreesizeLoopVerticleSampleItem : MonoBehaviour
{
    public Text TargetText;


    private static string mStr = "啊";


    private static System.Random mRandom = new Random((int) DateTime.UtcNow.Ticks);


    public void RefreshData(int index)
    {
        var _targetCount = mRandom.Next(20, 150);

        string _str = string.Empty;

        for (int i = 10; i < _targetCount; ++i)
        {
            _str += mStr;
        }

        this.TargetText.text = $"{(index + 1)}、 {_str}";

        this.gameObject.name = (index + 1).ToString();
    }
}
