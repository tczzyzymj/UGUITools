using System;
using UnityEngine.UI;
using UnityEngine;
using Random = System.Random;


public class NFFreeSizeUnFixDataSampleItem : NFLoopVerticleSampleItem
{
    private static System.Random mRandom = new Random((int) DateTime.UtcNow.Ticks);


    private static string mStr = "啊";


    public override void RefreshData(int index)
    {
        var _count = mRandom.Next(10, 150);

        string _str = string.Empty;

        for (int _j = 0; _j < _count; ++_j)
        {
            _str += mStr;
        }

        this.TargetText.text = $"{(index + 1)}、 {_str}";

        this.gameObject.name = (index + 1).ToString();
    }
}
