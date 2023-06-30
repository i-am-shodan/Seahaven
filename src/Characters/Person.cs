using Bogus;
using CountryData;
using Seahaven.Interfaces;
using System.Globalization;
using System.Runtime.Serialization;

namespace Seahaven.Characters
{
    public class Person : IPromptDescription
    {
        [DataContract(Name = "CarCondition")]
        public enum MBTI // Myers-Briggs Type Indicator 
        {
            ISTJ,
            ISFJ,
            INFJ,
            INTJ,
            ISTP,
            ISFP,
            INFP,
            INTP,
            ESTP,
            ESFP,
            ENFP,
            ENTP,
            ESTJ,
            ESFJ,
            ENFJ,
            ENTJ
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Location { get; set; }

        public int Age { get; set; }
        public int NumberOfChildren { get; set; }
        public string Personality { get; set; }

        public virtual string Prompt => $"{FirstName} {LastName} is a {Age} year old who lives in {Location}. {FirstName}'s Myers-Briggs personality is {Personality}, any text written on their behalf must reflect those traits. Do not state their personality type or Myers-Briggs in the text.";

        public static async Task<Person> Create(IPromptBasedGenerator generator, string location)
        {
            var promptBase = @"
Generate personal details for a story about an individual who resides in {0}.
Names must not look fictional or be based on famous people.
Use the most popular names in that region but avoid obviously dummy names like John Smith.
Their Personality type should reference their age, gender and location. It should be a list of keywords that describe how they come across. 
The JSON must have keys, FirstName, LastName, Age, Personality and NumberOfChildren. Age must be over 18. Personality must be a 4 character Myers-Briggs Type Indicator.";

            var prompt = string.Format(promptBase, location);

            var person = await generator.DeserializeResponseFromJson<Person>(prompt);
            person.Location = location;
            return person;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Task<Person> Create(string location)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var allCountryInfo = CountryLoader.CountryInfo;
            // get a random country with a populations over 10m
            var suitableCountries = allCountryInfo.Where(x => x.Name.ToLowerInvariant().Contains(location) || x.Iso.ToLowerInvariant() == location);
            var locale = "en";

            if (suitableCountries.Any())
            {
                var matchingCultures = CultureInfo.GetCultures(CultureTypes.AllCultures).Where(x => x.EnglishName.ToLowerInvariant().Contains(suitableCountries.First().Languages.First().ToLowerInvariant()));

                if (matchingCultures.Any())
                {
                    var internalLocale = matchingCultures.First();
                    if (internalLocale.Parent != null)
                    {
                        internalLocale = internalLocale.Parent;
                    }

                    locale = internalLocale.TwoLetterISOLanguageName.ToLowerInvariant();
                }
            }

            var faker = new Faker<Person>(locale)

            .RuleFor(property: u => u.FirstName, setter: (f, u) => f.Name.FirstName())
            .RuleFor(property: u => u.LastName, setter: (f, u) => f.Name.LastName())
            .RuleFor(property: u => u.Age, setter: (f, u) => f.Random.Int(18, 62))
            .RuleFor(property: u => u.NumberOfChildren, setter: (f, u) => f.Random.Int(0, 3))
            .RuleFor(property: u => u.Personality, setter: (f, u) =>  f.PickRandom<MBTI>().ToString());

            var targetInstance = faker.Generate();

            if (targetInstance == null)
            {
                throw new Exception();
            }

            targetInstance.Location = location;

            return targetInstance;
        }
    }
}