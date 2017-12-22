# Umbraco DictionaryHelper

All dictionary items are fetched and inserted into concurrency dictionary cache on start up and will be accessible with an extension on Umbraco helper.

Added value: Dictionary items can be created automaticly under specific parent key or by using namespacing to create a tree.

### Available on Nuget
[Click here to go to nuget project page](https://www.nuget.org/packages/Vettvangur.DictionaryHelper/)

#### Example of usage with Umbraco helper:

```
@using DictionaryHelper;

@Umbraco.DictionaryValue("key") //Fetches the value of the dictionary item. Returns empty string if not found.
@Umbraco.DictionaryValue("key", "Default value") //Fetches the value of the dictionary item. Returns the default value if value is empty or key not found.
@Umbraco.DictionaryValue("key", "Default value", true) //Fetches the value of the dictionary item. Returns the default value if value is empty or key not found and will create the dictionary item with the default value if the key is not found.
@Umbraco.DictionaryValue("key", "Default value", "parentKey", true) // Creates the dictionary key if not found as a child on the parent key.
@Umbraco.DictionaryValue("root.child.key", "Default value", null, true) // Checks if any of the keys are existing and creates them if not found. With this approach the developer does not have to create the tree in the backoffice and its easy to know where the keys are located.

```

#### Example of usage with service:

```
using DictionaryHelper;

@Service.GetValueByKeyAndCulture("key", "en-GB") 

```