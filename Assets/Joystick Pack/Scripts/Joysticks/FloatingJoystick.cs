using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick1 : Joystick
{
  
    protected override void Start()
    {
        base.Start();
        background.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        background.gameObject.SetActive(false);
        // Use the base class method to reset
        base.OnPointerUp(null);
    }

    void OnEnable()
    {
        background.gameObject.SetActive(false);
        base.OnPointerUp(null);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
        background.gameObject.SetActive(true);
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        background.gameObject.SetActive(false);
        base.OnPointerUp(eventData);
    }
}