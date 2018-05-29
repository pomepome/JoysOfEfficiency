using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoysOfEfficiency.OptionsElements
{
    public class MenuTab
    {
        public int Count { get { return optionsElements.Count; } }

        private readonly List<OptionsElement> optionsElements = new List<OptionsElement>();

        public void AddOptionsElement(OptionsElement element)
        {
            optionsElements.Add(element);
        }

        public List<OptionsElement> GetElements()
        {
            return new List<OptionsElement>(optionsElements);
        }
    }
}
