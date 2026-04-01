using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingBar : MonoBehaviour
{
    protected Slider loadingBar;

    protected virtual void Awake()
    {
        loadingBar = GetComponentInChildren<Slider>();
        loadingBar.value = loadingBar.minValue;
    }
    
    public void SetValue(float value)
    {
        loadingBar.value = value / 0.9f;
    }

    protected void OnEnable()
    {
        SetValue(0);
    }
}
