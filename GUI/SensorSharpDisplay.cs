/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenHardwareMonitor.Hardware;
using OpenHardwareMonitor.Utilities;

namespace OpenHardwareMonitor.GUI
{
    public class SensorSharpDisplay : IDisposable
    {

        private UnitManager unitManager;

        private ISensor sensor;
        private Color color;
        private Color darkColor;
        private Font font;
        private Font smallFont;
        public string iFirstLine;
        public string iSecondLine;


        public SensorSharpDisplay(SharpDisplay soundGraphDisplay, ISensor sensor,
          bool balloonTip, PersistentSettings settings, UnitManager unitManager)
        {
            this.unitManager = unitManager;
            this.sensor = sensor;

            // get the default dpi to create an icon with the correct size
            float dpiX, dpiY;
            using (Bitmap b = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
            {
                dpiX = b.HorizontalResolution;
                dpiY = b.VerticalResolution;
            }

            // adjust the size of the icon to current dpi (default is 16x16 at 96 dpi) 
            int width = (int)Math.Round(16 * dpiX / 96);
            int height = (int)Math.Round(16 * dpiY / 96);

            // make sure it does never get smaller than 16x16
            width = width < 16 ? 16 : width;
            height = height < 16 ? 16 : height;

            // adjust the font size to the icon size
            FontFamily family = SystemFonts.MessageBoxFont.FontFamily;
            float baseSize;
            switch (family.Name)
            {
                case "Segoe UI": baseSize = 12; break;
                case "Tahoma": baseSize = 11; break;
                default: baseSize = 12; break;
            }

            this.font = new Font(family,
              baseSize * width / 16.0f, GraphicsUnit.Pixel);
            this.smallFont = new Font(family,
              0.75f * baseSize * width / 16.0f, GraphicsUnit.Pixel);

        }

        public ISensor Sensor
        {
            get { return sensor; }
        }

        public Color Color
        {
            get { return color; }
            set
            {
                this.color = value;
                this.darkColor = Color.FromArgb(255,
                  this.color.R / 3,
                  this.color.G / 3,
                  this.color.B / 3);
            }
        }

        public void Dispose()
        {
            font.Dispose();
            smallFont.Dispose();
        }

        public string GetString()
        {
            if (!sensor.Value.HasValue)
                return "-";

            switch (sensor.SensorType)
            {
                case SensorType.Voltage:
                    return string.Format("{0:F1}", sensor.Value);
                case SensorType.Clock:
                    return string.Format("{0:F1}", 1e-3f * sensor.Value);
                case SensorType.Load:
                    return string.Format("{0:F0}", sensor.Value);
                case SensorType.Temperature:
                    if (unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit)
                        return string.Format("{0:F0}",
                          UnitManager.CelsiusToFahrenheit(sensor.Value));
                    else
                        return string.Format("{0:F0}", sensor.Value);
                case SensorType.Fan:
                    return string.Format("{0:F1}", 1e-3f * sensor.Value);
                case SensorType.Flow:
                    return string.Format("{0:F1}", 1e-3f * sensor.Value);
                case SensorType.Control:
                    return string.Format("{0:F0}", sensor.Value);
                case SensorType.Level:
                    return string.Format("{0:F0}", sensor.Value);
                case SensorType.Power:
                    return string.Format("{0:F0}", sensor.Value);
                case SensorType.Data:
                    return string.Format("{0:F0}", sensor.Value);
                case SensorType.Factor:
                    return string.Format("{0:F1}", sensor.Value);
            }
            return "-";
        }


        public void Update()
        {


            switch (sensor.SensorType)
            {
                case SensorType.Load:
                case SensorType.Control:
                case SensorType.Level:
                    //notifyIcon.Icon = CreatePercentageIcon();
                    break;
                default:
                    //notifyIcon.Icon = CreateTransparentIcon();
                    break;
            }


            string format = "";
            switch (sensor.SensorType)
            {
                case SensorType.Voltage: format = "{0:F2}V"; break;
                case SensorType.Clock: format = "{0:F0}MHz"; break;
                case SensorType.Load: format = "{0:F0}%"; break;
                //iMON VFD escape sequence for Celsius
                case SensorType.Temperature: format = "{0:F0}°C"; break;
                case SensorType.Fan: format = "{0:F0}R"; break; //RPM
                case SensorType.Flow: format = "{0:F0}L/h"; break;
                case SensorType.Control: format = "{0:F0}%"; break;
                case SensorType.Level: format = "{0:F0}%"; break;
                case SensorType.Power: format = "{0:F0}W"; break;
                case SensorType.Data: format = "{0:F0}GB"; break;
                case SensorType.Factor: format = "{0:F3}GB"; break;
            }
            string formattedValue = string.Format(format, sensor.Value);

            if (sensor.SensorType == SensorType.Temperature &&
              unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit)
            {
                //iMON VFD escape sequence for Fahrenheit
                format = "{0:F0}°F";
                formattedValue = string.Format(format, UnitManager.CelsiusToFahrenheit(sensor.Value));
            }

            //iFirstLine = sensor.Hardware.Name;
            //iSecondLine = sensor.Name+ ":" + formattedValue;

            iFirstLine = sensor.Name;
            iSecondLine = formattedValue;


        }
    }
}
