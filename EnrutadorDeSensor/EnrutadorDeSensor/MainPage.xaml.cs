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
using Windows.System.Threading;
using Windows.UI.Core;
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
        string sUrlRequest = "http://metrocontrol.metrolegal.com.co/registros"; 
        string path;
        ObservableCollection<Lectura> listaLecturas;
        ObservableCollection<LecturaJson> listaLecturasJson;
        SQLite.Net.SQLiteConnection conn;
        DateTime tiempoAnterior = DateTime.Now;
        DatabaseHelperClass Db_Helper = new DatabaseHelperClass();//Creating object for DatabaseHelperClass.cs from ViewModel/DatabaseHelp

        public  MainPage()
        {
            this.InitializeComponent();
            lblFechaHora.Text = DateTime.Now.ToString();
            TimeSpan period = TimeSpan.FromMilliseconds(10000);
            TimeSpan PeriodoTiempo = TimeSpan.FromMilliseconds(1000);


            ThreadPoolTimer PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
               {
                   await Dispatcher.RunAsync(CoreDispatcherPriority.High,
                       () =>
                       {
                           enviowebAsync();
                       });

               }, period);

            ThreadPoolTimer PeriodoTiempoTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.High,
                    () =>
                    {
                        ActualizarHora();
                    });

            }, period);


            serial();
        }

        private void ActualizarHora()
        {
            lblFechaHora.Text = DateTime.Now.ToString();
        }

        private async void enviowebAsync()
        {
            listaLecturasJson = new ObservableCollection<LecturaJson>();
            try
            {
                listaLecturas = Db_Helper.ReadAllLecturas();
                if (listaLecturas != null)
                {

                    foreach (Lectura item in listaLecturas)
                    {
                        try
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



                                string json = JsonConvert.SerializeObject(_lecturajson);
                                WebRequest request = WebRequest.Create(sUrlRequest);
                                Uri requestUri = new Uri(sUrlRequest);
                                var objClint = new HttpClient();
                                HttpResponseMessage respon = await objClint.PostAsync(requestUri, new StringContent(json, Encoding.UTF8, "application/json"));
                                string responJsonText = await respon.Content.ReadAsStringAsync();
                                var respuestaJson = JsonConvert.DeserializeObject(responJsonText);
                                Debug.WriteLine("RESPUESTA DE ENVIO =" + responJsonText);
                                item.Estado = true;
                                Db_Helper.UpdateDetails(item);

                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("ERROR =" + ex.Message);
                        }

                    }        
                }

            }
            catch (Exception ex)
            {
                txtLog.Text = "ERROR=" + ex.Message;
                Debug.WriteLine("ERROR=" + ex.Message);
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
                Denuevo:
                string aqs = SerialDevice.GetDeviceSelector();                  
                var dis = await DeviceInformation.FindAllAsync(aqs);
                foreach (var item in dis)
                {
                    if(item.Name== "CP2102 USB to UART Bridge Controller")
                    {
                        serialPort = await SerialDevice.FromIdAsync(item.Id);
                    }                   
                }

                if (serialPort == null)
                {
                    txtLog.Text = "Conecta El dispositivo!";
                    goto Denuevo;
                }

                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;

                // Display configured settings
                txtLog.Text = "Serial port configured successfully: ";
                txtLog.Text += serialPort.BaudRate + "-";
                txtLog.Text += serialPort.DataBits + "-";
                txtLog.Text += serialPort.Parity.ToString() + "-";
                txtLog.Text += serialPort.StopBits;



                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();



                Listen();
            }
            catch (Exception ex)
            {
                txtLog.Text = ex.Message;
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
                txtLog.Text = "Reading task was cancelled, closing device and cleaning up";
                CloseDevice();
            }
            catch (Exception ex)
            {
                txtLog.Text = ex.Message;
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

                string mensaje="";
                // Launch the task and wait
                   UInt32 bytesRead = await loadAsyncTask;
                   if (bytesRead > 0)
                   {

                    try
                    {
                        mensaje = dataReaderObject.ReadString(bytesRead);
                        Debug.WriteLine("LECTURA="+ mensaje);
                        txtLecturas.Text = mensaje;
                        mensaje = mensaje.Replace("\r", ";");
                         mensaje = mensaje.Replace("\u0002", "");
                        

                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        byte[] rawdata = new byte[bytesRead];
                        dataReaderObject.ReadBytes(rawdata);
                        Debug.WriteLine("Lectura ex=="+Encoding.Unicode.GetString(rawdata, 0, rawdata.Length));
                    }
                    try
                    {
                            Lectura obj = new Lectura();
                            String[] substrings = mensaje.Split(';');
                            substrings[0]= substrings[0].Replace("41040100000", "");
                            var chars0 = substrings[0].ToCharArray();
                            obj.Humedad = chars0[0] + "" + chars0[1] + "." + chars0[2];
                            substrings[1]=  substrings[1].Replace("42010100000", "");
                            var chars1 = substrings[1].ToCharArray();
                            obj.Temperatura = chars1[0] + "" + chars1[1] + "." + chars1[2];
                            substrings[2]= substrings[2].Replace("4391010000", "");
                            var chars2 = substrings[2].ToCharArray();
                            obj.Presion = chars2[0] + "" + chars2[1] + "" + chars2[2] + "." + chars2[3];
                            obj.Fecha = DateTime.Parse(lblFechaHora.Text.ToString());
                            obj.Estado = false;
                            TimeSpan tiempoTranscurrido = obj.Fecha - tiempoAnterior;
                            TimeSpan interval = new TimeSpan(0, 1, 00);
                            Debug.WriteLine("Tiempo Transcurrido="+ tiempoTranscurrido);
                 
                            if(tiempoTranscurrido >= interval)
                            {
                                Db_Helper.Insert(obj);
                                Debug.WriteLine("Registro Creado");
                                tiempoAnterior = obj.Fecha;
                            }
                        }                    
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Error almacenando la informacion en la db ="+ex.Message);
                        }
                    
                   }
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
                txtLog.Text = "";
                CancelReadTask();
                CloseDevice();
            }
            catch (Exception ex)
            {
                txtLog.Text = ex.Message;
            }
        }

      
    }
}
