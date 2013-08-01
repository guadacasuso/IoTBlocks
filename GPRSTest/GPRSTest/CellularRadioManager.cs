using System;
using System.Threading;
using Microsoft.SPOT;

namespace GPRSTest
{
    /// <summary>
    /// A helper to provide TCP capabilities to a <see cref="CellularRadio"/>
    /// </summary>
    public class CellularRadioManager : ITcpServerConnection
    {
        /// <summary>
        /// The International Mobile Station Equipment Identity number of the internal <see cref="CellularRadio"/>
        /// see http://en.wikipedia.org/wiki/International_Mobile_Station_Equipment_Identity
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

        private bool _gprsNetworkRegistered;
        private bool _gsmIsAttached;
        private bool _gprsIsAttached;
        private bool _imeiIsRetrieved;
        private bool _cellRadioIsReady;
        private bool _gprsConnectionRequestSent;

        private readonly int _radioSocket;
        // ReSharper disable NotAccessedField.Local
        private readonly Thread _thread;
        // ReSharper restore NotAccessedField.Local
        /// <summary>
        /// The current <see cref="RadioState"/> of the internal <see cref="CellularRadio"/>
        /// </summary>
        public RadioState RadioState { get; private set; }

        private readonly CellularRadio _radio;

        /// <summary>
        /// Creates a new CellularRadioManager with a <see cref="CellularRadio"/> on the supplied socket
        /// NOTE: Must be called from the Dispatcher thread
        /// </summary>
        /// <param name="socketNumber">The Gadgeteer socket number that the modem is attached to</param>
        public CellularRadioManager(int socketNumber)
        {
            RadioState = RadioState.Unitialized;
            _radioSocket = socketNumber;
            _radio = new CellularRadio(_radioSocket) { DebugPrintEnabled = true };
            _radio.PowerOff();
            RadioState = RadioState.PowerOff;
            _thread = new Thread(RadioManagerThreadRun);
            _thread.Start();
        }

        #region ITcpServerConnection
        /// <summary>
        /// Radio is ready for TCP activity
        /// </summary>
        public bool IsReady { get { return RadioState == RadioState.Ready; } }
        /// <summary>
        /// Radio can be used to send data (TCP is connected to a server)
        /// </summary>
        public bool CanSendData { get { return IsReady && _isTcpConnected; } }
        private readonly TimeSpan _tcpConnectTimeout = new TimeSpan(0, 0, 10);
        private bool _isTcpConnected;

        /// <summary>
        /// Connect to a TCP server.  NOTE: only one simultaneous connection is supported
        /// </summary>
        /// <param name="serverAddress">IP Address or Hostname to connect to</param>
        /// <param name="port">The port to use for this connection</param>
        /// <returns>true if connection successful</returns>
        public bool Connect(string serverAddress, int port = 80)
        {
            if (!IsReady)
            {
                return false;
            }

            var tcpState = _radio.ConnectTCP(serverAddress, port, _tcpConnectTimeout);

            _isTcpConnected = tcpState == CellularRadio.ReturnedState.OK;
            return _isTcpConnected;
        }

        /// <summary>
        /// Disconnect from the current TCP server
        /// </summary>
        /// <returns>true if disconnetion successful</returns>
        public bool Disconnect()
        {
            if (!IsReady) return false;

            Thread.Sleep(1000);
            _radio.SendRawTcpData("+++"); // set to command mode refer manual for correct timings
            Thread.Sleep(500);
            _radio.IsInDataMode = false;

            var disconnectState = _radio.DisconnectTCP();
            _isTcpConnected = false;
            return disconnectState == CellularRadio.ReturnedState.OK;
        }

        /// <summary>
        /// Send data to a TCP server. NOTE: This is a synchronous operation
        /// </summary>
        /// <param name="data"></param>
        /// <returns>true if data was sent successfully</returns>
        public bool SendData(string data)
        {
            if (!CanSendData) return false;

            _radio.IsInDataMode = true;
            var result = _radio.SendRawTcpData(data);	// Blocking call
            return result == CellularRadio.ReturnedState.OK;
        }

        private string _tcpReceiveData = "";
        private int _tcpReceivedDataEventCounter = 0;
        private const int MaxReceiveTimeout = 60000;

        /// <summary>
        /// Expects a complete HTTP response when waiting for data
        /// </summary>
        public bool IsHttpModeEnabled { get; set; }

