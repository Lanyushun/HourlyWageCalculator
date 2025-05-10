using System;
using Microsoft.Maui.Controls;

namespace demo2.Views
{
    public partial class AboutPage : ContentPage
    {
        public string BuildDate { get; set; } = "2025年5月";

        public AboutPage()
        {
            InitializeComponent();
            BindingContext = this;
            Console.WriteLine("AboutPage已初始化");
        }

        private async void OnCopyContactButtonClicked(object sender, EventArgs e)
        {
            try
            {
                await Clipboard.SetTextAsync("2747155774");
                await DisplayAlert("成功", "QQ号已复制到剪贴板", "确定");
                Console.WriteLine("联系方式已复制");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"复制联系方式失败: {ex.Message}");
                await DisplayAlert("错误", "复制失败，请手动复制", "确定");
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            Console.WriteLine("从关于页面返回主页面");
            // 直接导航到主页面，而不是使用 ".." 相对路径
            await Shell.Current.GoToAsync("///MainPage");
        }
    }
}