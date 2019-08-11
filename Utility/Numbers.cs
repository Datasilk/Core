using System.Text;

namespace Utility
{
    public static class Numbers
    {
       public static string ToFixed(this int number, int length)
        {
            var result = new StringBuilder();
            var num = number.ToString();
            if(num.Length < length)
            {
                for(var x = num.Length; x < length; x++)
                {
                    result.Append("0");
                }
            }
            result.Append(num);
            return result.ToString();
        }
    }
}
