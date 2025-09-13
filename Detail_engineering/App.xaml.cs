using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
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
            //PathHelper.BaseDir = @"\\192.168.94.4\Ardestan Dehshir\1-DCC\4.Detail Engineering";

            PathHelper.BaseDir = Settings.Default.BaseFolder ?? "";

            PathHelper.JsonPath = Settings.Default.JsonPath ?? "";

            // بعد MainWindow رو باز کن
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
