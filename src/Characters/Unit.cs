using Seahaven.Interfaces;

namespace Seahaven.Characters
{
    public class Unit : IPromptDescription
    {
        public string Name { get; set; }

        public List<Employee> Employees { get; set; } = new List<Employee>();

        public string Prompt => $"The {Name} business unit has {Employees.Count} employees.";

        public Unit(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name}";
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
