using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SQLite;
using demo2.Models;
using System.IO;

namespace demo2.Data
{
    public class WorkEntryDatabase
    {
        private SQLiteAsyncConnection _database;
        private static SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private bool _isInitialized;

        public WorkEntryDatabase()
        {
            // 构造函数保持简单，实际初始化在第一次使用时进行
            Console.WriteLine("WorkEntryDatabase实例已创建");
        }

        // 确保数据库初始化的方法
        public async Task EnsureInitializedAsync()
        {
            if (_isInitialized)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized)
                    return;

                Console.WriteLine("初始化数据库...");
                if (_database == null)
                {
                    // 获取数据库文件的完整路径
                    var dbPath = Path.Combine(FileSystem.AppDataDirectory, "WorkEntries.db");
                    Console.WriteLine($"数据库路径: {dbPath}");

                    // 检查目录是否存在
                    var directory = Path.GetDirectoryName(dbPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                        Console.WriteLine($"创建目录: {directory}");
                    }

                    // 尝试创建或打开数据库
                    _database = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
                    Console.WriteLine("SQLite连接已创建");
                }

                // 创建 WorkEntry 表，如果它不存在的话
                await _database.CreateTableAsync<WorkEntry>();
                Console.WriteLine("WorkEntry表已创建");

                // 检查表是否创建成功
                var tableInfo = await _database.GetTableInfoAsync("WorkEntry");
                Console.WriteLine($"WorkEntry表信息: {tableInfo.Count} 列");

                _isInitialized = true;
                Console.WriteLine("数据库初始化完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据库初始化失败: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                throw new InvalidOperationException("无法初始化数据库。详情请查看日志。", ex);
            }
            finally
            {
                _initLock.Release();
            }
        }

        // 获取所有工作记录
        public async Task<List<WorkEntry>> GetWorkEntriesAsync()
        {
            await EnsureInitializedAsync();
            try
            {
                Console.WriteLine("正在从数据库获取所有记录...");
                var entries = await _database.Table<WorkEntry>().ToListAsync();
                Console.WriteLine($"获取到 {entries.Count} 条记录");
                return entries;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"从数据库获取记录失败: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                return new List<WorkEntry>(); // 发生错误时返回空列表
            }
        }

        // 根据 ID 获取单条工作记录
        public async Task<WorkEntry> GetWorkEntryAsync(int id)
        {
            await EnsureInitializedAsync();
            try
            {
                Console.WriteLine($"正在获取ID为{id}的记录...");
                var entry = await _database.Table<WorkEntry>().Where(i => i.Id == id).FirstOrDefaultAsync();
                Console.WriteLine($"获取记录结果: {(entry != null ? "成功" : "未找到")}");
                return entry;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"从数据库获取单条记录失败 (ID: {id}): {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                throw new InvalidOperationException($"无法获取ID为{id}的记录。", ex);
            }
        }

        // 验证工作记录
        private bool ValidateWorkEntry(WorkEntry entry, out string errorMessage)
        {
            if (entry == null)
            {
                errorMessage = "记录不能为空。";
                return false;
            }

            if (entry.HoursWorked < 0)
            {
                errorMessage = "工作小时数不能为负数。";
                return false;
            }

            if (entry.HourlyRate < 0)
            {
                errorMessage = "每小时工资不能为负数。";
                return false;
            }

            // 可以添加更多验证规则

            errorMessage = string.Empty;
            return true;
        }

        // 保存或更新工作记录
        public async Task<(bool Success, string Message, int RecordId)> SaveWorkEntryAsync(WorkEntry entry)
        {
            await EnsureInitializedAsync();

            // 验证输入
            if (!ValidateWorkEntry(entry, out string errorMessage))
            {
                Console.WriteLine($"记录验证失败: {errorMessage}");
                return (false, errorMessage, 0);
            }

            try
            {
                int result;
                if (entry.Id != 0)
                {
                    Console.WriteLine($"更新ID为{entry.Id}的记录...");
                    result = await _database.UpdateAsync(entry);
                    Console.WriteLine($"更新结果: {result}");
                    return (result > 0, "更新成功。", entry.Id);
                }
                else
                {
                    Console.WriteLine("插入新记录...");
                    result = await _database.InsertAsync(entry);
                    Console.WriteLine($"插入结果: ID={result}");
                    return (result > 0, "保存成功。", result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存/更新记录失败 (ID: {entry.Id}): {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                return (false, $"保存失败: {ex.Message}", 0);
            }
        }

        // 删除工作记录
        public async Task<(bool Success, string Message)> DeleteWorkEntryAsync(WorkEntry entry)
        {
            await EnsureInitializedAsync();
            if (entry == null || entry.Id == 0)
            {
                Console.WriteLine("删除记录失败: 无效的记录");
                return (false, "无效的记录。");
            }

            try
            {
                Console.WriteLine($"删除ID为{entry.Id}的记录...");
                int result = await _database.DeleteAsync(entry);
                Console.WriteLine($"删除结果: {result}");
                return (result > 0, "删除成功。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除记录失败 (ID: {entry.Id}): {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                return (false, $"删除失败: {ex.Message}");
            }
        }

        // 添加测试记录方法
        public async Task<bool> AddTestRecordAsync()
        {
            await EnsureInitializedAsync();

            try
            {
                var testEntry = new WorkEntry
                {
                    Date = DateTime.Today,
                    HoursWorked = 8,
                    HourlyRate = 100,
                    TotalEarnings = 800,
                    Notes = "测试记录"
                };

                Console.WriteLine("添加测试记录...");
                var result = await _database.InsertAsync(testEntry);
                Console.WriteLine($"测试记录添加结果: ID={result}");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加测试记录失败: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                return false;
            }
        }

        // 查询记录数量
        public async Task<int> GetRecordCountAsync()
        {
            await EnsureInitializedAsync();
            try
            {
                var count = await _database.Table<WorkEntry>().CountAsync();
                Console.WriteLine($"数据库中的记录数量: {count}");
                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取记录数量失败: {ex.Message}");
                return 0;
            }
        }
    }
}