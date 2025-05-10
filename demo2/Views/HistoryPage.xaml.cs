using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using demo2.Data;
using demo2.Models;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Maui.ApplicationModel;

namespace demo2.Views
{
    public partial class HistoryPage : ContentPage
    {
        private WorkEntryDatabase database;
        public ObservableCollection<WorkEntry> WorkEntries { get; set; } = new ObservableCollection<WorkEntry>();
        public ObservableCollection<WorkEntry> FilteredWorkEntries { get; set; } = new ObservableCollection<WorkEntry>();

        private bool isBusy;
        public bool IsBusy
        {
            get => isBusy;
            set
            {
                SetProperty(ref isBusy, value);
            }
        }

        private bool isFiltered;
        public bool IsFiltered
        {
            get => isFiltered;
            set
            {
                SetProperty(ref isFiltered, value);
            }
        }

        private string filterStatusText = "��ʾ���м�¼";
        public string FilterStatusText
        {
            get => filterStatusText;
            set
            {
                SetProperty(ref filterStatusText, value);
            }
        }

        private string emptyViewText = "������ʷ��¼";
        public string EmptyViewText
        {
            get => emptyViewText;
            set
            {
                SetProperty(ref emptyViewText, value);
            }
        }

        private bool showStats;
        public bool ShowStats
        {
            get => showStats;
            set
            {
                SetProperty(ref showStats, value);
            }
        }

        private double totalHours;
        public double TotalHours
        {
            get => totalHours;
            set
            {
                SetProperty(ref totalHours, value);
            }
        }

        private double totalEarnings;
        public double TotalEarnings
        {
            get => totalEarnings;
            set
            {
                SetProperty(ref totalEarnings, value);
            }
        }

        private int? selectedYear;
        private int? selectedMonth;
        private int? selectedDay;

        public Command RefreshCommand { get; }
        public Command<WorkEntry> DeleteCommand { get; }
        public Command<WorkEntry> EditCommand { get; }

        public HistoryPage()
        {
            InitializeComponent();
            database = new WorkEntryDatabase();
            BindingContext = this;
            Console.WriteLine("HistoryPage���캯��: BindingContext������");

            RefreshCommand = new Command(async () => await LoadWorkEntriesAsync());
            DeleteCommand = new Command<WorkEntry>(async (entry) => await DeleteEntryAsync(entry));
            EditCommand = new Command<WorkEntry>(async (entry) => await EditEntryAsync(entry));

            // ��ʼ��ͳ������չʾ
            ShowStats = false;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Console.WriteLine("HistoryPage��ʾ��...");

            await database.EnsureInitializedAsync();

            // ��������
            await LoadWorkEntriesAsync();

            // ��ʼ������ѡ����
            InitializeDatePickers();
        }

        private void InitializeDatePickers()
        {
            try
            {
                // ���ѡ����
                YearPicker.Items.Clear();
                MonthPicker.Items.Clear();
                DayPicker.Items.Clear();

                // ���û�м�¼�������Ĭ��ѡ��
                if (WorkEntries.Count == 0)
                {
                    YearPicker.Items.Add("- ѡ����� -");
                    MonthPicker.Items.Add("- ѡ���·� -");
                    DayPicker.Items.Add("- ѡ������ -");

                    YearPicker.SelectedIndex = 0;
                    MonthPicker.SelectedIndex = 0;
                    DayPicker.SelectedIndex = 0;

                    // ����ѡ����
                    YearPicker.IsEnabled = false;
                    MonthPicker.IsEnabled = false;
                    DayPicker.IsEnabled = false;

                    return;
                }

                // ���"ȫ��"ѡ��
                YearPicker.Items.Add("ȫ��");
                MonthPicker.Items.Add("ȫ��");
                DayPicker.Items.Add("ȫ��");

                // ����ѡ����
                YearPicker.IsEnabled = true;
                MonthPicker.IsEnabled = false; // ��ʼ���ã���ѡ������ݺ�����
                DayPicker.IsEnabled = false;   // ��ʼ���ã���ѡ�����·ݺ�����

                // ��ȡ���в��ظ������
                var years = WorkEntries
                    .Select(e => e.Date.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToList();

                foreach (var year in years)
                {
                    YearPicker.Items.Add(year.ToString() + "��");
                }

                // Ĭ��ѡ��"ȫ��"
                YearPicker.SelectedIndex = 0;
                MonthPicker.SelectedIndex = 0;
                DayPicker.SelectedIndex = 0;

                Console.WriteLine("����ѡ�����ѳ�ʼ��");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"��ʼ������ѡ����ʧ��: {ex.Message}");
            }
        }

