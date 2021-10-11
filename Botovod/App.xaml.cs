using Botovod.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Botovod
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// https://ru.stackoverflow.com/questions/562578/%d0%a2%d0%be%d1%87%d0%ba%d0%b0-%d0%b2%d1%85%d0%be%d0%b4%d0%b0-%d0%b2-mvvm-app-xaml-cs-%d0%b8%d0%bb%d0%b8-%d0%bf%d1%80%d0%b5%d0%b4%d1%81%d1%82%d0%b0%d0%b2%d0%bb%d0%b5%d0%bd%d0%b8%d0%b5
    /// </summary>
    public partial class App : Application
    {
        private static Calculator calculator = new Calculator(new InitializedData());
        MainVM mainVM = new MainVM(calculator);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            new MainWindow() { DataContext = mainVM }.Show();
        }
    }
}
