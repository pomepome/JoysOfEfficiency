using JoysOfEfficiency.Configs;

namespace JoysOfEfficiency.Utils
{
    internal class CustomAnimalConfigHolder : ConfigHolder<ConfigCustomAnimalTool>
    {
        public CustomAnimalConfigHolder(string filePath) : base(filePath)
        {
        }

        protected override ConfigCustomAnimalTool GetNewInstance()
        {
            return new ConfigCustomAnimalTool();
        }
    }
}