        /// <summary>
        /// Receive data from the TCP server. e.g. a response to a GET or POST request
        /// </summary>
        /// <param name="timeout">the time in milliseconds to wait for data</param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool ReceiveData(int timeout, out string data)
        {
            return ReceiveData(timeout, out data, false);
        }
        /// <summary>
        /// Receive data from the TCP server. e.g. a response to a GET or POST request
        /// </summary>
        /// <param name="timeout">the time in milliseconds to wait for data</param>
        /// <param name="data"></param>
        /// <param name="returnOnFirstDataReceived">Return after any data is received. Has no effect if IsHttpModeEnabled is true</param>
        /// <returns></returns>
        public bool ReceiveData(int timeout, out string data, bool returnOnFirstDataReceived)
        {
            data = "";
            if (!(CanSendData && _radio.IsInDataMode)) return false;

            if (timeout > MaxReceiveTimeout)
                throw new ArgumentException("Must be less than 60,000ms", "timeout");

            // clear received data
            _tcpReceiveData = "";
            _tcpReceivedDataEventCounter = 0;
            IsDataReceived = false;

            // wait for timeout
            while (timeout-- > 0)
            {
                Thread.Sleep(1);
                if (IsHttpModeEnabled && IsDataReceived)
                {
                    if (_tcpReceiveData.IndexOf("Content-Length:") > -1)
                    {
                        // have we received all the data?
                        var startIx = _tcpReceiveData.IndexOf("Content-Length:") + 15;
                        var endIx = _tcpReceiveData.IndexOf("\r\n", startIx);
                        if (endIx == -1) continue;
                        var contentLength = int.Parse(_tcpReceiveData.Substring(startIx, endIx - startIx));
                        var trimString = _tcpReceiveData.Substring(endIx);
                        trimString = trimString.Trim();
                        if (trimString.Length >= contentLength) break; // finished
                    }
                }
                else if (returnOnFirstDataReceived && IsDataReceived)
                {
                    break; // exit
                }
            }
            // return received data
            //DebugPrint("Event Triggered " + _tcpReceivedDataEventCounter.ToString() + " times");
            //DebugPrint(_tcpReceiveData);
            data = _tcpReceiveData;

            return data.Length > 0;
        }

        /// <summary>
        /// Indicates some data has been received from the TCP server
        /// </summary>
        public bool IsDataReceived { get; private set; }

        private void TcpDataReceived(CellularRadio sender, string data)
        {
            _tcpReceivedDataEventCounter++;
            IsDataReceived = true;
            _tcpReceiveData += data;
        }

        #endregion

        private void RadioManagerThreadRun()
        {
            _radio.ImeiRetrieved += ImeiRetrieved;
            _radio.GprsAttached += GprsAttached;
            _radio.GsmNetworkRegistrationChanged += GsmNetworkRegistrationChanged;
            _radio.GprsNetworkRegistrationChanged += GprsNetworkRegistrationChanged;
            _radio.TcpDataReceived += TcpDataReceived;

            _radio.PowerOn(40);
            RadioState = RadioState.StartingUp;

            while (true)
            {
                if (!_cellRadioIsReady)
                {
                    _cellRadioIsReady = _radio.RetrievePhoneActivity() == CellularRadio.ReturnedState.OK;
                    RadioState = RadioState.StartingUp;
                }

                else if (!_gsmIsAttached)
                {
                    _radio.SendATCommand("AT+CREG?");
                    RadioState = RadioState.WaitingForNetworkRegistration;
                    Thread.Sleep(5000);
                }

                else if (!_gprsNetworkRegistered)
                {
                    _radio.SendATCommand("AT+CCREG?");
                    RadioState = RadioState.WaitingForGprsConnection;
                    Thread.Sleep(5000);
                }

                else if (!_imeiIsRetrieved)
                {
                    RadioState = RadioState.WaitingForIMEI;
                    _radio.RetrieveImei();
                    Thread.Sleep(5000);
                }

                else if (!_gprsIsAttached)
                {
                    if (!_gprsConnectionRequestSent)
                    {
                        bool result;
                        result = _radio.SendATCommand("AT+CIPMODE=1") == CellularRadio.ReturnedState.OK;
                        if (!result) return;
                        Thread.Sleep(2000);
                        result = _radio.AttachGPRS(AccessPointName, AccessPointUser, AccessPointPassword) == CellularRadio.ReturnedState.OK;
                        if (!result) return;
                        Thread.Sleep(10000);
                        // check IP
                        if (_ipAddress.Length < 6) return;
                        _gprsConnectionRequestSent = true;
                    }
                }
                else
                {
                    RadioState = RadioState.Ready;
                }

                Thread.Sleep(2000);

                Debug.Print("RadioState: " + this.RadioState);
            }
        }

        private void GprsNetworkRegistrationChanged(CellularRadio sender, CellularRadio.NetworkRegistrationState networkState)
        {
            if (networkState == CellularRadio.NetworkRegistrationState.Registered ||
                networkState == CellularRadio.NetworkRegistrationState.Roaming)
            {
                _gprsNetworkRegistered = true;
                DebugPrint("GPRS Network Registered");
            }
            else
            {
                _gprsNetworkRegistered = false;
                DebugPrint("GPRS Network Registration Error");
            }
        }

        private void GsmNetworkRegistrationChanged(CellularRadio sender, CellularRadio.NetworkRegistrationState networkState)
        {
            if (networkState == CellularRadio.NetworkRegistrationState.Registered ||
                networkState == CellularRadio.NetworkRegistrationState.Roaming)
            {
                DebugPrint("GSM Network Registered");

                _gsmIsAttached = true;
            }
            else
            {
                _gsmIsAttached = false;
                DebugPrint("GSM Network Registration Error");
            }
        }

        private string _ipAddress = string.Empty;
        private void GprsAttached(CellularRadio sender, string ipAddress)
        {
            _gprsIsAttached = true;
            _ipAddress = ipAddress;
            DebugPrint("IP: " + ipAddress);
        }

        private void ImeiRetrieved(CellularRadio sender, string imei)
        {
            IMEI = imei;
            _imeiIsRetrieved = true;
            DebugPrint("IMEI: " + IMEI);
        }

        private void DebugPrint(string debugString)
        {
            if (DebugPrintEnabled)
            {
                Debug.Print(debugString);
            }
        }
    }
}