        private void OnYearPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            MonthPicker.Items.Clear();
            DayPicker.Items.Clear();

            // ���"ȫ��"ѡ��
            MonthPicker.Items.Add("ȫ��");
            DayPicker.Items.Add("ȫ��");

            // ����Ĭ��ѡ��
            MonthPicker.SelectedIndex = 0;
            DayPicker.SelectedIndex = 0;

            // ��������ѡ����
            DayPicker.IsEnabled = false;

            if (YearPicker.SelectedIndex <= 0)
            {
                // ���ѡ����"ȫ��"�����������ɸѡ����
                selectedYear = null;
                MonthPicker.IsEnabled = false;
                return;
            }

            // �����·�ѡ����
            MonthPicker.IsEnabled = true;

            // ��ȡѡ�е����
            string yearText = YearPicker.Items[YearPicker.SelectedIndex];
            if (int.TryParse(yearText.Replace("��", ""), out int year))
            {
                selectedYear = year;

                // ��ȡ����������в��ظ����·�
                var months = WorkEntries
                    .Where(e => e.Date.Year == year)
                    .Select(e => e.Date.Month)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToList();

                foreach (var month in months)
                {
                    MonthPicker.Items.Add(month.ToString() + "��");
                }

                Console.WriteLine($"��ѡ�����: {selectedYear}");
            }
        }

        private void OnMonthPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            DayPicker.Items.Clear();

            // ���"ȫ��"ѡ��
            DayPicker.Items.Add("ȫ��");

            // ����Ĭ��ѡ��
            DayPicker.SelectedIndex = 0;

            if (MonthPicker.SelectedIndex <= 0 || selectedYear == null)
            {
                // ���ѡ����"ȫ��"����δѡ����ݣ��������·�ɸѡ����
                selectedMonth = null;
                DayPicker.IsEnabled = false;
                return;
            }

            // ��������ѡ����
            DayPicker.IsEnabled = true;

