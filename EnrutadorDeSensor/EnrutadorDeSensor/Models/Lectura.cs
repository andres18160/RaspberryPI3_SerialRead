using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnrutadorDeSensor.Models
{
  public  class Lectura
    {

        [PrimaryKey, AutoIncrement]  
        public int Id { get; set; }
        public string Presion { get; set; }

        public string Temperatura { get; set; }
        public string Humedad { get; set; }
        public string Fecha { get; set; }

        public bool Estado { get; set; }


        public Lectura()
        {

        }

        public Lectura(string _Presion,string _Temperatura,string _Humedad,string _Fecha)
        {
            Presion = _Presion;
            Temperatura = _Temperatura;
            Humedad = _Humedad;
            Fecha = _Fecha;
            Estado = false;
        }



    }


}
