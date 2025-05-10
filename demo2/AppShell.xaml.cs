using Microsoft.Maui.Controls;
using demo2.Views;

namespace demo2
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // 注册路由
            Routing.RegisterRoute("MainPage", typeof(MainPage));
            Routing.RegisterRoute("HistoryPage", typeof(HistoryPage));
            Routing.RegisterRoute("AboutPage", typeof(AboutPage));

            Console.WriteLine("AppShell: 路由已注册");
        }
    }
}