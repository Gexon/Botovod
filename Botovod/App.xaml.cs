using Botovod.Models;
using System;
using System.Windows;

namespace Botovod
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// https://ru.stackoverflow.com/questions/562578/%d0%a2%d0%be%d1%87%d0%ba%d0%b0-%d0%b2%d1%85%d0%be%d0%b4%d0%b0-%d0%b2-mvvm-app-xaml-cs-%d0%b8%d0%bb%d0%b8-%d0%bf%d1%80%d0%b5%d0%b4%d1%81%d1%82%d0%b0%d0%b2%d0%bb%d0%b5%d0%bd%d0%b8%d0%b5
    /// </summary>
    public partial class App : Application
    {
        private Calculator calculator;
        private MainVM mainVM;
        protected override void OnStartup(StartupEventArgs e)
        {
            // наш калькулятор, создаем тут на всякий пожарный.
            calculator = new Calculator(new InitializedData());
            
            base.OnStartup(e); 
            MainWindow window = new MainWindow();
            // Создаем модель представления ViewModel(у нас это mainVM) к которой привязываем главное окно.
            mainVM = new MainVM(calculator);
            // Когда ViewModel попросит закрыться, 
            // закроем его ХД. 
            mainVM.RequestClose += delegate { window.Close(); };
            //
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(mainVM.CurrentDomain_ProcessExit);
            // Разрешаем всем элементам управления в окне
            // привязываться к ViewModel, установив
            // DataContext, который распространяется вниз
            // по дереву элементов.
            window.DataContext = mainVM;
            window.Show();
        }
    }
}
