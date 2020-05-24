using rpi_ws281x;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ws281x.Visualizer.Helpers
{
    public static class LedHelper
    {
        public static StripType? GetStripType(this string[] args)
        {
            var typeName = args.GetString("--strip");
            return GetStripTypeFromName(typeName);
        }

        public static StripType? GetStripTypeFromName(string typeName)
        {
            if (!string.IsNullOrEmpty(typeName))
            {
                typeName = typeName.ToUpper().Replace("_STRIP", "");
                var types = Enum.GetValues(typeof(StripType));
                foreach (StripType strip in types)
                {
                    var name = strip.ToString().Replace("_STRIP", "");
                    if (name == typeName)
                    {
                        return strip;
                    }
                }
            }

            return null;
        }

        public static Pin? GetPin(this string[] args)
        {
            var pinNumber = args.GetInt("--pin");
            if (pinNumber == null)
            {
                return null;
            }

            return GetPinFromNumber(pinNumber.Value);
        }

        public static Pin? GetPinFromNumber(int pinNumber)
        {
            foreach (Pin pin in Enum.GetValues(typeof(Pin)))
            {
                if (pinNumber == (int)pin)
                {
                    return pin;
                }
            }

            return null;
        }

        public static ControllerType? GetControllerType(this string[] args, ControllerType? defaultType = ControllerType.PWM0)
        {
            var controller = args.GetString("--controller");
            if (string.IsNullOrEmpty(controller))
            {
                return defaultType;
            }
            else
            {
                return GetControllerTypeFromName(controller);
            }
        }

        public static ControllerType? GetControllerTypeFromName(string controllerName)
        {
            if (!string.IsNullOrEmpty(controllerName))
            {
                controllerName = controllerName.ToUpper();
                var types = Enum.GetValues(typeof(ControllerType));
                foreach (ControllerType type in types)
                {
                    var name = type.ToString();
                    if (name == controllerName)
                    {
                        return type;
                    }
                }
            }

            return null;
        }
    }
}
