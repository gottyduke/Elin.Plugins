using System.Linq;
using System.Reflection;

namespace Cwl.Helper;

public static class IntrospectCopy
{
    extension<T>(T source) where T : notnull, new()
    {
        public void IntrospectCopyTo<TU>(TU target, BindingFlags? flags = null) where TU : notnull
        {
            var srcType = source.GetType();
            var srcFields = srcType.GetCachedFields();
            var dstType = target.GetType();

            foreach (var dest in dstType.GetCachedFields()) {
                var field = srcFields.FirstOrDefault(f => f.Name == dest.Name &&
                                                          f.FieldType == dest.FieldType);
                if (field is null) {
                    continue;
                }

                dest.SetValue(target, field.GetValue(source));
            }
        }

        public T GetIntrospectCopy()
        {
            T val = new();
            source.IntrospectCopyTo(val);
            return val;
        }
    }
}