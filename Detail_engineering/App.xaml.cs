using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace Detail_engineering
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // اینجا Utils.BaseDir.Init() رو صدا بزن
            //Utils.BaseDir.Initialize();

            // بعد MainWindow رو باز کن
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
