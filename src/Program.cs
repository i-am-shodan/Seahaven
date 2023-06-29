using Newtonsoft.Json;
using Seahaven;
using Seahaven.Characters;
using Seahaven.Content;
using Seahaven.Database;
using Seahaven.Generation;
using Seahaven.Interfaces;
using System.CommandLine;
using System.CommandLine.Parsing;
using File = System.IO.File;

IPromptBasedGenerator generator = new AzureOpenAIPromptGenerator();
IRandomBasedGenerator random = new InternalRandomGenerator();

var rootCommand = new RootCommand("Seahaven");

ParseArgument<IDValue> x = (argument) => {

    if (!argument.Tokens.Any())
    {
        return new IDValue(IDValue.IDType.LastMatching, false);
    }

    var token = argument.Tokens.Single();

    if (token.Value == "?")
    {
        return new IDValue(IDValue.IDType.Random, true);
    }
    else if (ulong.TryParse(token.Value, out var value))
    {
        return new IDValue(IDValue.IDType.Value, true, value);
    }
    return new IDValue(IDValue.IDType.LastMatching, true);
};

var options = new Dictionary<string, Option>()
{
    { "name", new Option<string>("name=", "The name of an object. Usee double quotes to include spaces eg Firstname Lastname") { IsRequired = false } },
    { "location", new Option<string>("location=", "The location of an object, if omitted a random value may be used instead.") { IsRequired = false } },
    { "id", new Option<IDValue>("id=", x, true, "The ID of an object.") { IsRequired = false } },
    { "unit", new Option<string>("unit=", "The name of the unit.") { IsRequired = false }},
    { "from", new Option<IDValue>("from=", x, true, "The employee ID.") { IsRequired = false }},
    { "to", new Option<IDValue>("to=", x, true, "The employee ID.") { IsRequired = false }},
    { "product", new Option<IDValue>("product=", x, true, "The product ID.") { IsRequired = false }},
    { "attachment", new Option<bool>("attachment=", "Whether the email should have an attachment.") { IsRequired = false }},
    { "file", new Option<string>("file=", "The filename to write to.") { IsRequired = true }},
    { "multiply", new Option<uint>("multiply=", () => 1, "The number of times to repeat an operation.") { IsRequired = false }},
    { "fast", new Option<bool>("fast=", () => false, "Use local dummy data generation, not GPT.") { IsRequired = false }},
    { "prompt", new Option<string>("prompt=", "A prompt to use in AI text generation.") { IsRequired = false }},
    { "employee", new Option<IDValue>("employee=", x, true, "The ID of an employee to refer to.") { IsRequired = false }},
    { "deployment", new Option<string>("deployment=", "The name of the model to use.") { IsRequired = false }},
    { "key", new Option<string>("key=", "The auth key to use (default is to use your user creds).") { IsRequired = false }},
    { "uri", new Option<string>("uri=", "The uri to connect to.") { IsRequired = false }},
};

var commands = new Dictionary<string, Command>()
{
    { "new", new Command("new", "Create a new piece of information/object.") },
    { "show", new Command("show", "Displays a JSON representation of the data held on an object.") { options["id"] } },
    { "use", new Command("use", "Provides a value to be used in further requests.")},
    { "save", new Command("save", "Saves the current objects to a file."){ options["file"] } },
    { "load", new Command("load", "Loads saved objects to a file."){ options["file"] } },
    { "script", new Command("script", "Runs a script."){ options["file"] } },
    { "set", new Command("set", "Sets a number of configuration options."){ options["deployment"], options["key"], options["uri"] } },
};

commands["set"].SetHandler((deployment, key, uri) =>
{
    if (!string.IsNullOrWhiteSpace(deployment))
    {
        generator.SetDeployment(deployment.Trim('"'));
    }
    if (!string.IsNullOrWhiteSpace(key))
    {
        generator.SetKey(key.Trim('"'));
    }
    if (!string.IsNullOrWhiteSpace(uri))
    {
        generator.SetURI(uri.Trim('"'));
    }
}, options["deployment"] as Option<string>, options["key"] as Option<string>, options["uri"] as Option<string>);

commands["show"].SetHandler((idOpt) =>
{
    var o = idOpt.Get<object>();
    Console.WriteLine(JsonConvert.SerializeObject(o, Formatting.Indented));

}, options["id"] as Option<IDValue>);

commands["use"].SetHandler((idOpt) =>
{
    InMemoryDatabase.SetCurrentId(idOpt.Value);

    Console.WriteLine($"Using ID {idOpt.Value}");

}, options["id"] as Option<IDValue>);

commands["save"].SetHandler((fileOpts) =>
{
    var raw = JsonConvert.SerializeObject(InMemoryDatabase.Get<Company>(), Formatting.Indented);
    File.WriteAllText(fileOpts, raw);
}, options["file"] as Option<string>);

commands["load"].SetHandler(async (fileOpts) =>
{
    var listOfCompanies = JsonConvert.DeserializeObject<List<Company>>(await File.ReadAllTextAsync(fileOpts));

    foreach (var company in listOfCompanies)
    {
        InMemoryDatabase.AddToDatabase(company);

        foreach (var product in company.Products)
        {
            product.Company = company;
            InMemoryDatabase.AddToDatabase(product);
        }

        foreach (var bu in company.Units)
        {
            foreach (var employee in bu.Employees)
            {
                employee.BusinessUnit = bu;
                employee.Company = company;

                InMemoryDatabase.AddToDatabase(employee);

                foreach (var email in employee.SentMessages)
                {
                    InMemoryDatabase.AddToDatabase(email);
                }
            }
        }
    }

}, options["file"] as Option<string>);

commands["script"].SetHandler(async (fileOpts) =>
{
    if (File.Exists(fileOpts))
    {
        var lines = await File.ReadAllLinesAsync(fileOpts);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                continue;
            }

            await CommandHandling.Handle(rootCommand, line);
        }
    }
    else
    {
        Console.WriteLine("Error: Could not load file: "+fileOpts);
    }

}, options["file"] as Option<string>);

Company.ProvidesCommands(commands, options, generator, random);
Product.ProvidesCommands(commands, options, generator, random);
Employee.ProvidesCommands(commands, options, generator, random);
Email.ProvidesCommands(commands, options, generator, random);

rootCommand.AddCommand(commands["new"]);
rootCommand.AddCommand(commands["show"]);
rootCommand.AddCommand(commands["use"]);
rootCommand.AddCommand(commands["save"]);
rootCommand.AddCommand(commands["load"]);
rootCommand.AddCommand(commands["script"]);
rootCommand.AddCommand(commands["set"]);

await CommandHandling.Handle(rootCommand);