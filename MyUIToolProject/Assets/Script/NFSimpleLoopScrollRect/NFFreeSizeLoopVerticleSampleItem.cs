using System;
using UnityEngine.UI;
using UnityEngine;
using Random = System.Random;


public class NFFreeSizeLoopVerticleSampleItem : MonoBehaviour
{
    public Text TargetText;


    public void RefreshData(int index, string targetData)
    {
        this.TargetText.text = $"{(index + 1)}、 {targetData}";

        this.gameObject.name = (index + 1).ToString();
    }
}
