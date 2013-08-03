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
using Gadgeteer.Modules.GHIElectronics;
using Gadgeteer.Modules.Seeed;
using Microsoft.WindowsAzure.MobileServices;

namespace GPRSTest
{
    public partial class Program
    {

        //TODO: add your mobile service URI and app key below from the Windows Azure Portal https://manage.windowsazure.com 
        public static MobileServiceClient MobileService = new MobileServiceClient(
            new Uri("https://testgadgeteerguada.azure-mobile.net/"),
           "qgOcvvKDvYKqfHSNoMWqFhPLERiPel73"
        );

        private bool _gprsNetworkRegistered;
        private bool _gsmIsAttached;
        private bool _gprsIsAttached;
        private bool _imeiIsRetrieved;
        private bool _cellRadioIsReady;
        private bool _gprsConnectionRequestSent;
   

        /// </summary>
        public string IMEI { get; private set; }
        /// <summary>
        /// Print Debug messages to Output
        /// </summary>
        public bool DebugPrintEnabled { get; set; }
        /// <summary>
        /// The GPRS Access Point Name provided by the SIM card provider
        /// </summary>
        public string AccessPointName { get; set; }
        /// <summary>
        /// The GPRS Username provided by the SIM card provider (default empty)
        /// </summary>
        public string AccessPointUser { get; set; }
        /// <summary>
        /// The GPRS Password provided by the SIM card provider (default empty)
        /// </summary>
        public string AccessPointPassword { get; set; }

        private string _ipAddress = string.Empty;
           private Thread _thread;

     

            void ProgramStarted()
            {
                
                InitializeGprsNetwork();
               // cellularRadio.PowerOn();   
                button.ButtonPressed += new Button.ButtonEventHandler(button_ButtonPressed);
                Debug.Print("Program Started");
            }

            private void InitializeGprsNetwork()
            {
                Debug.Print("[" + DateTime.Now + ":" + DateTime.Now.Millisecond + "] Initializing Gprs network...");
                _thread = new Thread(RadioManagerThreadRun);
                _thread.Start();
                AccessPointName = "simple";
                    AccessPointUser = "";
                    AccessPointPassword = "";
                    DebugPrintEnabled = true;
                   // IsHttpModeEnabled = true
               
            }

            void button_ButtonPressed(Button sender, Button.ButtonState state)
            {
                if (button.IsLedOn)
                {
                    cellularRadio.SendATCommand("H"); // Hang up voice call.
                    cellularRadio.SendSms( "13059045624", "Por wassap :) avisame si te llega!!! ");
                    
                    
                    //954 927 6317 

                    //try
                    //{
                    //    //insert into the mobile service
                    //    EntidadAInsertar entidad = new EntidadAInsertar { complete = false, text = "testttt" };
                    //    var json = MobileService.GetTable("TodoItem").Insert(entidad);

                    //}
                    //catch (Exception ex)
                    //{
                    //    Debug.Print(ex.ToString());
                    //}

                    button.TurnLEDOff();
                }
                else
                {
                    // Dial voice call; Use phone number as string without hyphens.
                   cellularRadio.SendATCommand("D2065780941;");
                    button.TurnLEDOn();
                }
            }



    
        private void RadioManagerThreadRun()
        {
            cellularRadio.ImeiRetrieved += ImeiRetrieved;
            cellularRadio.GprsAttached += GprsAttached;
            cellularRadio.GsmNetworkRegistrationChanged += GsmNetworkRegistrationChanged;
            cellularRadio.GprsNetworkRegistrationChanged += GprsNetworkRegistrationChanged;
            //_radio.TcpDataReceived += TcpDataReceived;

            cellularRadio.PowerOn(40);
            //RadioState = RadioState.StartingUp;

            while (true)
            {
                if (!_cellRadioIsReady)
                {
                    _cellRadioIsReady = cellularRadio.RetrievePhoneActivity() == Gadgeteer.Modules.Seeed.CellularRadio.ReturnedState.OK;
                    //RadioState = RadioState.StartingUp;
                }

                else if (!_gsmIsAttached)
                {
                    cellularRadio.SendATCommand("AT+CREG?");
                    //RadioState = RadioState.WaitingForNetworkRegistration;
                    Thread.Sleep(5000);
                }

                else if (!_gprsNetworkRegistered)
                {
                    cellularRadio.SendATCommand("AT+CCREG?");
                    //RadioState = RadioState.WaitingForGprsConnection;
                    Thread.Sleep(5000);
                }

                else if (!_imeiIsRetrieved)
                {
                    //RadioState = RadioState.WaitingForIMEI;
                    cellularRadio.RetrieveImei();
                    Thread.Sleep(5000);
                }

                else if (!_gprsIsAttached)
                {
                    if (!_gprsConnectionRequestSent)
                    {
                        bool result;
                        result = cellularRadio.SendATCommand("AT+CIPMODE=1") == CellularRadio.ReturnedState.OK;
                        if (!result) return;
                        Thread.Sleep(2000);
                        result = cellularRadio.AttachGPRS(AccessPointName, AccessPointUser, AccessPointPassword) == CellularRadio.ReturnedState.OK;
                        if (!result) return;
                        Thread.Sleep(10000);
                        // check IP
                        if (_ipAddress.Length < 6) return;
                        _gprsConnectionRequestSent = true;
                    }
                }
                else
                {
                    //RadioState = RadioState.Ready;
                }

                Thread.Sleep(2000);

               // Debug.Print("RadioState: " + this.RadioState);
            }
        }

