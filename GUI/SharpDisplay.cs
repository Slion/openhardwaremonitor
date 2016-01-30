/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows;
using OpenHardwareMonitor.Hardware;
using OpenHardwareMonitor.Utilities;
using System.Runtime.InteropServices;
//SL: That's not needed anymore I guess.
//Could we stay on .NET 2.0 then?
//using System.ServiceModel;
using SharpLib.Display;


namespace OpenHardwareMonitor.GUI
{
    public class SharpDisplay : IDisposable
    {
        private IComputer computer;
        private PersistentSettings settings;
        private UnitManager unitManager;
        private List<SensorSharpDisplay> iSensors = new List<SensorSharpDisplay>();
        private Client iClient;
        TextField iTextFieldTop;
        TextField iTextFieldBottom;
        TextField iTextFieldTopRight;
        TextField iTextFieldBottomRight;

        DataField[] iTextFields;

        private int iNextSensorToDisplay = 0;
        private int iTickCounter = 0;
        bool iPacked = false;

        public SharpDisplay(IComputer computer, PersistentSettings settings, UnitManager unitManager)
        {
            this.computer = computer;
            this.settings = settings;
            this.unitManager = unitManager;
            computer.HardwareAdded += new HardwareEventHandler(HardwareAdded);
            computer.HardwareRemoved += new HardwareEventHandler(HardwareRemoved);

            //Connect our client
            //Instance context is then managed by our client class
            iClient = new Client();
            iClient.CloseOrderEvent += OnCloseOrder;
            //
            iTextFieldTop = new TextField("", ContentAlignment.MiddleLeft, 0, 0);
            iTextFieldBottom = new TextField("", ContentAlignment.MiddleLeft, 0, 1);
            iTextFieldTopRight = new TextField("", ContentAlignment.MiddleRight, 1, 0);
            iTextFieldBottomRight = new TextField("", ContentAlignment.MiddleRight, 1, 1);
            //
            iClient.Open();
            iClient.SetName("Open Hardware Monitor");
            iClient.SetPriority(Priorities.SystemMonitor);

            CreateFields();
        }

        public void OnCloseOrder()
        {
            iClient.Close();
        }


        private void HardwareRemoved(IHardware hardware)
        {
            hardware.SensorAdded -= new SensorEventHandler(SensorAdded);
            hardware.SensorRemoved -= new SensorEventHandler(SensorRemoved);
            foreach (ISensor sensor in hardware.Sensors)
                SensorRemoved(sensor);
            foreach (IHardware subHardware in hardware.SubHardware)
                HardwareRemoved(subHardware);
        }

        private void HardwareAdded(IHardware hardware)
        {
            foreach (ISensor sensor in hardware.Sensors)
                SensorAdded(sensor);
            hardware.SensorAdded += new SensorEventHandler(SensorAdded);
            hardware.SensorRemoved += new SensorEventHandler(SensorRemoved);
            foreach (IHardware subHardware in hardware.SubHardware)
                HardwareAdded(subHardware);
        }

        private void SensorAdded(ISensor sensor)
        {
            if (settings.GetValue(new Identifier(sensor.Identifier,
              "SharpDisplay").ToString(), false))
                Add(sensor, false);
        }

        private void SensorRemoved(ISensor sensor)
        {
            if (Contains(sensor))
                Remove(sensor, false);
        }

        public void Dispose()
        {
            foreach (SensorSharpDisplay icon in iSensors)
                icon.Dispose();

            Quit();
            //iServer.Stop();
            iClient.Close();

        }

        private void CreateFields()
        {
            if (iPacked)
            {
                //We just switched to packed mode                    
                //Make sure our layout is proper
                TableLayout layout = new TableLayout(2, 2);
                iClient.SetLayout(layout);
                iTextFields = new DataField[] { iTextFieldTop, iTextFieldBottom, iTextFieldTopRight, iTextFieldBottomRight };
                iClient.CreateFields(iTextFields);
            }
            else
            {
                //Non packed mode
                TableLayout layout = new TableLayout(1, 2);
                iClient.SetLayout(layout);
                iTextFields = new DataField[] { iTextFieldTop, iTextFieldBottom };
                iClient.CreateFields(iTextFields);
            }
        }

