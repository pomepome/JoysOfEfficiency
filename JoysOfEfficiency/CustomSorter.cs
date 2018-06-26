using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoysOfEfficiency
{
    internal class CustomSorter : IComparer<KeyValuePair<int,double>>
    {
        public int Compare(KeyValuePair<int, double> x, KeyValuePair<int, double> y)
        {
            if (Math.Abs(x.Value - y.Value) < 0.001)
            {
                return x.Key - y.Key;
            }
            return x.Value - y.Value > 0 ? 1 : -1;
        }
    }
}
