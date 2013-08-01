using System;
using Microsoft.SPOT;
using System.Net;
using GT = Gadgeteer;
using Gadgeteer.Modules.GHIElectronics;
using Gadgeteer.Modules.Seeed;

// These directives support the EthernetConnection class workaround: http://wp.me/p1TEdE-bj .
using Microsoft.SPOT.Net.NetworkInformation; // Add this using directive.


namespace GadgeteerApp3
{
    public partial class Program
    {
        string name;
        string pulseRate;
        string oxSat;
        GT.Timer timer;

        uint xIndex = 5;

       
        static bool IsNetworkUp = false;
        static public bool ethernet_last_status = false;

        void ProgramStarted()
        {
            pulseOximeter.ProbeAttached +=
            new PulseOximeter.ProbeAttachedHandler(pulseOximeter_ProbeAttached);
            pulseOximeter.ProbeDetached +=
            new PulseOximeter.ProbeDetachedHandler(pulseOximeter_ProbeDetached);
            button.ButtonPressed +=
                new Button.ButtonEventHandler(button_ButtonPressed);

            timer = new GT.Timer(10000);
            timer.Tick += new GT.Timer.TickEventHandler(timer_Tick);

           

            name = "TestName-4";
            Debug.Print("Using name: " + name);
            Debug.Print("Program Started");

            display_T35.SimpleGraphics.DisplayText("Name: " + name,
                Resources.GetFont(Resources.FontResources.NinaB), GT.Color.Red, 50, 60);
            display_T35.SimpleGraphics.DisplayText("Network up? " + IsNetworkUp,
            Resources.GetFont(Resources.FontResources.NinaB), GT.Color.Red, 50, 75);
        }

        void button_ButtonPressed(Button sender, Button.ButtonState state)
        {
            if (!button.IsLedOn && pulseOximeter.IsProbeAttached)
            {
                pulseOximeter.Heartbeat +=
                    new PulseOximeter.HeartbeatHandler(pulseOximeter_Heartbeat);
                button.TurnLEDOn();
                timer.Start();
            }
            else
            {
                pulseOximeter.Heartbeat -=
                    new PulseOximeter.HeartbeatHandler(pulseOximeter_Heartbeat);
                button.TurnLEDOff();
                timer.Stop();
            }
        }

        void timer_Tick(GT.Timer timer)
        {
           
                display_T35.SimpleGraphics.DisplayRectangle(GT.Color.Black, 2, GT.Color.Black, 50, 75, 150, 25);
                display_T35.SimpleGraphics.DisplayText("Network up? " + IsNetworkUp,
                Resources.GetFont(Resources.FontResources.NinaB), GT.Color.Red, 50, 75);

                pulseOximeter.Heartbeat -=
                        new PulseOximeter.HeartbeatHandler(pulseOximeter_Heartbeat);
                button.TurnLEDOff();
                timer.Stop();
            }
        

        void pulseOximeter_Heartbeat(PulseOximeter sender, PulseOximeter.Reading reading)
        {
            pulseRate = reading.PulseRate.ToString();
            oxSat = reading.SPO2.ToString();

            display_T35.SimpleGraphics.DisplayRectangle(GT.Color.Blue, 2, GT.Color.Black, 5, 5, 150, 25);
            display_T35.SimpleGraphics.DisplayRectangle(GT.Color.Blue, 2, GT.Color.Black, 5, 30, 150, 25);

            display_T35.SimpleGraphics.DisplayTextInRectangle("Oxygen Saturation: " + oxSat, 10, 10, 150, 50,
                                 GT.Color.Orange, Resources.GetFont(Resources.FontResources.small));
            display_T35.SimpleGraphics.DisplayTextInRectangle("Pulse: " + pulseRate, 10, 35, 150, 50,
                                 GT.Color.Green, Resources.GetFont(Resources.FontResources.small));

            uint graph_x = xIndex++;

            if (graph_x + 5 > display_T35.Width)
            {
                xIndex = 5;
                display_T35.SimpleGraphics.Clear();
            }

            uint graph_y = (uint)reading.PulseRate;     // Scale the voltage to size of display.
            display_T35.SimpleGraphics.SetPixel(GT.Color.Green, graph_x, display_T35.SimpleGraphics.Height - graph_y);
            graph_y = (uint)reading.SPO2;
            display_T35.SimpleGraphics.SetPixel(GT.Color.Orange, graph_x, display_T35.SimpleGraphics.Height - graph_y);
        }

        void pulseOximeter_ProbeAttached(PulseOximeter sender)
        {
            display_T35.SimpleGraphics.DisplayRectangle(GT.Color.Black, 2, GT.Color.Black, 25, 75, 150, 25);
            display_T35.SimpleGraphics.DisplayText("Probe attached.",
                Resources.GetFont(Resources.FontResources.NinaB), GT.Color.Green, 25, 75);

            display_T35.SimpleGraphics.DisplayRectangle(GT.Color.Black, 2, GT.Color.Black, 25, 90, 150, 25);
            display_T35.SimpleGraphics.DisplayText("Network up? " + IsNetworkUp,
                Resources.GetFont(Resources.FontResources.NinaB), IsNetworkUp ? GT.Color.Green : GT.Color.Red, 25, 90);
        }

        void pulseOximeter_ProbeDetached(PulseOximeter sender)
        {
            display_T35.SimpleGraphics.DisplayRectangle(GT.Color.Black, 2, GT.Color.Black, 25, 75, 150, 25);
            display_T35.SimpleGraphics.DisplayText("Probe detached.",
                Resources.GetFont(Resources.FontResources.NinaB), GT.Color.Red, 25, 75);

            display_T35.SimpleGraphics.DisplayRectangle(GT.Color.Black, 2, GT.Color.Black, 25, 90, 150, 25);
            display_T35.SimpleGraphics.DisplayText("Network up? " + IsNetworkUp,
            Resources.GetFont(Resources.FontResources.NinaB), IsNetworkUp ? GT.Color.Green : GT.Color.Red, 25, 90);

            timer.Stop();
            button.TurnLEDOff();
            pulseOximeter.Heartbeat -= new PulseOximeter.HeartbeatHandler(pulseOximeter_Heartbeat);
        }
    }
}