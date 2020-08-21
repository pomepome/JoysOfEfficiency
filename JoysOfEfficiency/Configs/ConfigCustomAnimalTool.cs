using System.Collections.Generic;
using JoysOfEfficiency.Core;

namespace JoysOfEfficiency.Configs
{
    class ConfigCustomAnimalTool
    {

        public List<CustomAnimalTool> CustomAnimalTools { get; set; }= new List<CustomAnimalTool>();

        public void AddCustomTool(string name, ToolType type)
        {
            if (CustomAnimalTools.Exists(c => c.Name == name))
            {
                return;
            }
            CustomAnimalTools.Add(new CustomAnimalTool(name, type));
            InstanceHolder.CustomAnimalTool.Save();
        }

        public void RemoveCustomTool(string name)
        {
            if (!CustomAnimalTools.Exists(c => c.Name == name))
            {
                return;
            }

            CustomAnimalTools.RemoveAll(c => c.Name == name);
            InstanceHolder.CustomAnimalTool.Save();
        }

        public bool Contains(string name)
        {
            return CustomAnimalTools.Exists(c => c.Name == name);
        }
    }
}
