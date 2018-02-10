using EnrutadorDeSensor.Helpers;
using EnrutadorDeSensor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;
using System.Net;
using Newtonsoft.Json;
using EnrutadorDeSensor.Entidades;
using System.Text;
using System.Net.Http;
using System.Dynamic;
using System.Diagnostics;
// La plantilla de elemento Página en blanco está documentada en https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0xc0a

namespace EnrutadorDeSensor
{
    /// <summary>
    /// Página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;
        private ObservableCollection<DeviceInformation> listOfDevices;
        private CancellationTokenSource ReadCancellationTokenSource;
        string sUrlRequest = "https://metrocontrol.000webhostapp.com/registros"; 
        string path;
        ObservableCollection<Lectura> listaLecturas;
        ObservableCollection<LecturaJson> listaLecturasJson;
        SQLite.Net.SQLiteConnection conn;
        DatabaseHelperClass Db_Helper = new DatabaseHelperClass();//Creating object for DatabaseHelperClass.cs from ViewModel/DatabaseHelp

        public  MainPage()
        {
            this.InitializeComponent();
            listOfDevices = new ObservableCollection<DeviceInformation>();
            ListAvailablePorts();

            //InitialiseGpio();


            serial();
        }



        private async void ListAvailablePorts()
        {
            try
            {
                string aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);

                status.Text = "Select a device and connect";

                for (int i = 0; i < dis.Count; i++)
                {
                    listOfDevices.Add(dis[i]);
                }

                DeviceListSource.Source = listOfDevices;
                comPortInput.IsEnabled = true;
                ConnectDevices.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }


        private bool ConsultarRegistro()
        {
            try
            {

                return true;
            }
            catch (Exception ex)
            {
                return false;
                
            }
        }

        private async void serial()
        {
            try
            {

                string aqs = SerialDevice.GetDeviceSelector("UART0");                   /* Find the selector string for the serial device   */
                var dis = await DeviceInformation.FindAllAsync(aqs);                    /* Find the serial device with our selector string  */
                serialPort = await SerialDevice.FromIdAsync(dis[0].Id);    /* Create an serial device with our selected device */


                if (serialPort == null) return;

                // Disable the 'Connect' button 
                comPortInput.IsEnabled = false;

                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;

                // Display configured settings
                status.Text = "Serial port configured successfully: ";
                status.Text += serialPort.BaudRate + "-";
                status.Text += serialPort.DataBits + "-";
                status.Text += serialPort.Parity.ToString() + "-";
                status.Text += serialPort.StopBits;

                // Set the RcvdText field to invoke the TextChanged callback
                // The callback launches an async Read task to wait for data
                rcvdText.Text = "Waiting for data...";

                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();

                // Enable 'WRITE' button to allow sending data
                sendTextButton.IsEnabled = true;

                Listen();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                comPortInput.IsEnabled = true;
                sendTextButton.IsEnabled = false;
            }
        }

      
        /// <summary>
        /// comPortInput_Click: Action to take when 'Connect' button is clicked
        /// - Get the selected device index and use Id to create the SerialDevice object
        /// - Configure default settings for the serial port
        /// - Create the ReadCancellationTokenSource token
        /// - Start listening on the serial port input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void comPortInput_Click(object sender, RoutedEventArgs e)
        {
            var selection = ConnectDevices.SelectedItems;

            if (selection.Count <= 0)
            {
                status.Text = "Select a device and connect";
                return;
            }

            DeviceInformation entry = (DeviceInformation)selection[0];

            try
            {

                string aqs = SerialDevice.GetDeviceSelector("UART0");                   /* Find the selector string for the serial device   */
                var dis = await DeviceInformation.FindAllAsync(aqs);                    /* Find the serial device with our selector string  */
                serialPort = await SerialDevice.FromIdAsync(dis[0].Id);    /* Create an serial device with our selected device */


                if (serialPort == null) return;

                // Disable the 'Connect' button 
                comPortInput.IsEnabled = false;

                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;

                // Display configured settings
                status.Text = "Serial port configured successfully: ";
                status.Text += serialPort.BaudRate + "-";
                status.Text += serialPort.DataBits + "-";
                status.Text += serialPort.Parity.ToString() + "-";
                status.Text += serialPort.StopBits;

                // Set the RcvdText field to invoke the TextChanged callback
                // The callback launches an async Read task to wait for data
                rcvdText.Text = "Waiting for data...";

                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();

                // Enable 'WRITE' button to allow sending data
                sendTextButton.IsEnabled = true;

                Listen();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                comPortInput.IsEnabled = true;
                sendTextButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// sendTextButton_Click: Action to take when 'WRITE' button is clicked
        /// - Create a DataWriter object with the OutputStream of the SerialDevice
        /// - Create an async task that performs the write operation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void sendTextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (serialPort != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    dataWriteObject = new DataWriter(serialPort.OutputStream);

                    //Launch the WriteAsync task to perform the write
                    await WriteAsync();
                }
                else
                {
                    status.Text = "Select a device and connect";
                }
            }
            catch (Exception ex)
            {
                status.Text = "sendTextButton_Click: " + ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }

