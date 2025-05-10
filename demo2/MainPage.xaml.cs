using System;
using System.Globalization;
using demo2.Data;
using demo2.Models;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace demo2
{
    [QueryProperty(nameof(WorkEntryId), "workEntryId")]
    public partial class MainPage : ContentPage
    {
        private WorkEntryDatabase database;
        private WorkEntry _currentWorkEntry;
        private bool _isLoading = false; // 防止在加载时触发事件

        // 接收来自 HistoryPage 的编辑记录 ID
        private int _workEntryId;
        public int WorkEntryId
        {
            get => _workEntryId;
            set
            {
                _workEntryId = value;
                if (value > 0)
                {
                    Console.WriteLine($"接收到编辑记录ID: {value}");
                    LoadWorkEntryForEditAsync(value).SafeFireAndForget(
                        onException: ex => DisplayAlert("错误", $"加载记录时发生错误: {ex.Message}", "确定")
                    );
                }
            }
        }

        private async void OnAboutButtonClicked(object sender, EventArgs e)
        {
            Console.WriteLine("导航到关于页面");
            await Shell.Current.GoToAsync("///AboutPage");
        }

        public MainPage()
        {
            InitializeComponent();
            database = new WorkEntryDatabase();
            Console.WriteLine("MainPage已初始化");

            // 默认设置为当天
            DateInput.Date = DateTime.Today;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Console.WriteLine("MainPage显示中...");
            await database.EnsureInitializedAsync();
            CalculateEstimatedEarnings();
        }

        // 根据 ID 加载记录用于编辑
        private async Task LoadWorkEntryForEditAsync(int id)
        {
            if (_isLoading || id <= 0) return;

            _isLoading = true;
            try
            {
                Console.WriteLine($"开始加载ID为{id}的记录进行编辑...");
                _currentWorkEntry = await database.GetWorkEntryAsync(id);
                if (_currentWorkEntry != null)
                {
                    // 将记录数据显示到输入框
                    DateInput.Date = _currentWorkEntry.Date;
                    HoursWorkedEntry.Text = _currentWorkEntry.HoursWorked.ToString("F2", CultureInfo.InvariantCulture);
                    HourlyRateEntry.Text = _currentWorkEntry.HourlyRate.ToString("F2", CultureInfo.InvariantCulture);

                    // 更新UI显示状态
                    RecordButton.Text = "更新记录";
                    CalculateEstimatedEarnings();

                    Console.WriteLine("编辑模式: 记录已加载");
                    await DisplayAlert("编辑模式", "已加载记录进行编辑。", "确定");
                }
                else
                {
                    Console.WriteLine($"找不到ID为{id}的记录");
                    await DisplayAlert("错误", "找不到要编辑的记录。", "确定");
                    ResetForm();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载记录失败: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                await DisplayAlert("错误", $"加载记录失败: {ex.Message}", "确定");
                ResetForm();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void ResetForm()
        {
            // 重置为新建模式
            _currentWorkEntry = null;
            HoursWorkedEntry.Text = string.Empty;
            HourlyRateEntry.Text = string.Empty;
            DateInput.Date = DateTime.Today;
            RecordButton.Text = "记录工时";
            CalculateEstimatedEarnings();
            Console.WriteLine("表单已重置为新建模式");
        }

        private void OnHoursOrRateChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading) return;
            CalculateEstimatedEarnings();
        }

        private void CalculateEstimatedEarnings()
        {
            if (double.TryParse(HoursWorkedEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double hours) &&
                double.TryParse(HourlyRateEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double rate))
            {
                if (hours < 0 || rate < 0)
                {
                    TotalEarningsLabel.Text = "小时数和时薪不能为负数";
                    TotalEarningsLabel.TextColor = Colors.Red;
                    return;
                }

                double total = hours * rate;
                TotalEarningsLabel.Text = $"{total:F2} 元";
                TotalEarningsLabel.TextColor = Colors.Green;
            }
            else
            {
                TotalEarningsLabel.Text = "请输入有效数字";
                TotalEarningsLabel.TextColor = Colors.Red;
            }
        }

        private async void OnRecordButtonClicked(object sender, EventArgs e)
        {
            if (!ValidateInputs())
            {
                return;
            }

            try
            {
                double hours = double.Parse(HoursWorkedEntry.Text, CultureInfo.InvariantCulture);
                double rate = double.Parse(HourlyRateEntry.Text, CultureInfo.InvariantCulture);

                // 创建一个新的WorkEntry对象，避免直接修改_currentWorkEntry
                var entryToSave = new WorkEntry
                {
                    Date = DateInput.Date,
                    HoursWorked = hours,
                    HourlyRate = rate,
                    TotalEarnings = hours * rate,
                    Notes = ""
                };

                // 如果是编辑模式，则保留ID
                if (_currentWorkEntry != null)
                {
                    entryToSave.Id = _currentWorkEntry.Id;
                    entryToSave.Notes = _currentWorkEntry.Notes; // 保留原有备注
                    Console.WriteLine($"正在更新ID为{_currentWorkEntry.Id}的记录");
                }
                else
                {
                    Console.WriteLine("正在创建新记录");
                }

                Console.WriteLine($"记录详情: 日期={entryToSave.Date:yyyy-MM-dd}, 工时={entryToSave.HoursWorked}, 时薪={entryToSave.HourlyRate}, 总收入={entryToSave.TotalEarnings}");

                var result = await database.SaveWorkEntryAsync(entryToSave);
                Console.WriteLine($"保存结果: 成功={result.Success}, 消息={result.Message}, 记录ID={result.RecordId}");

                if (result.Success)
                {
                    await DisplayAlert("保存成功", $"已记录 {entryToSave.Date.ToShortDateString()} 的 {entryToSave.HoursWorked} 小时工时，总收入 {entryToSave.TotalEarnings:F2} 元。", "确定");
                    ResetForm(); // 重置表单
                }
                else
                {
                    await DisplayAlert("保存失败", result.Message, "确定");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存记录时发生错误: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                await DisplayAlert("错误", $"保存时发生错误: {ex.Message}", "确定");
            }
        }

        private bool ValidateInputs()
        {
            if (!double.TryParse(HoursWorkedEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double hours) ||
                !double.TryParse(HourlyRateEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double rate))
            {
                DisplayAlert("输入错误", "请确保小时数和每小时工资都输入了有效数字。", "确定").SafeFireAndForget();
                return false;
            }

            if (hours < 0)
            {
                DisplayAlert("输入错误", "工作小时数不能为负数。", "确定").SafeFireAndForget();
                return false;
            }

            if (rate < 0)
            {
                DisplayAlert("输入错误", "每小时工资不能为负数。", "确定").SafeFireAndForget();
                return false;
            }

            return true;
        }

        private async void OnViewHistoryButtonClicked(object sender, EventArgs e)
        {
            Console.WriteLine("导航到历史记录页面");
            await Shell.Current.GoToAsync("///HistoryPage");
        }
    }
}