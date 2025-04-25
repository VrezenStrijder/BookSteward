using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BookSteward.Utility
{
    public static class UIElementExtensions
    {
        /// <summary>
        /// 尝试查找指定类型的父元素
        /// </summary>
        public static T? TryFindParent<T>(this DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;
            else
                return TryFindParent<T>(parentObject);
        }

        /// <summary>
        /// 查找指定类型的子元素
        /// </summary>
        public static T? FindDescendant<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            T? foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t)
                {
                    foundChild = t;
                    break;
                }

                foundChild = FindDescendant<T>(child);

                if (foundChild != null) break;
            }

            return foundChild;
        }

        /// <summary>
        /// 查找视觉树中所有指定类型的子元素
        /// </summary>
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t)
                {
                    yield return t;
                }

                foreach (var childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }


}