        /// <summary>
        /// WriteAsync: Task that asynchronously writes data from the input text box 'sendText' to the OutputStream 
        /// </summary>
        /// <returns></returns>
        private async Task WriteAsync()
        {
            Task<UInt32> storeAsyncTask;

            if (sendText.Text.Length != 0)
            {
                // Load the text from the sendText input text box to the dataWriter object
                dataWriteObject.WriteString(sendText.Text);

                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0)
                {
                    status.Text = sendText.Text + ", ";
                    status.Text += "bytes written successfully!";
                }
                sendText.Text = "";
            }
            else
            {
                status.Text = "Enter the text you want to write and then click on 'WRITE'";
            }
        }

        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Listen()
        {

            try
            {
                if (serialPort != null)
                {
                    dataReaderObject = new DataReader(serialPort.InputStream);

                    // keep reading the serial input
                    while (true)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (TaskCanceledException tce)
            {
                status.Text = "Reading task was cancelled, closing device and cleaning up";
                CloseDevice();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        /// <summary>
        /// ReadAsync: Task that waits on data and reads asynchronously from the serial device InputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReadAsync(CancellationToken cancellationToken)
        {



            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            using (var childCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                // Create a task object to wait for data on the serialPort.InputStream
                loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(childCancellationTokenSource.Token);

    
                // Launch the task and wait
                   UInt32 bytesRead = await loadAsyncTask;
                   if (bytesRead > 0)
                   {
                       string mensaje= dataReaderObject.ReadString(bytesRead);
                    mensaje = mensaje.Replace("\r", ";");
                    mensaje = mensaje.Replace("\u0002", "");
                    String[] substrings = mensaje.Split(';');
                    substrings[0].Replace("41040100000", "");
                    substrings[1].Replace("42010100000", "");
                    substrings[2].Replace("4391010000", "");
                    try
                    {
                        LecturaJson _lectura = new LecturaJson();
                        _lectura.Id = 10;
                        _lectura.Humedad = substrings[0];
                        _lectura.Temperatura = substrings[1];
                        _lectura.Presion = substrings[2];
                        _lectura.Fecha = DateTime.Now.ToString();



                        Db_Helper.Insert(new Lectura()
                        {
                            Humedad = substrings[0],
                            Temperatura = substrings[1],
                            Presion = substrings[2],                          
                            Fecha = DateTime.Now,
                            Estado = false

                        });

                        listaLecturas = Db_Helper.ReadAllLecturas();
                        if (listaLecturas != null)
                        {
                            try
                            {
                                foreach (Lectura item in listaLecturas)
                                {
                                    DateTime date;
                                    LecturaJson _lecturajson = new LecturaJson();
                                    if (!item.Estado)
                                    {
                                        DateTime myDate = new DateTime();
                                        _lecturajson.Id = item.Id;
                                        _lecturajson.Humedad = item.Humedad;
                                        _lecturajson.Presion = item.Presion;
                                        _lecturajson.Temperatura = item.Temperatura;
                                        myDate = item.Fecha;
                                        _lecturajson.Fecha = myDate.Year.ToString() + "-" + myDate.Month.ToString() + "-" + myDate.Day.ToString();
                                        _lecturajson.Hora = myDate.Hour.ToString() + ":" + myDate.Minute.ToString() + ":" + myDate.Second.ToString();
                                        listaLecturasJson.Add(_lecturajson);
                                    }

                                }
                                enviowebAsync(listaLecturasJson);
                            }
                            catch (Exception ex)
                            {

                            }
                           
                        }



                    }
                    catch (Exception ex)
                    {

                    }
                    rcvdText.Text = mensaje;
                       status.Text = "bytes read successfully!";
                   }
            }
        }

        private async void enviowebAsync(ObservableCollection<LecturaJson> lista)
        {

            try
            {
                string json = JsonConvert.SerializeObject(lista);
                WebRequest request = WebRequest.Create(sUrlRequest);
                Uri requestUri = new Uri(sUrlRequest); //replace your Url   
                var objClint = new HttpClient();
                HttpResponseMessage respon = await objClint.PostAsync(requestUri, new StringContent(json, Encoding.UTF8, "application/json"));
                string responJsonText = await respon.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
            }
           
        }

        /// <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }

        /// <summary>
        /// CloseDevice:
        /// - Disposes SerialDevice object
        /// - Clears the enumerated device Id list
        /// </summary>
        private void CloseDevice()
        {
            if (serialPort != null)
            {
                serialPort.Dispose();
            }
            serialPort = null;

            comPortInput.IsEnabled = true;
            sendTextButton.IsEnabled = false;
            rcvdText.Text = "";
            listOfDevices.Clear();
        }

        /// <summary>
        /// closeDevice_Click: Action to take when 'Disconnect and Refresh List' is clicked on
        /// - Cancel all read operations
        /// - Close and dispose the SerialDevice object
        /// - Enumerate connected devices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeDevice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                status.Text = "";
                CancelReadTask();
                CloseDevice();
                ListAvailablePorts();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }

        private void InitialiseGpio()
        {
            GpioController controller = GpioController.GetDefault();

            //Muestra un error si no hay un controlador GPIO
            if (controller != null)
            {

            }
            else
            {

                Debug.WriteLine("No hay controlador GPIO en este dispositivo");
            }
        }

    }
}
