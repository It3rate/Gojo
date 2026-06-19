using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Ports;

namespace ScratchMill.Comm
{
    public class ComPort : IDisposable
    {
        public SerialPort port { get; }
        public bool IsConncected { get; private set; } = false;
        public bool CanContinue => _continueReading;
        public string PortName { get; private set; } = "COM6";
        public int PortBaudRate { get; private set; } = 115200;

        private int _connRetries = 0;
        private const int _maxConnRetries = 2;
        private bool _continueReading = false;
        private readonly object _portLock = new object();
        private readonly ConcurrentQueue<string> _responseQueue = new ConcurrentQueue<string>();

        public bool IsOpen => port.IsOpen;

        public ComPort()
        {
            port = new SerialPort();
            port.DataReceived += (sender, e) =>
            {
                if (_continueReading && port.IsOpen)
                {
                    try
                    {
                        lock (_portLock)
                        {
                            string data = port.ReadExisting();
                            if (!string.IsNullOrEmpty(data))
                            {
                                _responseQueue.Enqueue(data);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error in DataReceived: {ex.Message}");
                    }
                }
            };
        }

        public string ReadLine()
        {
            lock (_portLock)
            {
                return port.ReadLine();
            }
        }

        public bool TryDequeueResponse(out string response)
        {
            return _responseQueue.TryDequeue(out response);
        }

        public void Write(string txt)
        {
            lock (_portLock)
            {
                port.Write(txt);
            }
        }

        public void WriteLine(string txt)
        {
            lock (_portLock)
            {
                port.WriteLine(txt);
            }
        }

        public void Close()
        {
            lock (_portLock)
            {
                port.Close();
            }
        }

        public bool OpenSerialPort(SerialDataReceivedEventHandler readSerialHandle)
        {
            if (!port.IsOpen)
            {
                _connRetries = 0;
                IsConncected = false;
                while (_connRetries < _maxConnRetries && !port.IsOpen)
                {
                    try
                    {
                        port.PortName = PortName;
                        port.DataBits = 8;
                        port.BaudRate = PortBaudRate;
                        port.Parity = Parity.None;
                        port.StopBits = StopBits.One;
                        port.Handshake = Handshake.None;
                        port.DtrEnable = false;
                        port.ReadTimeout = 1000; // Reduced timeout
                        port.WriteTimeout = 1000;

                        _continueReading = true;
                        port.DataReceived += readSerialHandle;

                        port.Open();
                        IsConncected = true;
                        Trace.WriteLine($"Connection successful to {PortName}.");
                        return true;
                    }
                    catch (Exception e)
                    {
                        _connRetries++;
                        Trace.WriteLine($"Failed to open {PortName} {_connRetries} times. \n{e.Message}");
                        IsConncected = false;
                        if (_connRetries < _maxConnRetries)
                        {
                            Thread.Sleep(500);
                        }
                    }
                }
            }
            Trace.WriteLine($"Connection to {PortName} unsuccessful after {_maxConnRetries} attempts.");
            return false;
        }

        public bool FindGrblController(SerialDataReceivedEventHandler readSerialHandle, out string foundPort)
        {
            foundPort = null;
            string[] ports = SerialPort.GetPortNames();
            if (!ports.Any())
            {
                Trace.WriteLine("No COM ports found on the system.");
                return false;
            }

            foreach (string portName in ports)
            {
                Trace.WriteLine($"Trying COM port: {portName}");
                try
                {
                    lock (_portLock)
                    {
                        if (port.IsOpen)
                        {
                            port.Close();
                        }

                        port.PortName = portName;
                        port.BaudRate = 115200;
                        port.DataBits = 8;
                        port.Parity = Parity.None;
                        port.StopBits = StopBits.One;
                        port.Handshake = Handshake.None;
                        port.DtrEnable = false;
                        port.ReadTimeout = 500;
                        port.WriteTimeout = 500;

                        port.Open();
                    }
                    while (_responseQueue.TryDequeue(out _)) { }

                    _continueReading = true;
                    // Send $I to get version info
                    WriteLine("$I");
                    Thread.Sleep(500); // Wait for response

                    bool grblFound = false;
                    string combinedResponse = "";
                    for (int i = 0; i < 3; i++) // Try multiple times to catch response
                    {
                        while (_responseQueue.TryDequeue(out string response))
                        {
                            combinedResponse += response;
                            if (combinedResponse.Contains("ok") && (combinedResponse.Contains("Grbl") || combinedResponse.Contains("VER")))
                            {
                                Trace.WriteLine($"GRBL controller found on {portName}: {combinedResponse}");
                                foundPort = portName;
                                PortName = portName;
                                IsConncected = true;
                                _continueReading = true;
                                port.DataReceived += readSerialHandle;
                                grblFound = true;
                                break;
                            }
                        }
                        if (grblFound) break;
                        Thread.Sleep(200); // Wait for more data
                    }

                    if (grblFound)
                    {
                        return true;
                    }
                    else
                    {
                        Trace.WriteLine($"No GRBL controller on {portName}. Response: {combinedResponse}");
                        lock (_portLock)
                        {
                            port.Close();
                        }
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Error testing {portName}: {e.Message}");
                    lock (_portLock)
                    {
                        if (port.IsOpen)
                        {
                            port.Close();
                        }
                    }
                }
            }

            Trace.WriteLine("No GRBL controller found on any COM port.");
            return false;
        }

        public void CloseSerialPort()
        {
            try
            {
                if (port.IsOpen)
                {
                    _continueReading = false;
                    lock (_portLock)
                    {
                        port.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to close {PortName}. \n{e.Message}");
            }
            finally
            {
                _continueReading = false;
                IsConncected = false;
            }
        }

        public void Dispose()
        {
            CloseSerialPort();
            lock (_portLock)
            {
                port.Dispose();
            }
        }
    }
}