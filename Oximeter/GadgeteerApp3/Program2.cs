using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;

namespace GadgeteerApp3
{
    public partial class Program
    {
        // This method is run when the mainboard is powered up or reset.   
      void ProgramStarted()
        {
            pulseOximeter.ProbeAttached += new Gadgeteer.Modules.Seeed.PulseOximeter.ProbeAttachedHandler(pulseOximeter_ProbeAttached);
            pulseOximeter.ProbeDetached +=                new Gadgeteer.Modules.Seeed.PulseOximeter.ProbeDetachedHandler(pulseOximeter_ProbeDetached);
            button.ButtonPressed +=                new Gadgeteer.Modules.GHIElectronics.Button.ButtonEventHandler(button_ButtonPressed);
 
            Debug.Print("Program Started");
        }
 
        void pulseOximeter_ProbeDetached(Gadgeteer.Modules.Seeed.PulseOximeter sender)
        {
            Debug.Print(pulseOximeter.IsProbeAttached.ToString());
        }
 
        void button_ButtonPressed(Gadgeteer.Modules.GHIElectronics.Button sender, Gadgeteer.Modules.GHIElectronics.Button.ButtonState state)
        {
            if (!button.IsLedOn && pulseOximeter.IsProbeAttached)
            {
                pulseOximeter.Heartbeat +=
                    new Gadgeteer.Modules.Seeed.PulseOximeter.HeartbeatHandler(pulseOximeter_Heartbeat);
                button.TurnLEDOn();
            }
            else
            {
                pulseOximeter.Heartbeat -=
                    new Gadgeteer.Modules.Seeed.PulseOximeter.HeartbeatHandler(pulseOximeter_Heartbeat);
                button.TurnLEDOff();
            }
        }
 
        void pulseOximeter_Heartbeat(Gadgeteer.Modules.Seeed.PulseOximeter sender, Gadgeteer.Modules.Seeed.PulseOximeter.Reading reading)
        {
            Debug.Print("PulseRate: " + reading.PulseRate);
            Debug.Print("Oxygen Saturation: " + reading.SPO2);
        }
 
        void pulseOximeter_ProbeAttached(Gadgeteer.Modules.Seeed.PulseOximeter sender)
        {
            Debug.Print(pulseOximeter.IsProbeAttached.ToString());
        }
}
}
