using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActivityWatchVS.Tools
{
    public static class ExceptionExtensions
    {
        public static T GetInnerst<T>(this Exception ex)
        {
            T innerstEx = default(T);

            do
            {
                // do we have a exception with the expected type?
                if (typeof(T).IsAssignableFrom(ex.GetType()))
                {
                    //save the innerst
                    innerstEx = (T)Convert.ChangeType(ex, typeof(T));
                }
                // go down
                ex = ex.InnerException;
            } while (ex != null);

            return innerstEx;
        }
    }
}
