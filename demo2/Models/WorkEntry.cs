using System;
using SQLite;

namespace demo2.Models
{
    public class WorkEntry
    {
        // PrimaryKey 表示这是数据库表的主键，Id 会自动递增
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        // 工作日期，方便查询和显示
        public DateTime Date { get; set; }
        // 工作小时数
        public double HoursWorked { get; set; }
        // 每小时的工资
        public double HourlyRate { get; set; }
        // 总收入 (由 HoursWorked * HourlyRate 自动计算)
        public double TotalEarnings { get; set; }
        // 备注 (可选，可以添加一些额外信息)
        public string Notes { get; set; }
    }
}