using EnrutadorDeSensor.Models;
using SQLite.Net;
using SQLite.Net.Platform.WinRT;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EnrutadorDeSensor.Helpers
{
    class DatabaseHelperClass
    {
        //Create Tabble   
        public void CreateDatabase(string DB_PATH)
        {
            if (!CheckFileExists(DB_PATH).Result)
            {
                using (SQLiteConnection conn = new SQLiteConnection(new SQLitePlatformWinRT(), DB_PATH))
                {
                    conn.CreateTable<Lectura>();

                }
            }
        }
        private async Task<bool> CheckFileExists(string fileName)
        {
            try
            {
                var store = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
                return true;
            }
            catch
            {
                return false;
            }
        }
        // Insert the new contact in the Contacts table.   
        public void Insert(Lectura _lectura)
        {
            using (SQLiteConnection conn = new SQLiteConnection(new SQLitePlatformWinRT(), App.DB_PATH))
            {
                conn.RunInTransaction(() =>
                {
                    conn.Insert(_lectura);
                });
            }
        }
 
        public Lectura ReadLectura(int id)
        {
            using (SQLiteConnection conn = new SQLiteConnection(new SQLitePlatformWinRT(), App.DB_PATH))
            {
                var existLectura = conn.Query<Lectura>("select * from Lectura where Id =" + id).FirstOrDefault();
                return existLectura;
            }
        }
        public ObservableCollection<Lectura> ReadAllLecturas()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(new SQLitePlatformWinRT(), App.DB_PATH))
                {
                    List<Lectura> myCollection = conn.Table<Lectura>().ToList<Lectura>();
                    ObservableCollection<Lectura> LecturaList = new ObservableCollection<Lectura>(myCollection);
                    return LecturaList;
                }
            }
            catch
            {
                return null;
            }

        }
        //Update existing Lectura   
        public void UpdateDetails(Lectura _Lectura)
        {
            using (SQLiteConnection conn = new SQLiteConnection(new SQLitePlatformWinRT(), App.DB_PATH))
            {

                var existLectura = conn.Query<Lectura>("select * from Lectura where Id =" + _Lectura.Id).FirstOrDefault();
                if (existLectura != null)
                {

                    conn.RunInTransaction(() =>
                    {
                        conn.Update(_Lectura);
                    });
                }

            }
        }

        //Update Stado Lectura  por id
        public void UpdateLecturaEstado(Lectura _Lectura)
        {
            using (SQLiteConnection conn = new SQLiteConnection(new SQLitePlatformWinRT(), App.DB_PATH))
            {

                var existLectura = conn.Query<Lectura>("select * from Lectura where Id =" + _Lectura.Id).FirstOrDefault();
                if (existLectura != null)
                {

                    conn.RunInTransaction(() =>
                    {
                        conn.Update(_Lectura);
                    });
                }

            }
        }
        //Delete all LecturaList or delete Lectura table     
        public void DeleteLectura()
        {
            using (SQLiteConnection conn = new SQLiteConnection(new SQLitePlatformWinRT(), App.DB_PATH))
            {

                conn.DropTable<Lectura>();
                conn.CreateTable<Lectura>();
                conn.Dispose();
                conn.Close();

            }
        }
        //Delete specific Lectura     
        public void DeleteLectura(int Id)
        {
            using (SQLiteConnection conn = new SQLiteConnection(new SQLitePlatformWinRT(), App.DB_PATH))
            {

                var existLectura = conn.Query<Lectura>("select * from Lectura where Id =" + Id).FirstOrDefault();
                if (existLectura != null)
                {
                    conn.RunInTransaction(() =>
                    {
                        conn.Delete(existLectura);
                    });
                }
            }
        }
    }
}