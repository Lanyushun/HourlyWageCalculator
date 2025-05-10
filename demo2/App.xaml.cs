using Microsoft.Maui.Controls;

namespace demo2
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
            Console.WriteLine("应用程序已启动，AppShell已设置为MainPage");
        }

        protected override void OnStart()
        {
            base.OnStart();
            Console.WriteLine("应用程序已开始运行");
        }
    }
}