using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnrutadorDeSensor.Entidades
{
  public  class LecturaJson
    {
        public int Id { get; set; }
        public string Presion { get; set; }

        public string Temperatura { get; set; }
        public string Humedad { get; set; }
        public string Fecha { get; set; }
        public string Hora { get; set; }
    }
}