        public void Redraw(bool aPacked, bool aDisplayTime)
        {
            const int KNumberOfTickBeforeSwitch = 4;
            const int KMaxCharacterPerLine = 16;
            int count = 0;

            //string time = DateTime.Now.ToShortTimeString();
            string time = DateTime.Now.ToLongTimeString();

            if (iSensors.Count > 0)
            {
                if (iPacked != aPacked)
                {
                    //Remember mode
                    iPacked = aPacked;

                    CreateFields();

                }
            }


            //Update all sensors from our front view
            foreach (SensorSharpDisplay sensor in iSensors)
            {
                count++;
                sensor.Update();

                if (aDisplayTime && count == 1)
                {
                    //First slot is taken by time display
                    count++;
                    iTextFieldTop.Text = time;
                    iClient.SetField(iTextFieldTop);
                }

                if (aPacked)
                {
                    //Build strings for packed mode
                    string packedText = "";
                    packedText = sensor.iFirstLine.Substring(0, 3) + ":" + sensor.iSecondLine;
                    if (count == 1)
                    {
                        iTextFieldTop.Text = packedText;
                        iClient.SetField(iTextFieldTop);
                    }
                    else if (count == 2)
                    {
                        iTextFieldBottom.Text = packedText;
                        iClient.SetField(iTextFieldBottom);
                    }
                    else if (count == 3)
                    {
                        iTextFieldTopRight.Text = packedText;
                        iClient.SetField(iTextFieldTopRight);
                    }
                    else if (count == 4)
                    {
                        iTextFieldBottomRight.Text = packedText;
                        iClient.SetField(iTextFieldBottomRight);
                    }
                }
            }

            //Alternate between sensors 
            if (iSensors.Count > 0)
            {
                if (aPacked)
                {
                    //Review that stuff cause as it is it's probably useless
                    //string packedLine = "";
                    iTickCounter++;
                    if (iTickCounter == KNumberOfTickBeforeSwitch) //Move to the next sensor only every so many tick
                    {
                        iTickCounter = 0;
                        if (iNextSensorToDisplay == 1)
                        {
                            iNextSensorToDisplay = 0;
                        }
                        else
                        {
                            iNextSensorToDisplay = 1;
                        }
                    }
                }
                else
                {
                    string secondLine = iSensors[iNextSensorToDisplay].iSecondLine;
                    if (aDisplayTime)
                    {
                        //Add enough spaces
                        while (secondLine.Length + time.Length < KMaxCharacterPerLine)
                        {
                            secondLine += " ";
                        }
                        secondLine += time;
                    }
                    //Display current sensor on our FrontView display
                    SetText(iSensors[iNextSensorToDisplay].iFirstLine, secondLine);
                    iTickCounter++;
                    if (iTickCounter == KNumberOfTickBeforeSwitch) //Move to the next sensor only every so many tick
                    {
                        iTickCounter = 0;
                        iNextSensorToDisplay++;
                    }
                }
            }

            if (iNextSensorToDisplay == iSensors.Count)
            {
                //Go back to first sensor
                iNextSensorToDisplay = 0;
            }


        }

        public bool Contains(ISensor sensor)
        {
            foreach (SensorSharpDisplay icon in iSensors)
                if (icon.Sensor == sensor)
                    return true;
            return false;
        }

        public void Add(ISensor sensor, bool balloonTip)
        {
            if (Contains(sensor))
            {
                return;
            }
            else
            {
                //SL:
                iSensors.Add(new SensorSharpDisplay(this, sensor, balloonTip, settings, unitManager));
                //UpdateMainIconVisibilty();
                settings.SetValue(new Identifier(sensor.Identifier, "SharpDisplay").ToString(), true);
                iNextSensorToDisplay = 0;
                if (iSensors.Count == 1)
                {
                    //Just added first sensor in FrontView, unable FrontView plug-in mode
                    Init();
                }
            }

        }

        public void Remove(ISensor sensor)
        {
            Remove(sensor, true);
            iNextSensorToDisplay = 0;
            if (iSensors.Count == 0)
            {
                //No sensor to display in FrontView, just disable FrontView plug-in mode
                Uninit();
            }

        }

        private void Remove(ISensor sensor, bool deleteConfig)
        {
            if (deleteConfig)
            {
                settings.Remove(
                  new Identifier(sensor.Identifier, "SharpDisplay").ToString());
            }
            SensorSharpDisplay instance = null;
            foreach (SensorSharpDisplay icon in iSensors)
                if (icon.Sensor == sensor)
                    instance = icon;
            if (instance != null)
            {
                iSensors.Remove(instance);
                //UpdateMainIconVisibilty();
                instance.Dispose();
            }
        }



        private void UpdateMainIconVisibilty()
        {
            /*
            if (mainIconEnabled)
            {
                mainIcon.Visible = list.Count == 0;
            }
            else
            {
                mainIcon.Visible = false;
            }
             */
        }

        public void Init()
        {
            //iServer.SendMessage("init:");
        }

        public void Uninit()
        {
            //iServer.SendMessage("uninit:");
        }

        public void SetText(string aUpperLine, string aLowerLine)
        {
            //iServer.SendMessage("set-vfd-text:" + aUpperLine + "\n" + aLowerLine);
            iTextFieldTop.Text = aUpperLine;
            iTextFieldBottom.Text = aLowerLine;
            iClient.SetFields(iTextFields);
        }

        public void Quit()
        {
            //iServer.SendMessage("quit:");
        }

        /*
        public bool IsMainIconEnabled
        {
            get { return mainIconEnabled; }
            set
            {
                if (mainIconEnabled != value)
                {
                    mainIconEnabled = value;
                    UpdateMainIconVisibilty();
                }
            }
        }*/


    }
}
