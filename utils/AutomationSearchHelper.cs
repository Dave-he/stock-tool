using stock_tool.common;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace stock_tool.utils
{
    public static class AutomationSearchHelper
    {
        /// <summary>
        /// 在 UI 自动化树中查找符合条件的元素
        /// </summary>
        /// <param name="parent">起始查找的父元素</param>
        /// <param name="condition">元素筛选条件</param>
        /// <returns>符合条件的元素列表</returns>
        public static List<AutomationElement> FindElements(AutomationElement parent, Condition condition = null)
        {
            List<AutomationElement> result = new List<AutomationElement>();
            if (parent == null)
            {
                return result;
            }

            // 创建 TreeWalker 用于遍历树
            TreeWalker walker = TreeWalker.ControlViewWalker;
            AutomationElement element = walker.GetFirstChild(parent);

            while (element != null)
            {
                if (condition == null || element.FindAll(TreeScope.Element, condition).Count > 0)
                {
                    result.Add(element);
                }
                // 递归查找子元素
                result.AddRange(FindElements(element, condition));
                element = walker.GetNextSibling(element);
            }

            return result;
        }

        /// <summary>
        /// 在 UI 自动化树中查找第一个符合条件的元素
        /// </summary>
        /// <param name="parent">起始查找的父元素</param>
        /// <param name="condition">元素筛选条件</param>
        /// <returns>第一个符合条件的元素，如果未找到则返回 null</returns>
        public static AutomationElement FindFirstElement(AutomationElement parent, Condition condition)
        {
            if (parent == null)
            {
                return null;
            }

            // 创建 TreeWalker 用于遍历树
            TreeWalker walker = TreeWalker.ControlViewWalker;
            AutomationElement element = walker.GetFirstChild(parent);

            while (element != null)
            {
                if (condition == null || element.FindAll(TreeScope.Element, condition).Count > 0)
                {
                    return element;
                }
                // 递归查找子元素
                AutomationElement foundChild = FindFirstElement(element, condition);
                if (foundChild != null)
                {
                    return foundChild;
                }
                element = walker.GetNextSibling(element);
            }

            return null;
        }

        /// <summary>
        /// 异步查找符合条件的所有元素
        /// </summary>
        /// <param name="rootElement">根元素</param>
        /// <param name="condition">筛选条件</param>
        /// <returns>包含所有符合条件元素的列表</returns>
        public static async Task<List<AutomationElement>> FindAllElementsAsync(AutomationElement rootElement, Condition condition = null)
        {
            return await Task.Run(() =>
            {
                var result = new List<AutomationElement>();
                if (rootElement == null)
                {
                    return result;
                }

                TreeWalker walker = TreeWalker.ControlViewWalker;
                AutomationElement element = walker.GetFirstChild(rootElement);

                while (element != null)
                {
                    if (condition == null || element.FindAll(TreeScope.Element, condition).Count > 0)
                    {
                        result.Add(element);
                    }
                    result.AddRange(FindAllElementsRecursive(element, walker, condition));
                    element = walker.GetNextSibling(element);
                }

                return result;
            });
        }

        private static List<AutomationElement> FindAllElementsRecursive(AutomationElement parentElement, TreeWalker walker, Condition condition)
        {
            var elements = new List<AutomationElement>();
            AutomationElement childElement = walker.GetFirstChild(parentElement);

            while (childElement != null)
            {
                if (condition == null || childElement.FindAll(TreeScope.Element, condition).Count > 0)
                {
                    elements.Add(childElement);
                }
                elements.AddRange(FindAllElementsRecursive(childElement, walker, condition));
                childElement = walker.GetNextSibling(childElement);
            }

            return elements;
        }

        /// <summary>
        /// 异步查找符合条件的第一个元素
        /// </summary>
        /// <param name="rootElement">根元素</param>
        /// <param name="condition">筛选条件</param>
        /// <returns>第一个符合条件的元素，如果未找到则返回 null</returns>
        public static async Task<AutomationElement> FindFirstElementAsync(AutomationElement rootElement, Condition condition)
        {
            return await Task.Run(() =>
            {
                if (rootElement == null)
                {
                    return null;
                }

                TreeWalker walker = TreeWalker.ControlViewWalker;
                AutomationElement element = walker.GetFirstChild(rootElement);

                while (element != null)
                {
                    if (condition == null || element.FindAll(TreeScope.Element, condition).Count > 0)
                    {
                        return element;
                    }
                    var found = FindFirstElementRecursive(element, walker, condition);
                    if (found != null)
                    {
                        return found;
                    }
                    element = walker.GetNextSibling(element);
                }

                return null;
            });
        }

        private static AutomationElement FindFirstElementRecursive(AutomationElement parentElement, TreeWalker walker, Condition condition)
        {
            AutomationElement childElement = walker.GetFirstChild(parentElement);

            while (childElement != null)
            {
                if (condition == null || childElement.FindAll(TreeScope.Element, condition).Count > 0)
                {
                    return childElement;
                }
                var found = FindFirstElementRecursive(childElement, walker, condition);
                if (found != null)
                {
                    return found;
                }
                childElement = walker.GetNextSibling(childElement);
            }

            return null;
        }

        /// <summary>
        /// 获取指定父元素下的所有子窗口
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>子窗口元素列表</returns>
        public static async Task<List<AutomationElement>> GetChildWindows(AutomationElement parent)
        {
            // 假设窗口元素的 ControlType 为 Window
            Condition windowCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
            return await FindAllElementsAsync(parent, windowCondition);
        }

        /// <summary>
        /// 获取指定父元素下的第一个符合条件的子窗口
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <param name="condition">筛选条件</param>
        /// <returns>符合条件的子窗口元素，如果未找到则返回 null</returns>
        public static AutomationElement GetFirstChildWindow(AutomationElement parent, Condition condition = null)
        {
            // 假设窗口元素的 ControlType 为 Window
            Condition windowCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
            if (condition != null)
            {
                windowCondition = new AndCondition(windowCondition, condition);
            }
            return FindFirstElement(parent, windowCondition);
        }

        /// <summary>
        /// 根据窗口标题查找窗口
        /// </summary>
        /// <param name="windowTitle">窗口标题</param>
        /// <returns>找到的窗口元素，如果未找到则返回 null</returns>
        public static AutomationElement FindWindowByTitle(AutomationElement parent, string windowTitle)
        { 
            Condition condition = new PropertyCondition(AutomationElement.NameProperty, windowTitle);
            return GetFirstChildWindow(parent, condition) ;
        }

        public static List<AutomationElement> FindElementById(AutomationElement parent, string id) { 
            Condition condition = new PropertyCondition(AutomationElement.AutomationIdProperty, id);
            AutomationElementCollection foundElements = parent.FindAll(TreeScope.Descendants, condition);
            return foundElements.Cast<AutomationElement>().ToList();
        }

        public static AutomationElement FindFirstElementById(AutomationElement parent, string id)
        {
            Condition condition = new PropertyCondition(AutomationElement.AutomationIdProperty, id);
            return parent.FindFirst(TreeScope.Descendants, condition);
        }



        public static AutomationElement FindFirstElementByName(AutomationElement parent, string name)
        {
            Condition condition = new PropertyCondition(AutomationElement.NameProperty, name);
            return parent.FindFirst(TreeScope.Descendants, condition);
        }


        /// <summary>
        /// 在 Panel 元素中查找表格元素
        /// </summary>
        /// <param name="panelElement">Panel 元素</param>
        /// <returns>找到的表格元素，如果未找到则返回 null</returns>
        public static AutomationElement FindTableInPanel(AutomationElement panelElement)
        {
            Condition tableCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Table);
            return panelElement.FindFirst(TreeScope.Descendants, tableCondition);
        }


        /// <summary>
        /// 打印表格信息
        /// </summary>
        /// <param name="tableElement">表格元素</param>
        public static void PrintTableInfo(AutomationElement tableElement)
        {
            if (tableElement.TryGetCurrentPattern(GridPattern.Pattern, out object gridPatternObj))
            {
                GridPattern gridPattern = (GridPattern)gridPatternObj;

                int rowCount = gridPattern.Current.RowCount;
                int columnCount = gridPattern.Current.ColumnCount;

                Logger.Info($"表格 {tableElement.Current.Name}  行数: {rowCount}");
                Logger.Info($"表格 {tableElement.Current.AutomationId} 列数: {columnCount}");

                // 可以进一步遍历单元格
                for (int row = 0; row < rowCount; row++)
                {
                    for (int col = 0; col < columnCount; col++)
                    {
                        AutomationElement cell = gridPattern.GetItem(row, col);
                        if (cell != null)
                        {
                            string cellName = cell.Current.Name;
                            Logger.Info($"第 {row + 1} 行，第 {col + 1} 列的单元格名称: {cellName}");
                        }
                    }
                }
            }
            else
            {
                Logger.Info("找到的元素不支持 GridPattern，可能不是表格。");
            }
        }


        /// <summary>
        /// 尝试点击指定元素
        /// </summary>
        /// <param name="element">要点击的元素</param>
        /// <returns>如果点击成功返回 true，否则返回 false</returns>
        public static bool TryClickElement(AutomationElement element)
        {
            if (element.TryGetCurrentPattern(InvokePattern.Pattern, out object pattern))
            {
                InvokePattern invokePattern = (InvokePattern)pattern;
                invokePattern.Invoke();
                return true;
            }
            return false;
        }


        /// <summary>
        /// 尝试激活指定窗体
        /// </summary>
        /// <param name="windowElement">要激活的窗体元素</param>
        /// <returns>如果激活成功返回 true，否则返回 false</returns>
        public static bool TryActivateWindow(AutomationElement windowElement)
        {
            if (windowElement.TryGetCurrentPattern(WindowPattern.Pattern, out object pattern))
            {
                WindowPattern windowPattern = (WindowPattern)pattern;
                try
                {
                    // 将窗体状态设置为正常，从而激活窗体
                    windowPattern.SetWindowVisualState(WindowVisualState.Normal);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Info($"激活窗体时发生错误: {ex.Message}");
                }
            }

            try
            {
                // 获取窗口句柄
                object nativeWindowHandle = windowElement.GetCurrentPropertyValue(AutomationElement.NativeWindowHandleProperty);
                IntPtr hWnd = (IntPtr)nativeWindowHandle;
                Console.WriteLine($"窗口句柄: {hWnd}");

                // 这里可以根据句柄进行其他操作，例如激活窗口
                SetForegroundWindow(hWnd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取窗口句柄时出错: {ex.Message}");
            }

            return false;
        }


        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
