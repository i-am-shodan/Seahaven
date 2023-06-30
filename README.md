# Seahaven
Seahaven is a command-line tool written in C# that generates internally consistent test data. This is useful if you are trying to create environments that humans will interact with and require data that needs to pass a human sanity check. Other tools, like Bogus, exist that can generate lots of test data but this data often based firmly in fictional. Seahaven utilizes a combination of GPT models hosted on either Azure or OpenAI as well as Bogus to ensure the generated data is not only realistic but also contextually meaningful.

The name of the project, pays homage to the town featured in the film "The Truman Show." Just like the meticulously crafted world in the movie, Seahaven aims to generate test data that mirrors the authenticity and coherence found in a real human environment. By harnessing the power of GPT models, Seahaven creates h data that feels as genuine and interconnected as the fabricated reality Truman unwittingly inhabited.

## Features
* Generate test data that is internally consistent and human-readable.
* Utilize GPT models hosted on Azure or OpenAI for generating data.
* Easy-to-use command-line, REPL or script interface.
* Can generate various data types such as names, addresses, phone numbers, emails, and more.

## Getting Started
### Getting Seahaven
Go to the releases page and download the latest release for your OS. It doesn't matter if you don't have the dotnet installed.

### Usage
To generate test data using Seahaven, follow these steps:

* Open a terminal or command prompt.
* Navigate to the Seahaven binary directory.
* Run the Seahaven executable with no options to enter REPL mode

### Seahaven commands

The following commands can either be used at the command like directly or perhaps more usefully combined into a script. Here is an example script, you can run it with the `script file=<THEFILE>` command or type each command in directly.

```
# Your Azure/OpenAI model. You need to get this from the OpenAI dev page or the Azure portal
set deployment= "gpt-3.5-turbo-16k"
# To use OpenAI you just set the key
#set key= "YOUR_KEY_HERE"
# To use Azure OpenAI, Set either:
# * Just the URI. In this case your logged in user creds will be used for auth
# * Set the key and the URI for key based auth
#set uri= "https://YOURENV.openai.azure.com/"

# Now generate a company, 20 products for this company and 50 employees
# The company will be generated with GPT but the products and employees will use the bogus library
new company
new product fast= true multiply= 20
new employee fast= true multiply= 50

# Generate an email from employee 40 to employee 41 about employee 42
new email from= 40 to= 41 employee= 42
# We can now generate a reply to that message
new email id= 72
# You can use show with email, product, employee etc to see the raw data JSON
# by default it will use the last matching id
show email

# Generate some emails, we can't generate these quickly. We need to use GPT
# Here we are using ? to pick a random employee
new email from= ? to= ? product= ? multiply= 1
new email from= ? to= ? employee= ? multiply= 1
new email from= ? to= ? prompt= "an exciting corporate event"
new email from= ? to= ? prompt= "juicy gossip about another employee" multiply= 1
new email from= ? to= ? prompt= "their favourite TV show" multiply= 1
new email from= ? to= ? prompt= "corporate news" multiply= 1
new email from= ? to= ? prompt= "non adherence to company values" multiply= 1
new email from= ? to= ? prompt= "next financial year projects" multiply= 1
new email from= ? to= ? prompt= "a security indcident" multiply= 1
new email from= ? to= ? prompt= "business software being too expensive" multiply= 1
new email from= ? to= ? prompt= "not adhering to the expenses policy" multiply= 1

# Save you output to a file, it can be loaded with load
save file= yourfile.json

# if you just want to dump the output to the terminal you can put
show company
```

To get a full list of commands use 
```
Seahaven -h
```

### Building Seahaven from source
#### Prerequisites
To run Seahaven, you need to have the following installed on your system:
* .NET 6 SDK

#### Installation
```
git clone https://github.com/your-username/Seahaven.git
cd Seahaven
dotnet build
```

#### Contributing
Contributions to Seahaven are welcome! If you'd like to contribute to this project, please follow these steps:

* Fork the repository.
* Create a new branch for your feature or bug fix.
* Make your changes and commit them with descriptive messages.
* Push your changes to your fork.
* Submit a pull request to the main repository.

## License
This project is licensed under the MIT License. See the LICENSE file for details.