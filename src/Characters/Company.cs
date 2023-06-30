using CountryData;
using Newtonsoft.Json;
using Seahaven.Database;
using Seahaven.Interfaces;
using System.CommandLine;

namespace Seahaven.Characters
{
    public class Company : IPromptDescription
    {
        public string Name { get; set; }
        public int EmployeesTotal { get; set; }
        public string Industry { get; set; }
        public IList<string> OperatingLocations { get; set; } = new List<string>();

        public IList<Product> Products { get; set; } = new List<Product>();

        public IList<Unit> Units { get; set; } = new List<Unit>();

        public virtual string Prompt => $"{Name} is a company with {EmployeesTotal} employees that operates in the {Industry} industry. It operates out of countries {string.Join(",", OperatingLocations)}.";

        public string Domain { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            return (Name + EmployeesTotal + Industry).GetHashCode();
        }

        private class IntCompanyRep
        {
            public string Name { get; set; }
            public int EmployeesTotal { get; set; }
            public string Industry { get; set; }
            public IList<string> OperatingLocations { get; set; }
            public IList<string> BusinessUnits { get; set; }
            public string DomainName { get; set; }
        }

        public static async Task<Company> Create(IPromptBasedGenerator generator, string location)
        {
            var promptBase = @"
Generate a brand new business which is operated out of the {0}. Your output must be JSON encoded
Output JSON must only have 5 keys named Name, EmployeesTotal, Industry, BusinessUnits OperatingLocations and DomainName.
OperatingLocations is a list of countries where the company operates.
Industry is the type of industry the company is aligned to.
BusinessUnits is a list of internal subdivisions the company has, this should include core business functions as well as organisations that may be related to the company's work. The number of organisations must reflect the size of the business.
OperatingLocations locations should be correct for the industry. The key EmployeesTotal is the total number of employees their business has.
DomainName should be a possible domain name the business could be using";

            var prompt = string.Format(promptBase, location);

            var internalRep = await generator.DeserializeResponseFromJson<IntCompanyRep>(prompt);

            return new Company()
            {
                Name = internalRep.Name,
                EmployeesTotal = internalRep.EmployeesTotal,
                Industry = internalRep.Industry,
                OperatingLocations = internalRep.OperatingLocations,
                Units = internalRep.BusinessUnits.Select(bu => new Unit(bu)).ToList(),
                Domain = internalRep.DomainName
            };
        }

        public static void ProvidesCommands(IDictionary<string, Command> commands, IDictionary<string, Option> options, IPromptBasedGenerator generator, IRandomBasedGenerator random)
        {
            var newCompanyCommand = new Command("company", "Generate a new company/business.")
            {
                options["name"],
                options["location"],
                options["multiply"],
                options["fast"]
            };
            commands["new"].AddCommand(newCompanyCommand);

            newCompanyCommand.SetHandler(async (nameOpt, locationOpt, multiplyFactor, fast) =>
            {
                for (int x = 0; x < multiplyFactor; x++)
                {
                    var location = locationOpt;

                    if (string.IsNullOrWhiteSpace(location))
                    {
                        var allCountryInfo = CountryLoader.CountryInfo;
                        // get a random country with a populations over 10m
                        var suitableCountries = allCountryInfo.Where(x => x.Population >= 20 * 1000000);
                        location = suitableCountries.ElementAt(random.Next(0, suitableCountries.Count())).Name;
                    }

                    // create a new company
                    var company = await Company.Create(generator, location);

                    if (!string.IsNullOrWhiteSpace(nameOpt))
                    {
                        company.Name = nameOpt;
                    }

                    InMemoryDatabase.AddToDatabase(company);
                }
            }, options["name"] as Option<string>, options["location"] as Option<string>, options["multiply"] as Option<uint>, options["fast"] as Option<bool>);


            // show company command

            var showCompanyCommand = new Command("company", "Display JSON document defining a generated company/business.")
            {
                options["name"],
                options["id"],
            };
            commands["show"].AddCommand(showCompanyCommand);

            showCompanyCommand.SetHandler((nameOpt, idOpt) =>
            {
                List<Company> results = new();

                if (!string.IsNullOrWhiteSpace(nameOpt))
                {
                    nameOpt = nameOpt.ToLower();

                    results.AddRange(InMemoryDatabase.Get<Company>().Where(x => x.Name.ToLower().Contains(nameOpt)));
                }
                else
                {
                    var company = idOpt.Get<Company>();
                    results.Add(company);
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