# Umbraco DictionaryHelper

This project was specifically created to fix sql issues we had with the core Umbraco dictionary service.

All dictionary items are fetched and inserted into concurrency dictionary cache on start up and will be accessible with an extension on Umbraco helper.

### Example of usage with Umbraco helper:

```
@using DictionaryHelper;

@Umbraco.DictionaryValue("key") //Fetches the value of the dictionary item. Returns empty string if not found.
@Umbraco.DictionaryValue("key", "Default value") //Fetches the value of the dictionary item. Returns the default value if value is empty or key not found.
@Umbraco.DictionaryValue("key", "Default value", true) //Fetches the value of the dictionary item. Returns the default value if value is empty or key not found and will create the dictionary item with the default value if the key is not found.

```

### Example of usage with service:

```
using DictionaryHelper;

@Service.GetValueByKeyAndCulture("key", "en-GB") 
@Service.GetValueByKeyAndCulture("key", "en-GB", "Default value") 
@Service.GetValueByKeyAndCulture("key", "en-GB", "Default value", true)

```