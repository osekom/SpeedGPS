using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using SpeedGPS.Model;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SpeedGPS
{
	public partial class MainPage : ContentPage
	{
        public double time = 5;                                     //tiempo que se mantendra el while y se usara para realizar el calculo
        public Position fromPosi { get; set; }                      //punto de referencia donde inicia el usuario
        public Position toPosi { get; set; }                        //Punto de cierre para el calculo

        public MainPage()
		{
			InitializeComponent();
            DL.Text = "0.0 km/h";
		}

        async Task StartListening()
        {
            if (CrossGeolocator.Current.IsListening)
                return;

            await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(time), 10, true);

            CrossGeolocator.Current.PositionChanged += PositionChanged;
            CrossGeolocator.Current.PositionError += PositionError;
        }

        private void PositionChanged(object sender, PositionEventArgs e)
        {

            activity.IsRunning = true;
            activity.IsVisible = true;
            //obtendremos la posicion actual que se almacenara
            toPosi = e.Position;
            if (fromPosi == null)
            {
                fromPosi = toPosi;
            }

            //Creamos esta funcion para reflejar los datos obtenidos del GPS para Depuracion.
            Depurar(toPosi);

            // se raliza la operacion matematica para comvertir a velocidad en Km/h (Distancia/Tiempo/1000.0)
            DL.Text = ((GetDistance(fromPosi, toPosi) / time) / 1000.0).ToString() + "km/h";

            //una vez mostrado el resultado de la velocidad la posicion actual pasa a ser la antigua
            fromPosi = toPosi;
            activity.IsRunning = false;
            activity.IsVisible = false;
        }

        private void PositionError(object sender, PositionErrorEventArgs e)
        {
            Debug.WriteLine(e.Error);
            //Handle event here for errors
        }

        async Task StopListening()
        {
            if (!CrossGeolocator.Current.IsListening)
                return;

            await CrossGeolocator.Current.StopListeningAsync();

            CrossGeolocator.Current.PositionChanged -= PositionChanged;
            CrossGeolocator.Current.PositionError -= PositionError;
        }

        // obtiene la distancia en metros de 2 coordenadas
        public static double GetDistance(Position point1, Position point2)
        {
            double distance = 0;
            double Lat = (point2.Latitude - point1.Latitude) * (Math.PI / 180);
            double Lon = (point2.Longitude - point1.Longitude) * (Math.PI / 180);
            double a = Math.Sin(Lat / 2) * Math.Sin(Lat / 2) + Math.Cos(point1.Latitude * (Math.PI / 180)) * Math.Cos(point2.Latitude * (Math.PI / 180)) * Math.Sin(Lon / 2) * Math.Sin(Lon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            distance = Constant.EarthRadius * c;
            return distance;
        }

        //indentifica si el GPS esta disponible
        public bool IsLocationAvailable()
        {
            if (!CrossGeolocator.IsSupported)
                return false;

            return CrossGeolocator.Current.IsGeolocationAvailable;
        }

        //evento click al iniciar o parar el rastreo de la velicad.
        private async void SoS_Clicked(object sender, EventArgs e)
        {
            //primero verificamos los permisos de la app para obtener la posicion actual
            //para mas informacion de permisos https://github.com/jamesmontemagno/PermissionsPlugin
            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
                if (status != PermissionStatus.Granted)
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Location))
                    {
                        await DisplayAlert("Aviso", "Necesitamos obtener la ubicacion.", "Entendido");
                    }

                    var results = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Location);
                    if (results.ContainsKey(Permission.Location))
                        status = results[Permission.Location];
                }

                if (status == PermissionStatus.Granted)
                {
                    //verificamos el estado del Boton
                    if (SoS.Text.Equals("INICIAR"))
                    {
                        SoS.Text = "DETENER";
                        await StartListening();
                        
                    }
                    else
                    {
                        SoS.Text = "INICIAR";
                        await StopListening();
                    }
                }
                else
                {
                    await DisplayAlert("Aviso","No tenemos permiso para obtener la ubicacion actual","Entendido");
                }
            }
            catch(Exception ex)
            {
                await DisplayAlert("Error","Error: " + ex,"Ok");
            }
        }

        //depurador de datos obtenidos
        void Depurar(Position PosiActual)
        {
            DepuLabel.Text = string.Format("Latitud: {0}, Longitud: {1}, Altitud: {2}", PosiActual.Latitude, PosiActual.Longitude, PosiActual.Altitude);
        }
    }
}
