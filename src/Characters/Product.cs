using Bogus;
using Newtonsoft.Json;
using Seahaven.Database;
using Seahaven.Interfaces;
using System.CommandLine;
using System.Data;

namespace Seahaven.Characters
{
    public class Product : IPromptDescription
    {
        public string Name { get; set; }
        public string Price { get; set; }

        [JsonIgnore]
        public Company Company { get; set; }

        public string Prompt => $"{Name} is a product created by {Company.Name} priced at {Price}.";

        public override string ToString()
        {
            return $"{Name}";
        }

        public override int GetHashCode()
        {
            return (Name+Price).GetHashCode();
        }

        public static async Task<Product> Create(IPromptBasedGenerator generator, Company company)
        {
            var promptBase = @"
Create a realistic product that could be on sale today from a company called {0} based in the {1} that operates in the {2} industry.
The JSON must only three keys, Name, Description and Price. These keys refer to the name of the product, a single line description of the product and its unit price.
The value for the Price field must be a string in the format '<AMOUNT> <ISO CURRENCY>' example 84000 GBP.
The company name should not be included in either the product or the description";

            var prompt = string.Format(promptBase, company.Name, company.OperatingLocations.First(), company.Industry);

            var p = await generator.DeserializeResponseFromJson<Product>(prompt);
            p.Company = company;

            company.Products.Add(p);

            return p; ;
        }

        public static async Task<Product> Create(Company company)
        {
            var faker = new Faker<Product>()           
                .RuleFor(property: u => u.Name, setter: (f, u) => f.Commerce.ProductName())
                .RuleFor(property: u => u.Price, setter: (f, u) => f.Commerce.Price());

            var targetInstance = faker.Generate();

            if (targetInstance == null)
            {
                throw new Exception();
            }

            company.Products.Add(targetInstance);
            targetInstance.Company = company;

            return targetInstance;
        }

        public static void ProvidesCommands(IDictionary<string, Command> commands, IDictionary<string, Option> options, IPromptBasedGenerator generator, IRandomBasedGenerator random)
        {
            var newProductCommand = new Command("product", "Generates a product a company will produce.")
            {
                options["name"],
                options["id"],
                options["multiply"],
                options["fast"]
            };
            commands["new"].AddCommand(newProductCommand);

            newProductCommand.SetHandler(async (nameOpt, idOpt, multiplyFactor, fast) =>
            {
                for (int x = 0; x < multiplyFactor; x++)
                {
                    var company = idOpt.Get<Company>();
                    var product = fast ? await Product.Create(company) : await Product.Create(generator, company);

                    if (!string.IsNullOrWhiteSpace(nameOpt))
                    {
                        product.Name = nameOpt;
                    }

                    InMemoryDatabase.AddToDatabase(product);
                }
            }, options["name"] as Option<string>, options["id"] as Option<IDValue>, options["multiply"] as Option<uint>, options["fast"] as Option<bool>);

            // show product

            var showProductCommand = new Command("product", "Display JSON document defining a generated product.")
            {
                options["id"],
            };
            commands["show"].AddCommand(showProductCommand);

            showProductCommand.SetHandler(async (idOpt) =>
            {
                var p = idOpt.Get<Product>();

                Console.WriteLine(JsonConvert.SerializeObject(p, Formatting.Indented));
            }, options["id"] as Option<IDValue>);
        }
    }
}
