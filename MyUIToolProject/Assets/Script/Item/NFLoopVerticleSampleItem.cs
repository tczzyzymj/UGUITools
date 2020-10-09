using System;
using UnityEngine.UI;
using UnityEngine;


public class NFLoopVerticleSampleItem : MonoBehaviour
{
    public Text TargetText;


    public virtual void RefreshData(int index)
    {
        TargetText.text = (index + 1).ToString();

        this.gameObject.name = TargetText.text;
    }
}
