using Newtonsoft.Json;
using Seahaven.Characters;
using Seahaven.Database;
using Seahaven.Interfaces;
using System.CommandLine;

namespace Seahaven.Content
{
    public class Email : IPromptDescription
    {
        public string Subject { get; internal set; }
        public string Body { get; internal set; }

        public string FromAccount => From.EmailAddress;

        public string ToAccount => To.EmailAddress;

        [JsonIgnore]
        public Employee From { get; internal set; }

        [JsonIgnore]
        public Employee To { get; internal set; }

        public string Prompt => $"An email from {From.FirstName} {From.LastName} To {To.FirstName} {To.LastName}. the subject line is {Subject}";

        public string? Attachment { get; private set; }

        public override string ToString()
        {
            return $"{FromAccount} TO {ToAccount} - {Subject}";
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public async Task<Email> Reply(IPromptBasedGenerator generator, Employee from, Employee to)
        {
            var promptBase = $@"{to.FirstName} {to.LastName} has sent you an email. Write a short reply from {from.FirstName} {from.LastName}.
{from.Prompt}
Add a reasonable frequency of human errors, this could be spelling or grammatical errors, typos or incorrect/missing apostrophies.
The first line must be the email subject. The remainder the body.
The email you are replying to text follow: " + Subject + Environment.NewLine +Body;

            var resultStr = await generator.GetResponseAsString(promptBase);

            var lines = resultStr.Split("\n");

            var email = new Email()
            {
                Subject = CleanSubjectLine(lines.First()),
                Body = string.Join(Environment.NewLine, lines.Skip(1)),
                From = To,
                To = From,
            };

            From.SentMessages.Add(email);

            return email;
        }

        /// <summary>
        /// Creates an email between two employees in different business units refering to a product.
        /// </summary>
        /// <param name="employeeA">Employee sending email</param>
        /// <param name="businessUnitA">EmployeeA business unit</param>
        /// <param name="employeeB">Emplaying recieving email</param>
        /// <param name="businessUnitB">EmployeeB business unit</param>
        /// <param name="productToDiscuss">The product</param>
        /// <returns>Email</returns>
        public static async Task<Email> Create(
            IPromptBasedGenerator generator,
            Employee employeeA,
            Employee employeeB,
            IEnumerable<IPromptDescription> objectsToDiscuss,
            bool hasAttachment = false,
            string? additionalPrompt = null)
        {
            string companyPrompt = (employeeA.Company.Name == employeeB.Company.Name) ? employeeA.Company.Prompt : employeeA.Company.Prompt + employeeB.Company.Prompt;

            var internalOrExternal = employeeA.Company.Name == employeeB.Company.Name ? "internal" : "external";

            var prompt = $@"Write an {internalOrExternal} email from {employeeA.FirstName} {employeeA.LastName} to {employeeB.FirstName} {employeeB.LastName}.
{employeeA.Prompt}
{employeeB.Prompt}
{companyPrompt}
Sign off the email with the senders first name only.
Emails should be at most two paragraphs long. Internals email should be informal, external ones formal.
Don't mention a persons job title unless it's relevant to the email.
For internal emails add a reasonable frequency of human errors, this could be spelling or grammatical errors, typos or incorrect/missing apostrophies.
Internal emails could also include nicknames or briefly mention events outside of work.";

            foreach (var promptObject in objectsToDiscuss)
            {
                switch (promptObject)
                {
                    case Product product:
                        prompt += Environment.NewLine + $"The email must refer to a product called '{product.Name}'. The price of the product may not be important to the message. {product.Prompt}";
                        break;
                    case Employee employee:
                        prompt += Environment.NewLine + $"The email must refer to the employee named {employee.Prompt}";
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            if (!string.IsNullOrWhiteSpace(additionalPrompt))
            {
                prompt += Environment.NewLine + "The email topic should be about " + additionalPrompt;
            }

            if (hasAttachment)
            {
                prompt += Environment.NewLine + "The email must refer to an attached document. The first line must be the email subject. The second line must be the file name of the attachment in the format 'Attachment: <filename>'. The remainder must be the email body";
            }
            else
            {
                prompt += Environment.NewLine + "The first line must be the email subject. The remainder must be the email body";
            }

            var resultStr = await generator.GetResponseAsString(prompt);

            var lines = resultStr.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var email = new Email()
            {
                Subject = CleanSubjectLine(lines.First().Trim()),
                Body = string.Join(Environment.NewLine, lines.Skip(hasAttachment ? 2 : 1)).Trim(),
                Attachment = hasAttachment ? lines[1].Trim().Replace("Attachment: ", "") : null,
                From = employeeA,
                To = employeeB,
            };

            employeeA.SentMessages.Add(email);

            return email;
        }

        private static string CleanSubjectLine(string raw)
        {
            return raw.Replace("Subject :", "").Replace("Subject:", "").Trim();
        }

        public static void ProvidesCommands(IDictionary<string, Command> commands, IDictionary<string, Option> options, IPromptBasedGenerator generator, IRandomBasedGenerator random)
        {
            var newEmailCommand = new Command("email", "Generates a new email.")
            {
                options["id"],
                options["from"],
                options["to"],
                options["product"],
                options["attachment"],
                options["fast"],
                options["prompt"],
                options["employee"],
                options["multiply"],
            };
            commands["new"].AddCommand(newEmailCommand);

            newEmailCommand.SetHandler(async (id , fromId, toId, productId, hasAttachment, fast, prompt, employee) =>
            {
                uint multiply = 1;

                // So system.commandline currently only supports 8 args. This HACK gets around that by force parsing the raw cmdline
                try
                {
                    var parsedResult = (options["multiply"] as Option<uint>).Parse(CommandHandling.CommandLineRaw);
                    multiply = uint.Parse(parsedResult.CommandResult.Children.Single().Tokens.Single().Value);
                }
                catch
                {

                }

                for (int x = 0; x < 1; x++)
                {
                    List<IPromptDescription> objectsToReferToInTheEmail = new();

                    if (productId.WasProviderByUser)
                    {
                        objectsToReferToInTheEmail.Add(productId.Get<Product>());
                    }
                    if (employee.WasProviderByUser)
                    {
                        objectsToReferToInTheEmail.Add(employee.Get<Employee>());
                    }

                    Email email;
                    // first if we are given an explicit ID from the user then by definition we are replying to the email
                    // referenced by that ID
                    if (id.WasProviderByUser)
                    {
                        var oldEmail = id.Get<Email>();
                        email = await oldEmail.Reply(generator, oldEmail.To, oldEmail.From);
                    }
                    else
                    {
                        var from = fromId.Get<Employee>();
                        var to = toId.Get<Employee>();
                        email = await Create(generator, from, to, objectsToReferToInTheEmail, hasAttachment, prompt);
                    }

                    InMemoryDatabase.AddToDatabase(email);
                }
            }, 
            options["id"] as Option<IDValue>,
            options["from"] as Option<IDValue>,
            options["to"] as Option<IDValue>,
            options["product"] as Option<IDValue>,
            options["attachment"] as Option<bool>,
            options["fast"] as Option<bool>,
            options["prompt"] as Option<string>,
            options["employee"] as Option<IDValue>
            //options["multiply"] as Option<uint> - See HACK above
            );


            var showEmailCommand = new Command("email", "Display JSON document defining a generated email.")
            {
                options["id"],
            };
            commands["show"].AddCommand(showEmailCommand);

            showEmailCommand.SetHandler(async (idOpt) =>
            {
                var email = idOpt.Get<Email>();
                Console.WriteLine(JsonConvert.SerializeObject(email, Formatting.Indented).Replace("\\r\\n", Environment.NewLine));
                
            }, options["id"] as Option<IDValue>);
        }
    }
}
