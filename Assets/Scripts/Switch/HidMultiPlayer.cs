#if UNITY_SWITCH
using UnityEngine;
using nn.hid;
using UnityEngine.Switch;

public class HidMultiPlayer : MonoBehaviour
{

    //    private UnityEngine.UI.Text textComponent;
    //    private System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();

    //#if true
    //    // 4 players
    //    private NpadId[] npadIds = { NpadId.Handheld, NpadId.No1, NpadId.No2, NpadId.No3, NpadId.No4 };
    //#else
    //    // 2 players
    //    private NpadId[] npadIds = { NpadId.Handheld, NpadId.No1, NpadId.No2 };
    //#endif

    //    private NpadState npadState = new NpadState();
    //    private long[] preButtons;
    //    private ControllerSupportArg controllerSupportArg = new ControllerSupportArg();
    //    private nn.Result result = new nn.Result();

    //    void Start()
    //    {
    //        textComponent = GameObject.Find("/Canvas/Text").GetComponent<UnityEngine.UI.Text>();

    //        Npad.Initialize();
    //        Npad.SetSupportedIdType(npadIds);
    //        NpadJoy.SetHoldType(NpadJoyHoldType.Horizontal);

    //#if true
    //        // Horizontal single Joy-Con
    //        Npad.SetSupportedStyleSet(NpadStyle.FullKey | NpadStyle.Handheld | NpadStyle.JoyDual |
    //            NpadStyle.JoyLeft | NpadStyle.JoyRight);

    //        for (int i = 1; i < npadIds.Length; i++)
    //        {
    //            NpadJoy.SetAssignmentModeSingle(npadIds[i]);
    //        }
    //#else
    //        // Dual Joy-Con
    //        Npad.SetSupportedStyleSet(NpadStyle.FullKey | NpadStyle.Handheld | NpadStyle.JoyDual);
    //#endif

    //        preButtons = new long[npadIds.Length];
    //    }

    //    void Update()
    //    {
    //        stringBuilder.Length = 0;
    //        stringBuilder.Append(" +, -: Controller Support Applet\n");

    //        NpadButton onButtons = 0;

    //        for (int i = 0; i < npadIds.Length; i++)
    //        {
    //            NpadId npadId = npadIds[i];
    //            NpadStyle npadStyle = Npad.GetStyleSet(npadId);
    //            if (npadStyle == NpadStyle.None) { continue; }

    //            Npad.GetState(ref npadState, npadId, npadStyle);

    //            bool isPushedEnterButton = false;
    //            if (npadStyle == NpadStyle.JoyLeft)
    //            {
    //                isPushedEnterButton = (npadState.buttons & NpadButton.Down) != 0;
    //            }
    //            else if (npadStyle == NpadStyle.JoyRight)
    //            {
    //                isPushedEnterButton = (npadState.buttons & NpadButton.X) != 0;
    //            }
    //            else if ((npadStyle == NpadStyle.FullKey) || (npadStyle == NpadStyle.JoyDual) || (npadStyle == NpadStyle.Handheld))
    //            {
    //                isPushedEnterButton = (npadState.buttons & NpadButton.A) != 0;
    //            }

    //            if (isPushedEnterButton)
    //            {
    //                stringBuilder.AppendFormat("<color=#ff8888ff>{0} {1} {2}</color>\n", npadId, npadStyle, npadState);
    //            }
    //            else
    //            {
    //                stringBuilder.AppendFormat("{0} {1} {2}\n", npadId, npadStyle, npadState);
    //            }

    //            onButtons |= ((NpadButton)preButtons[i] ^ npadState.buttons) & npadState.buttons;
    //            preButtons[i] = (long)npadState.buttons;
    //        }

    //        if ((onButtons & (NpadButton.Plus | NpadButton.Minus)) != 0)
    //        {
    //            ShowControllerSupport();
    //        }
    //        stringBuilder.AppendFormat("Result: {0}\n", result);

    //        textComponent.text = stringBuilder.ToString();
    //    }

    //    void ShowControllerSupport()
    //    {
    //        controllerSupportArg.SetDefault();
    //        controllerSupportArg.playerCountMax = (byte)(npadIds.Length - 1);

    //        controllerSupportArg.enableIdentificationColor = true;
    //        nn.util.Color4u8 color = new nn.util.Color4u8();
    //        color.Set(255, 128, 128, 255);
    //        controllerSupportArg.identificationColor[0] = color;
    //        color.Set(128, 128, 255, 255);
    //        controllerSupportArg.identificationColor[1] = color;
    //        color.Set(128, 255, 128, 255);
    //        controllerSupportArg.identificationColor[2] = color;
    //        color.Set(224, 224, 128, 255);
    //        controllerSupportArg.identificationColor[3] = color;

    //        controllerSupportArg.enableExplainText = true;
    //        ControllerSupport.SetExplainText(ref controllerSupportArg, "Red", NpadId.No1);
    //        ControllerSupport.SetExplainText(ref controllerSupportArg, "Blue", NpadId.No2);
    //        ControllerSupport.SetExplainText(ref controllerSupportArg, "Green", NpadId.No3);
    //        ControllerSupport.SetExplainText(ref controllerSupportArg, "Yellow", NpadId.No4);

    //        Debug.Log(controllerSupportArg);
    //        result = ControllerSupport.Show(controllerSupportArg);
    //        if (!result.IsSuccess()) { Debug.Log(result); }
    //    }
}
#endif