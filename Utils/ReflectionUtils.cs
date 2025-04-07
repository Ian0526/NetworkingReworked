using HarmonyLib;
using System.Reflection;
using System.Text;
using System;

public static class ReflectionUtils
{
    public static string DumpObject(object obj, int depth = 0, int maxDepth = 1)
    {
        if (obj == null)
            return "null";
        Type type = obj.GetType();
        if (depth > maxDepth || type.IsPrimitive || type == typeof(string) || type.IsEnum)
            return obj.ToString();
        StringBuilder sb = new StringBuilder();
        string indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}{type.Name} {{");
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (FieldInfo field in fields)
        {
            object fieldValue;
            try { fieldValue = field.GetValue(obj); }
            catch (Exception ex) { fieldValue = $"Exception: {ex.Message}"; }
            sb.Append($"{indent}  {field.Name}: ");
            if (fieldValue == null)
                sb.AppendLine("null");
            else if (field.FieldType.IsPrimitive || field.FieldType == typeof(string) || field.FieldType.IsEnum)
                sb.AppendLine(fieldValue.ToString());
            else
                sb.AppendLine(DumpObject(fieldValue, depth + 1, maxDepth));
        }
        sb.AppendLine($"{indent}}}");
        return sb.ToString();
    }

    public static T TryGetField<T>(Traverse t, string name) where T : class
    {
        var field = t.Field(name);
        if (field != null && field.FieldExists())
        {
            var val = field.GetValue<T>();
            return val;
        }
        return null;
    }

    public static T TryGetStructField<T>(Traverse t, string name) where T : struct
    {
        var field = t.Field(name);
        if (field != null && field.FieldExists())
        {
            return field.GetValue<T>();
        }
        return default;
    }
}