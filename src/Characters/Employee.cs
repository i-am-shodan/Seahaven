using Bogus;
using Bogus.DataSets;
using Newtonsoft.Json;
using Seahaven.Content;
using Seahaven.Database;
using Seahaven.Interfaces;
using System.CommandLine;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Seahaven.Characters
{
    public class Employee : Person
    {
        public string Role { get; set; }

        [JsonIgnore]
        public Unit BusinessUnit { get; set; }

        [JsonIgnore]
        public Company Company { get; set; }

        public List<Email> SentMessages { get; set; } = new List<Email>();

        public override string ToString()
        {
            return $"{base.FirstName} {base.LastName} - {Role}";
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        private class Request
        {
            public string Salary { get; set; }
            public DateTime StartDate { get; set; }

            public string Role { get; set; }
        }

        public override string Prompt => base.Prompt + $" As background they work as a {Role} at {Company.Name} in the {BusinessUnit.Name} department.";

        [JsonIgnore]
        public string EmailAddress => $"{FirstName.ToLowerInvariant()}.{LastName.ToLowerInvariant()}@{Company.Domain}";

        public Employee()
        {

        }

        public Employee(Person p, Company c, string salary, string role, Unit bu)
        {
            this.FirstName = p.FirstName;
            this.LastName = p.LastName;
            this.Age = p.Age;
            this.NumberOfChildren = p.NumberOfChildren;
            this.Personality = p.Personality;
            this.Location = p.Location;
            this.Role = role;
            this.Company = c;
            this.BusinessUnit = bu;
        }

        public static async Task<Employee> Create(Company company, string location, Unit businessUnit)
        {
            var person = await Person.Create(location);

            var faker = new Faker();
            var title = faker.Name.JobTitle();

            var employee = new Employee(person, company, "", title, businessUnit);
            businessUnit.Employees.Add(employee);

            return employee;
        }

        public static async Task<Employee> Create(IPromptBasedGenerator generator, Company company, string location, Unit businessUnit)
        {
            var person = await Person.Create(generator, location);

            var promptBase = $@"
You must generate fictional employment data for an employeee. The employee is named {person.FirstName} {person.LastName}, they work in {businessUnit.Name}.
{person.Prompt}
The JSON must have only two keys Salary and Role.
Their salary should be realistic to their location, role, gender and employer sector.
Salary should be a string in the format <AMOUNT> <ISO CURRENCY> example 84000 GBP.";

            var prompt = string.Format(promptBase, person.FirstName, person.LastName, businessUnit, person.Age, person.NumberOfChildren, company.Name, company.Industry, company.OperatingLocations.First());

            var req = await generator.DeserializeResponseFromJson<Request>(prompt);

            var employee = new Employee(person, company, req.Salary, req.Role, businessUnit);
            businessUnit.Employees.Add(employee);

            return employee;
        }

        public static void ProvidesCommands(IDictionary<string, Command> commands, IDictionary<string, Option> options, IPromptBasedGenerator generator, IRandomBasedGenerator random)
        {
            var newEmployeeCommand = new Command("employee", "Generates a new employee for a company (given by id). Name and Unit is randomly selected if not given.")
            {
                options["name"],
                options["unit"],
                options["location"],
                options["id"],
                options["multiply"],
                options["fast"]
            };
            commands["new"].AddCommand(newEmployeeCommand);

            newEmployeeCommand.SetHandler(async (nameOpt, unitOpt, locOpt, idOpt, multiplyFactor, fast) =>
            {
                for (int x = 0; x < multiplyFactor; x++)
                {
                    var company = idOpt.Get<Company>();

                    // use any provided location of randomly pick one from the company OperatingLocations
                    var employeeLocation = !string.IsNullOrWhiteSpace(locOpt) ? locOpt : company.OperatingLocations.OrderBy(x => random.Next()).First();

                    // use any provided unit or randomly pick one from the company Units
                    var businessUnit = !string.IsNullOrWhiteSpace(unitOpt) ? company.Units.Single(x => x.Name == unitOpt) : company.Units.OrderBy(x => random.Next()).First();
                    
                    var employee = fast ? await Employee.Create(company, employeeLocation, businessUnit) : await Employee.Create(generator, company, employeeLocation, businessUnit);

                    if (!string.IsNullOrWhiteSpace(nameOpt))
                    {
                        var split = nameOpt.Split(" ");

                        employee.FirstName = split.First();
                        employee.LastName = split.Last();
                    }

                    InMemoryDatabase.AddToDatabase(employee);
                }
            }, options["name"] as Option<string>, options["unit"] as Option<string>, options["location"] as Option<string>, options["id"] as Option<IDValue>, options["multiply"] as Option<uint>, options["fast"] as Option<bool>);


            var showEmployeeCommand = new Command("employee", "Display JSON document defining a generated employee.")
            {
                options["name"],
                options["id"],
            };
            commands["show"].AddCommand(showEmployeeCommand);

            showEmployeeCommand.SetHandler(async (nameOpt, idOpt) =>
            {
                List<Employee> results = new();

                if (!string.IsNullOrWhiteSpace(nameOpt))
                {
                    nameOpt = nameOpt.ToLower();

                    results.AddRange(InMemoryDatabase.Get<Employee>().Where(x => x.FirstName.ToLower().Contains(nameOpt) || x.LastName.ToLower().Contains(nameOpt) ));
                }
                else
                {
                    var employee = idOpt.Get<Employee>();
                    results.Add(employee);
                }

                if (!results.Any())
                {
                    Console.WriteLine($"Error: not entry was found");
                }

                foreach (var c in results)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(c, Formatting.Indented));
                }
            }, options["name"] as Option<string>, options["id"] as Option<IDValue>);
        }
    }
}
