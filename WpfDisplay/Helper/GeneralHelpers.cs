using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDisplay.Helper;

public static class GeneralHelpers
{
    /// <summary>
    /// ulong to string formatter extension
    /// </summary>
    public static string ToKMB(this ulong num)
    {
        if (num > 999999999)
        {
            return num.ToString("0,,,.000B", CultureInfo.InvariantCulture);
        }
        else
        if (num > 999999)
        {
            return num.ToString("0,,.00M", CultureInfo.InvariantCulture);
        }
        else
        if (num > 999)
        {
            return num.ToString("0,.0K", CultureInfo.InvariantCulture);
        }
        else
        {
            return num.ToString(CultureInfo.InvariantCulture);
        }
    }
}
