using UnityEngine;

#if UNITY_SWITCH
using nn.hid;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.RayTracingAccelerationStructure;
#endif 

public class SwitchControllerSupport : MonoBehaviour
{
#if UNITY_SWITCH
    PlayerInputManager playerInputManager; 

    private NpadId[] npadIds =
    {
        NpadId.Handheld,
        NpadId.No1,
        NpadId.No2,
        NpadId.No3,
        NpadId.No4
    };

    private NpadState npadState = new NpadState();
    private long[] prevButtons;

    private ControllerSupportArg controllerSupportArg = new ControllerSupportArg();
    private nn.Result result = new nn.Result();

    private bool initialized;
    bool canShowApplet = false; 
    private HashSet<int> spawnedDeviceIds = new HashSet<int>();

    void OnEnable()
    {
        playerInputManager = GetComponent<PlayerInputManager>();

        Npad.Initialize();

        Npad.SetSupportedIdType(npadIds);

        Npad.SetSupportedStyleSet(
            NpadStyle.FullKey |
            NpadStyle.Handheld |
            NpadStyle.JoyDual |
            NpadStyle.JoyLeft |
            NpadStyle.JoyRight
        );

        NpadJoy.SetHoldType(NpadJoyHoldType.Horizontal);

        prevButtons = new long[npadIds.Length];

        ResetAllAssignments();
        Invoke(nameof(ToggleCanShowApplet), .5f);
    }

    void ToggleCanShowApplet()
    {
        canShowApplet = !canShowApplet;
    }

    void ResetAllAssignments()
    {
        for (int i = 1; i < npadIds.Length; i++)
        {
            NpadJoy.SetAssignmentModeDual(npadIds[i]);
        }
    }

    void Update()
    {
        if (!canShowApplet) return; 

        NpadButton pressed = 0;

        for (int i = 0; i < npadIds.Length; i++)
        {
            var id = npadIds[i];
            var style = Npad.GetStyleSet(id);

            if (style == NpadStyle.None)
                continue;

            Npad.GetState(ref npadState, id, style);

            pressed |= ((NpadButton)prevButtons[i] ^ npadState.buttons) & npadState.buttons;

            prevButtons[i] = (long)npadState.buttons;
        }

        if (!initialized || (pressed & (NpadButton.Plus | NpadButton.Minus)) != 0)
        {
            ShowControllerSupport();
            initialized = true;
        }
    }

    void ShowControllerSupport()
    {
        ResetAllAssignments();

        controllerSupportArg.SetDefault();
        controllerSupportArg.playerCountMax = (byte)(npadIds.Length - 1);

        controllerSupportArg.enableExplainText = true;

        ControllerSupport.SetExplainText(ref controllerSupportArg, "Player 1", NpadId.No1);
        ControllerSupport.SetExplainText(ref controllerSupportArg, "Player 2", NpadId.No2);
        ControllerSupport.SetExplainText(ref controllerSupportArg, "Player 3", NpadId.No3);
        ControllerSupport.SetExplainText(ref controllerSupportArg, "Player 4", NpadId.No4);

        result = ControllerSupport.Show(controllerSupportArg);

        if (!result.IsSuccess())
        {
            Debug.Log("ControllerSupport failed: " + result);
        }
        enabled = false; 
    }
#endif
}