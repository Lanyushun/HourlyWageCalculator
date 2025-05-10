using System;
using Microsoft.Maui.Controls;

namespace demo2.Views
{
    public partial class AboutPage : ContentPage
    {
        public string BuildDate { get; set; } = "2025��5��";

        public AboutPage()
        {
            InitializeComponent();
            BindingContext = this;
            Console.WriteLine("AboutPage�ѳ�ʼ��");
        }

        private async void OnCopyContactButtonClicked(object sender, EventArgs e)
        {
            try
            {
                await Clipboard.SetTextAsync("2747155774");
                await DisplayAlert("�ɹ�", "QQ���Ѹ��Ƶ�������", "ȷ��");
                Console.WriteLine("��ϵ��ʽ�Ѹ���");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ϵ��ʽʧ��: {ex.Message}");
                await DisplayAlert("����", "����ʧ�ܣ����ֶ�����", "ȷ��");
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            Console.WriteLine("�ӹ���ҳ�淵����ҳ��");
            // ֱ�ӵ�������ҳ�棬������ʹ�� ".." ���·��
            await Shell.Current.GoToAsync("///MainPage");
        }
    }
}