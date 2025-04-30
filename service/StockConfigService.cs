using stock_tool.common;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace stock_tool.service;

class StockConfigService
{
    private TextBox stockTextBox;
    private string StockFilePath;

    private static StockConfigService _instance;

    public StockConfigService(TextBox textBox)
    {
        StockFilePath = Config.Get("StockFilePath");
        stockTextBox = textBox;
        stockTextBox.LostFocus += StockTextBox_LostFocus;
        LoadStockFromFile();
    }

    internal static void Init(TextBox stockTextBox)
    {
       _instance = new StockConfigService(stockTextBox);
    }

    private void LoadStockFromFile()
    {
        try
        {
            if (File.Exists(StockFilePath))
            {
                string json = File.ReadAllText(StockFilePath);
                var stockList = JsonSerializer.Deserialize<List<StockItem>>(json);
                foreach (var item in stockList)
                {
                    if (item.key == "num")
                    {
                        stockTextBox.Text = item.value;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
           Logger.Error($"读取库存配置时出错: {ex.Message}");
        }
    }

    private void StockTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(stockTextBox.Text))
        {
            return;
        }

        // 检查库存是否为数字
        if (!stockTextBox.Text.Contains("-"))
        {
            if (!int.TryParse(stockTextBox.Text, out int stockNum))
            {
                MessageBox.Show("库存格式为[数字-数字]");
                return;
            }
        }
        else {
            string[] split = stockTextBox.Text.Split('-');
            if (!int.TryParse(split[0], out int stockNum1) 
                || !int.TryParse(split[1], out int stockNum2))
            {
                MessageBox.Show("库存格式为[数字-数字]");
                return;
            }
        }

        try
        {
            if (File.Exists(StockFilePath))
            {
                string json = File.ReadAllText(StockFilePath);
                var stockList = JsonSerializer.Deserialize<List<StockItem>>(json);
                bool updated = false;
                foreach (var item in stockList)
                {
                    if (item.key == "num")
                    {
                        item.value = stockTextBox.Text;
                        updated = true;
                        break;
                    }
                }
                if (updated)
                {
                    string newJson = JsonSerializer.Serialize(stockList);
                    File.WriteAllText(StockFilePath, newJson);
                    MessageBox.Show($"配置已改为 {stockTextBox.Text}", "信息");
                }
            }
            else { 
                File.WriteAllText(StockFilePath, $"[{{\"key\":\"num\",\"value\":\"{stockTextBox.Text}\"}}]");
                MessageBox.Show($"配置已改为 {stockTextBox.Text}", "信息");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"更新库存文件时出错: {ex.Message}");
        }
    }

    private class StockItem
    {
        public string key { get; set; }
        public string value { get; set; }
    }
}
