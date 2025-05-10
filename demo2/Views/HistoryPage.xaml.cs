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

        private string filterStatusText = "显示所有记录";
        public string FilterStatusText
        {
            get => filterStatusText;
            set
            {
                SetProperty(ref filterStatusText, value);
            }
        }

        private string emptyViewText = "暂无历史记录";
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
            Console.WriteLine("HistoryPage构造函数: BindingContext已设置");

            RefreshCommand = new Command(async () => await LoadWorkEntriesAsync());
            DeleteCommand = new Command<WorkEntry>(async (entry) => await DeleteEntryAsync(entry));
            EditCommand = new Command<WorkEntry>(async (entry) => await EditEntryAsync(entry));

            // 初始化统计数据展示
            ShowStats = false;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Console.WriteLine("HistoryPage显示中...");

            await database.EnsureInitializedAsync();

            // 加载数据
            await LoadWorkEntriesAsync();

            // 初始化日期选择器
            InitializeDatePickers();
        }

        private void InitializeDatePickers()
        {
            try
            {
                // 清空选择器
                YearPicker.Items.Clear();
                MonthPicker.Items.Clear();
                DayPicker.Items.Clear();

                // 如果没有记录，则添加默认选项
                if (WorkEntries.Count == 0)
                {
                    YearPicker.Items.Add("- 选择年份 -");
                    MonthPicker.Items.Add("- 选择月份 -");
                    DayPicker.Items.Add("- 选择日期 -");

                    YearPicker.SelectedIndex = 0;
                    MonthPicker.SelectedIndex = 0;
                    DayPicker.SelectedIndex = 0;

                    // 禁用选择器
                    YearPicker.IsEnabled = false;
                    MonthPicker.IsEnabled = false;
                    DayPicker.IsEnabled = false;

                    return;
                }

                // 添加"全部"选项
                YearPicker.Items.Add("全部");
                MonthPicker.Items.Add("全部");
                DayPicker.Items.Add("全部");

                // 启用选择器
                YearPicker.IsEnabled = true;
                MonthPicker.IsEnabled = false; // 初始禁用，等选择了年份后启用
                DayPicker.IsEnabled = false;   // 初始禁用，等选择了月份后启用

                // 获取所有不重复的年份
                var years = WorkEntries
                    .Select(e => e.Date.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToList();

                foreach (var year in years)
                {
                    YearPicker.Items.Add(year.ToString() + "年");
                }

                // 默认选择"全部"
                YearPicker.SelectedIndex = 0;
                MonthPicker.SelectedIndex = 0;
                DayPicker.SelectedIndex = 0;

                Console.WriteLine("日期选择器已初始化");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化日期选择器失败: {ex.Message}");
            }
        }

        private void OnYearPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            MonthPicker.Items.Clear();
            DayPicker.Items.Clear();

            // 添加"全部"选项
            MonthPicker.Items.Add("全部");
            DayPicker.Items.Add("全部");

            // 设置默认选择
            MonthPicker.SelectedIndex = 0;
            DayPicker.SelectedIndex = 0;

            // 禁用日期选择器
            DayPicker.IsEnabled = false;

            if (YearPicker.SelectedIndex <= 0)
            {
                // 如果选择了"全部"，则重置年份筛选条件
                selectedYear = null;
                MonthPicker.IsEnabled = false;
                return;
            }

            // 启用月份选择器
            MonthPicker.IsEnabled = true;

            // 获取选中的年份
            string yearText = YearPicker.Items[YearPicker.SelectedIndex];
            if (int.TryParse(yearText.Replace("年", ""), out int year))
            {
                selectedYear = year;

                // 获取该年份下所有不重复的月份
                var months = WorkEntries
                    .Where(e => e.Date.Year == year)
                    .Select(e => e.Date.Month)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToList();

                foreach (var month in months)
                {
                    MonthPicker.Items.Add(month.ToString() + "月");
                }

                Console.WriteLine($"已选择年份: {selectedYear}");
            }
        }

        private void OnMonthPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            DayPicker.Items.Clear();

            // 添加"全部"选项
            DayPicker.Items.Add("全部");

            // 设置默认选择
            DayPicker.SelectedIndex = 0;

            if (MonthPicker.SelectedIndex <= 0 || selectedYear == null)
            {
                // 如果选择了"全部"或者未选择年份，则重置月份筛选条件
                selectedMonth = null;
                DayPicker.IsEnabled = false;
                return;
            }

            // 启用日期选择器
            DayPicker.IsEnabled = true;

            // 获取选中的月份
            string monthText = MonthPicker.Items[MonthPicker.SelectedIndex];
            if (int.TryParse(monthText.Replace("月", ""), out int month))
            {
                selectedMonth = month;

                // 获取该年月下所有不重复的日期
                var days = WorkEntries
                    .Where(e => e.Date.Year == selectedYear && e.Date.Month == month)
                    .Select(e => e.Date.Day)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                foreach (var day in days)
                {
                    DayPicker.Items.Add(day.ToString() + "日");
                }

                Console.WriteLine($"已选择月份: {selectedMonth}");
            }
        }

        private void OnDayPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            if (DayPicker.SelectedIndex <= 0 || selectedMonth == null || selectedYear == null)
            {
                // 如果选择了"全部"或者未选择年月，则重置日期筛选条件
                selectedDay = null;
                return;
            }

            // 获取选中的日期
            string dayText = DayPicker.Items[DayPicker.SelectedIndex];
            if (int.TryParse(dayText.Replace("日", ""), out int day))
            {
                selectedDay = day;
                Console.WriteLine($"已选择日期: {selectedDay}");
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
            Console.WriteLine("应用筛选...");
            Console.WriteLine($"筛选条件: 年={selectedYear}, 月={selectedMonth}, 日={selectedDay}");

            // 克隆一个工作记录列表进行筛选
            var filteredEntries = new List<WorkEntry>(WorkEntries);

            // 根据选择的条件筛选
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

            // 更新筛选状态
            IsFiltered = selectedYear.HasValue || selectedMonth.HasValue || selectedDay.HasValue;

            // 按日期降序排序
            filteredEntries = filteredEntries.OrderByDescending(e => e.Date).ToList();

            // 更新UI
            MainThread.BeginInvokeOnMainThread(() => {
                FilteredWorkEntries.Clear();
                foreach (var entry in filteredEntries)
                {
                    FilteredWorkEntries.Add(entry);
                }

                // 更新筛选状态文本
                UpdateFilterStatusText();

                // 更新空视图文本
                if (FilteredWorkEntries.Count == 0 && IsFiltered)
                {
                    EmptyViewText = "没有符合筛选条件的记录";
                }
                else
                {
                    EmptyViewText = "暂无历史记录";
                }

                // 计算并显示统计信息
                CalculateStatistics();
            });

            Console.WriteLine($"筛选结果: {FilteredWorkEntries.Count} 条记录");
        }

        private void UpdateFilterStatusText()
        {
            if (!IsFiltered)
            {
                FilterStatusText = "显示所有记录";
                return;
            }

            string yearText = selectedYear.HasValue ? $"{selectedYear.Value}年" : "";
            string monthText = selectedMonth.HasValue ? $"{selectedMonth.Value}月" : "";
            string dayText = selectedDay.HasValue ? $"{selectedDay.Value}日" : "";

            FilterStatusText = $"筛选: {yearText}{monthText}{dayText} ({FilteredWorkEntries.Count} 条记录)";
        }

        private void ResetFilter()
        {
            Console.WriteLine("重置筛选...");

            // 重置筛选条件
            selectedYear = null;
            selectedMonth = null;
            selectedDay = null;

            // 重置选择器
            YearPicker.SelectedIndex = 0;
            MonthPicker.Items.Clear();
            DayPicker.Items.Clear();

            MonthPicker.Items.Add("全部");
            DayPicker.Items.Add("全部");

            MonthPicker.SelectedIndex = 0;
            DayPicker.SelectedIndex = 0;

            MonthPicker.IsEnabled = false;
            DayPicker.IsEnabled = false;

            // 更新UI
            IsFiltered = false;
            ShowStats = false;

            // 恢复原始记录列表
            MainThread.BeginInvokeOnMainThread(() => {
                FilteredWorkEntries.Clear();
                foreach (var entry in WorkEntries)
                {
                    FilteredWorkEntries.Add(entry);
                }

                // 更新筛选状态文本
                FilterStatusText = "显示所有记录";

                // 更新空视图文本
                EmptyViewText = "暂无历史记录";
            });

            Console.WriteLine("筛选已重置");
        }

        private void CalculateStatistics()
        {
            if (FilteredWorkEntries.Count == 0)
            {
                ShowStats = false;
                return;
            }

            // 计算总工时和总收入
            TotalHours = FilteredWorkEntries.Sum(e => e.HoursWorked);
            TotalEarnings = FilteredWorkEntries.Sum(e => e.TotalEarnings);

            // 显示统计信息
            ShowStats = true;

            Console.WriteLine($"统计: 总工时={TotalHours:F2}, 总收入={TotalEarnings:F2}");
        }

        private async Task LoadWorkEntriesAsync()
        {
            if (IsBusy)
            {
                Console.WriteLine("已在加载中，跳过重复操作");
                return;
            }

            IsBusy = true;
            Console.WriteLine("开始加载历史记录...");

            try
            {
                // 确保数据库已初始化
                await database.EnsureInitializedAsync();

                // 获取所有记录
                List<WorkEntry> entries = await database.GetWorkEntriesAsync();
                Console.WriteLine($"从数据库获取到 {entries.Count} 条记录");

                // 按日期降序排序
                entries = entries.OrderByDescending(e => e.Date).ToList();
                Console.WriteLine("记录已按日期排序");

                // 更新集合，保持UI响应
                MainThread.BeginInvokeOnMainThread(() => {
                    WorkEntries.Clear();
                    FilteredWorkEntries.Clear();
                    Console.WriteLine("清空集合");

                    foreach (var entry in entries)
                    {
                        WorkEntries.Add(entry);
                        FilteredWorkEntries.Add(entry);
                        Console.WriteLine($"添加记录: ID={entry.Id}, 日期={entry.Date:yyyy-MM-dd}, 工时={entry.HoursWorked}, 总收入={entry.TotalEarnings}");
                    }

                    // 检查CollectionView是否有数据源绑定
                    Console.WriteLine($"CollectionView ItemsSource绑定状态: {HistoryCollectionView.ItemsSource != null}");
                    Console.WriteLine($"ObservableCollection数量: {WorkEntries.Count}");
                });

                if (entries.Count == 0)
                {
                    await DisplayAlert("提示", "目前没有任何工时记录。请返回主页面添加记录。", "确定");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载历史记录失败: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");

                // 显示更详细的错误信息
                await DisplayAlert("错误", $"加载历史记录失败: {ex.Message}", "确定");
            }
            finally
            {
                IsBusy = false;
                Console.WriteLine("加载完成");
            }
        }

        private async Task DeleteEntryAsync(WorkEntry entryToDelete)
        {
            if (entryToDelete == null)
            {
                Console.WriteLine("无法删除: 记录为空");
                return;
            }

            Console.WriteLine($"准备删除ID为{entryToDelete.Id}的记录");
            bool confirm = await DisplayAlert("确认删除", $"确定要删除 {entryToDelete.Date.ToShortDateString()} 的记录吗？", "是", "否");
            if (confirm)
            {
                try
                {
                    Console.WriteLine("用户确认删除");
                    var result = await database.DeleteWorkEntryAsync(entryToDelete);
                    Console.WriteLine($"删除结果: 成功={result.Success}, 消息={result.Message}");

                    if (result.Success)
                    {
                        // 从两个集合中移除记录
                        WorkEntries.Remove(entryToDelete);
                        FilteredWorkEntries.Remove(entryToDelete);

                        Console.WriteLine("记录已从集合中移除");
                        await DisplayAlert("删除成功", "记录已删除。", "确定");

                        // 更新统计信息和日期选择器
                        if (IsFiltered)
                        {
                            UpdateFilterStatusText();
                            CalculateStatistics();
                        }

                        // 重新初始化日期选择器
                        InitializeDatePickers();
                    }
                    else
                    {
                        await DisplayAlert("删除失败", result.Message, "确定");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除记录失败: {ex.Message}");
                    Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                    await DisplayAlert("错误", $"删除记录失败: {ex.Message}", "确定");
                }
            }
            else
            {
                Console.WriteLine("用户取消删除");
            }
        }

        private async Task EditEntryAsync(WorkEntry entryToEdit)
        {
            if (entryToEdit == null)
            {
                Console.WriteLine("无法编辑: 记录为空");
                return;
            }

            try
            {
                Console.WriteLine($"准备编辑ID为{entryToEdit.Id}的记录");
                await Shell.Current.GoToAsync($"///MainPage?workEntryId={entryToEdit.Id}");
                Console.WriteLine("已导航到主页面进行编辑");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导航到编辑页面失败: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                await DisplayAlert("错误", $"无法打开编辑页面: {ex.Message}", "确定");
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            Console.WriteLine("返回主页面");
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