 private void GprsNetworkRegistrationChanged(CellularRadio sender, CellularRadio.NetworkRegistrationState networkState)
        {
            if (networkState == CellularRadio.NetworkRegistrationState.Registered ||
                networkState == CellularRadio.NetworkRegistrationState.Roaming)
            {
                _gprsNetworkRegistered = true;
                Debug.Print("GPRS Network Registered");
            }
            else
            {
                _gprsNetworkRegistered = false;
                Debug.Print("GPRS Network Registration Error");
            }
        }
 private void GsmNetworkRegistrationChanged(CellularRadio sender, CellularRadio.NetworkRegistrationState networkState)
        {
            if (networkState == CellularRadio.NetworkRegistrationState.Registered ||
                networkState == CellularRadio.NetworkRegistrationState.Roaming)
            {
                Debug.Print("GSM Network Registered");

                _gsmIsAttached = true;
            }
            else
            {
                _gsmIsAttached = false;
                Debug.Print("GSM Network Registration Error");
            }
        }
 private void GprsAttached(CellularRadio sender, string ipAddress)
        {
            _gprsIsAttached = true;
            _ipAddress = ipAddress;
            Debug.Print("IP: " + ipAddress);
        }

        private void ImeiRetrieved(CellularRadio sender, string imei)
        {
            IMEI = imei;
            _imeiIsRetrieved = true;
            Debug.Print("IMEI: " + IMEI);
        }
   

    #region "Servicios"
    private void SendDataToAtionet(string trama, ApiTypeEnum apiType)
        {
            var content = POSTContent.CreateTextBasedContent(trama);
            var url = string.Empty;

            switch (apiType)
            {
                case ApiTypeEnum.Auth:
                    url = this.boardConfig.ApiUrl + "auth/";
                    break;

                case ApiTypeEnum.Maintenance:
                    url = this.boardConfig.ApiUrl + "maintenance/";
                    break;

                case ApiTypeEnum.Legacy:
                    url = this.boardConfig.ApiUrl + "legacy/";
                    break;
            }

            this.webRequest = HttpHelper.CreateHttpPostRequest(url, content, null);
            this.webRequest.AddHeaderField("Authorization", "Basic" + this.boardConfig.ApiUser + ":" + this.boardConfig.ApiPassword);

            this.webRequest.ResponseReceived += this.WebRequestResponseReceived;

            this.log.Print("[" + DateTime.Now + ":" + DateTime.Now.Millisecond + "] [Program] - Sending to Ationet: *" + trama + "*");
            this.log.Print("[" + DateTime.Now + ":" + DateTime.Now.Millisecond + "] [Program] - Ationet Url: " + url);

            this.webRequest.SendRequest();

            LedsSequence.TcpTransmision.Flash(this.buttons).Start();
        }


     private bool SendDataToServer(string post)
        {
            if (this.cellularRadio.ConnectTCP("168.62.20.37", 80))
            {
                this.TcpDisconnect();
                Thread.Sleep(2000);
                return false;
            }

            if (!this.gprsManager.SendData(post))
            {
                this.TcpDisconnect();
                Thread.Sleep(2000);
                return false;
            }

            // wait to flush
            string data;
            if (!this.gprsManager.ReceiveData(10000, out data))
            {
                this.TcpDisconnect();
                Thread.Sleep(2000);
                return false;
            }

            if (data.IndexOf(@"HTTP/1.1 201 Created") <= -1)
            {
                // unsuccessful server transaction
                this.TcpDisconnect();
                Thread.Sleep(2000);
                return false;
            }

            // successful server transaction
            this.TcpDisconnect();
            Thread.Sleep(2000);
            return true;
        }
    #endregion
    }
}
