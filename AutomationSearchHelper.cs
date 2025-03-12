
using System.Windows.Automation;

namespace stock_tool
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
        /// 获取指定父元素下的所有子窗口
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <returns>子窗口元素列表</returns>
        public static List<AutomationElement> GetChildWindows(AutomationElement parent)
        {
            // 假设窗口元素的 ControlType 为 Window
            Condition windowCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
            return FindElements(parent, windowCondition);
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
            return GetFirstChildWindow(parent, condition);
        }

    }
}
