using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick1 : Joystick
{
    protected override void Start()
    {
        base.Start();
        background.gameObject.SetActive(true);
    }

    void OnDisable()
    {
        // Use the base class method to reset the knob, but keep background visible
        base.OnPointerUp(null);
    }

    void OnEnable()
    {
        background.gameObject.SetActive(true);
        base.OnPointerUp(null);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        // No repositioning — background stays where it's placed in the editor
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        // Don't hide the background anymore, just reset the knob
        base.OnPointerUp(eventData);
    }
}