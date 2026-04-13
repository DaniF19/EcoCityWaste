using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EcoCityWaste.Helpers
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            var member = enumValue
                .GetType()
                .GetMember(enumValue.ToString())
                .FirstOrDefault();

            if (member == null)
                return enumValue.ToString();

            var displayAttr = member.GetCustomAttribute<DisplayAttribute>();

            return displayAttr?.GetName() ?? enumValue.ToString();
        }
    }
}