            // ��ȡѡ�е��·�
            string monthText = MonthPicker.Items[MonthPicker.SelectedIndex];
            if (int.TryParse(monthText.Replace("��", ""), out int month))
            {
                selectedMonth = month;

                // ��ȡ�����������в��ظ�������
                var days = WorkEntries
                    .Where(e => e.Date.Year == selectedYear && e.Date.Month == month)
                    .Select(e => e.Date.Day)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                foreach (var day in days)
                {
                    DayPicker.Items.Add(day.ToString() + "��");
                }

                Console.WriteLine($"��ѡ���·�: {selectedMonth}");
            }
        }

        private void OnDayPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            if (DayPicker.SelectedIndex <= 0 || selectedMonth == null || selectedYear == null)
            {
                // ���ѡ����"ȫ��"����δѡ�����£�����������ɸѡ����
                selectedDay = null;
                return;
            }

            // ��ȡѡ�е�����
            string dayText = DayPicker.Items[DayPicker.SelectedIndex];
            if (int.TryParse(dayText.Replace("��", ""), out int day))
            {
                selectedDay = day;
                Console.WriteLine($"��ѡ������: {selectedDay}");
            }
        }

        private void OnFilterButtonClicked(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void OnResetFilterButtonClicked(object sender, EventArgs e)
        {
            ResetFilter();
        }

        private void OnViewStatsButtonClicked(object sender, EventArgs e)
        {
            CalculateStatistics();
        }

        private void ApplyFilter()
        {
            Console.WriteLine("Ӧ��ɸѡ...");
            Console.WriteLine($"ɸѡ����: ��={selectedYear}, ��={selectedMonth}, ��={selectedDay}");

            // ��¡һ��������¼�б����ɸѡ
            var filteredEntries = new List<WorkEntry>(WorkEntries);

            // ����ѡ�������ɸѡ
            if (selectedYear.HasValue)
            {
                filteredEntries = filteredEntries.Where(e => e.Date.Year == selectedYear.Value).ToList();

                if (selectedMonth.HasValue)
                {
                    filteredEntries = filteredEntries.Where(e => e.Date.Month == selectedMonth.Value).ToList();

                    if (selectedDay.HasValue)
                    {
                        filteredEntries = filteredEntries.Where(e => e.Date.Day == selectedDay.Value).ToList();
                    }
                }
            }

            // ����ɸѡ״̬
            IsFiltered = selectedYear.HasValue || selectedMonth.HasValue || selectedDay.HasValue;

            // �����ڽ�������
            filteredEntries = filteredEntries.OrderByDescending(e => e.Date).ToList();

            // ����UI
            MainThread.BeginInvokeOnMainThread(() => {
                FilteredWorkEntries.Clear();
                foreach (var entry in filteredEntries)
                {
                    FilteredWorkEntries.Add(entry);
                }

                // ����ɸѡ״̬�ı�
                UpdateFilterStatusText();

                // ���¿���ͼ�ı�
                if (FilteredWorkEntries.Count == 0 && IsFiltered)
                {
                    EmptyViewText = "û�з���ɸѡ�����ļ�¼";
                }
                else
                {
                    EmptyViewText = "������ʷ��¼";
                }

                // ���㲢��ʾͳ����Ϣ
                CalculateStatistics();
            });

            Console.WriteLine($"ɸѡ���: {FilteredWorkEntries.Count} ����¼");
        }

        private void UpdateFilterStatusText()
        {
            if (!IsFiltered)
            {
                FilterStatusText = "��ʾ���м�¼";
                return;
            }

            string yearText = selectedYear.HasValue ? $"{selectedYear.Value}��" : "";
            string monthText = selectedMonth.HasValue ? $"{selectedMonth.Value}��" : "";
            string dayText = selectedDay.HasValue ? $"{selectedDay.Value}��" : "";

            FilterStatusText = $"ɸѡ: {yearText}{monthText}{dayText} ({FilteredWorkEntries.Count} ����¼)";
        }

        private void ResetFilter()
        {
            Console.WriteLine("����ɸѡ...");

            // ����ɸѡ����
            selectedYear = null;
            selectedMonth = null;
            selectedDay = null;

            // ����ѡ����
            YearPicker.SelectedIndex = 0;
            MonthPicker.Items.Clear();
            DayPicker.Items.Clear();

            MonthPicker.Items.Add("ȫ��");
            DayPicker.Items.Add("ȫ��");

            MonthPicker.SelectedIndex = 0;
            DayPicker.SelectedIndex = 0;

            MonthPicker.IsEnabled = false;
            DayPicker.IsEnabled = false;

            // ����UI
            IsFiltered = false;
            ShowStats = false;

            // �ָ�ԭʼ��¼�б�
            MainThread.BeginInvokeOnMainThread(() => {
                FilteredWorkEntries.Clear();
                foreach (var entry in WorkEntries)
                {
                    FilteredWorkEntries.Add(entry);
                }

                // ����ɸѡ״̬�ı�
                FilterStatusText = "��ʾ���м�¼";

                // ���¿���ͼ�ı�
                EmptyViewText = "������ʷ��¼";
            });

            Console.WriteLine("ɸѡ������");
        }

        private void CalculateStatistics()
        {
            if (FilteredWorkEntries.Count == 0)
            {
                ShowStats = false;
                return;
            }

            // �����ܹ�ʱ��������
            TotalHours = FilteredWorkEntries.Sum(e => e.HoursWorked);
            TotalEarnings = FilteredWorkEntries.Sum(e => e.TotalEarnings);

            // ��ʾͳ����Ϣ
            ShowStats = true;

            Console.WriteLine($"ͳ��: �ܹ�ʱ={TotalHours:F2}, ������={TotalEarnings:F2}");
        }

        private async Task LoadWorkEntriesAsync()
        {
            if (IsBusy)
            {
                Console.WriteLine("���ڼ����У������ظ�����");
                return;
            }

            IsBusy = true;
            Console.WriteLine("��ʼ������ʷ��¼...");

            try
            {
                // ȷ�����ݿ��ѳ�ʼ��
                await database.EnsureInitializedAsync();

                // ��ȡ���м�¼
                List<WorkEntry> entries = await database.GetWorkEntriesAsync();
                Console.WriteLine($"�����ݿ��ȡ�� {entries.Count} ����¼");

                // �����ڽ�������
                entries = entries.OrderByDescending(e => e.Date).ToList();
                Console.WriteLine("��¼�Ѱ���������");

                // ���¼��ϣ�����UI��Ӧ
                MainThread.BeginInvokeOnMainThread(() => {
                    WorkEntries.Clear();
                    FilteredWorkEntries.Clear();
                    Console.WriteLine("��ռ���");

                    foreach (var entry in entries)
                    {
                        WorkEntries.Add(entry);
                        FilteredWorkEntries.Add(entry);
                        Console.WriteLine($"��Ӽ�¼: ID={entry.Id}, ����={entry.Date:yyyy-MM-dd}, ��ʱ={entry.HoursWorked}, ������={entry.TotalEarnings}");
                    }

                    // ���CollectionView�Ƿ�������Դ��
                    Console.WriteLine($"CollectionView ItemsSource��״̬: {HistoryCollectionView.ItemsSource != null}");
                    Console.WriteLine($"ObservableCollection����: {WorkEntries.Count}");
                });

                if (entries.Count == 0)
                {
                    await DisplayAlert("��ʾ", "Ŀǰû���κι�ʱ��¼���뷵����ҳ����Ӽ�¼��", "ȷ��");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ʷ��¼ʧ��: {ex.Message}");
                Console.WriteLine($"�쳣��ջ: {ex.StackTrace}");

                // ��ʾ����ϸ�Ĵ�����Ϣ
                await DisplayAlert("����", $"������ʷ��¼ʧ��: {ex.Message}", "ȷ��");
            }
            finally
            {
                IsBusy = false;
                Console.WriteLine("�������");
            }
        }

        private async Task DeleteEntryAsync(WorkEntry entryToDelete)
        {
            if (entryToDelete == null)
            {
                Console.WriteLine("�޷�ɾ��: ��¼Ϊ��");
                return;
            }

            Console.WriteLine($"׼��ɾ��IDΪ{entryToDelete.Id}�ļ�¼");
            bool confirm = await DisplayAlert("ȷ��ɾ��", $"ȷ��Ҫɾ�� {entryToDelete.Date.ToShortDateString()} �ļ�¼��", "��", "��");
            if (confirm)
            {
                try
                {
                    Console.WriteLine("�û�ȷ��ɾ��");
                    var result = await database.DeleteWorkEntryAsync(entryToDelete);
                    Console.WriteLine($"ɾ�����: �ɹ�={result.Success}, ��Ϣ={result.Message}");

                    if (result.Success)
                    {
                        // �������������Ƴ���¼
                        WorkEntries.Remove(entryToDelete);
                        FilteredWorkEntries.Remove(entryToDelete);

                        Console.WriteLine("��¼�ѴӼ������Ƴ�");
                        await DisplayAlert("ɾ���ɹ�", "��¼��ɾ����", "ȷ��");

                        // ����ͳ����Ϣ������ѡ����
                        if (IsFiltered)
                        {
                            UpdateFilterStatusText();
                            CalculateStatistics();
                        }

                        // ���³�ʼ������ѡ����
                        InitializeDatePickers();
                    }
                    else
                    {
                        await DisplayAlert("ɾ��ʧ��", result.Message, "ȷ��");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ɾ����¼ʧ��: {ex.Message}");
                    Console.WriteLine($"�쳣��ջ: {ex.StackTrace}");
                    await DisplayAlert("����", $"ɾ����¼ʧ��: {ex.Message}", "ȷ��");
                }
            }
            else
            {
                Console.WriteLine("�û�ȡ��ɾ��");
            }
        }

        private async Task EditEntryAsync(WorkEntry entryToEdit)
        {
            if (entryToEdit == null)
            {
                Console.WriteLine("�޷��༭: ��¼Ϊ��");
                return;
            }

            try
            {
                Console.WriteLine($"׼���༭IDΪ{entryToEdit.Id}�ļ�¼");
                await Shell.Current.GoToAsync($"///MainPage?workEntryId={entryToEdit.Id}");
                Console.WriteLine("�ѵ�������ҳ����б༭");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"�������༭ҳ��ʧ��: {ex.Message}");
                Console.WriteLine($"�쳣��ջ: {ex.StackTrace}");
                await DisplayAlert("����", $"�޷��򿪱༭ҳ��: {ex.Message}", "ȷ��");
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            Console.WriteLine("������ҳ��");
            await Shell.Current.GoToAsync("///MainPage");
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}