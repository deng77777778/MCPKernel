using System.Reflection;

namespace MCP.AI
{
    /// <summary>
    /// 快速属性访问扩展
    /// </summary>
    public static class PropertyExtensions
    {
        public static object GetValueFast(this PropertyInfo property, object target)
        {
            return PropertyAccessor.GetGetter(property)(target);
        }

        public static void SetValueFast(this PropertyInfo property, object target, object value)
        {
            PropertyAccessor.GetSetter(property)(target, value);
        }

        public static object GetValueFast(this FieldInfo field, object target)
        {
            return PropertyAccessor.GetFieldGetter(field)(target);
        }

        public static void SetValueFast(this FieldInfo field, object target, object value)
        {
            PropertyAccessor.GetFieldSetter(field)(target, value);
        }
    }
}