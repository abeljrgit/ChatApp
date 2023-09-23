using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

string ConnectionString = "Endpoint=sb://abelchatappsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=hnWJSyPD8pH2biUUBru6L/9LV7eOYPQX8+ASbES7FLw=";
string TopicName = "chattopic";

Console.WriteLine("Enter name:");
var userName = Console.ReadLine();

// Create an administration client to manage artifacts
var serviceBusAdministrationClient = new ServiceBusAdministrationClient(ConnectionString);

// Create a topic if it does not exist
if(!await serviceBusAdministrationClient.TopicExistsAsync(TopicName))
{
    await serviceBusAdministrationClient.CreateTopicAsync(TopicName);
}

// Create a temporary subscription for the user if it does not exist
if(!await serviceBusAdministrationClient.SubscriptionExistsAsync(TopicName, userName))
{
    var options = new CreateSubscriptionOptions(TopicName, userName)
    {
        AutoDeleteOnIdle = TimeSpan.FromMinutes(5)
    };
    await serviceBusAdministrationClient.CreateSubscriptionAsync(options);
}

// Create a service bus client
var serviceBusClient = new ServiceBusClient(ConnectionString);

// Create a service bus sender
var serviceBusSender = serviceBusClient.CreateSender(TopicName);

// Create a message processor
var processor = serviceBusClient.CreateProcessor(TopicName, userName);

// Add handler to process messages
processor.ProcessMessageAsync += MessageHandler;

// Add handler to process any errors
processor.ProcessErrorAsync += ErrorHandler;

// Start the message processor
await processor.StartProcessingAsync();

// Send a Hello message
var helloMessage = new ServiceBusMessage($"{ userName } has entered the room.");
await serviceBusSender.SendMessageAsync(helloMessage);

while(true)
{
    var text = Console.ReadLine();

    if(text == "exit")
    {
        break;
    }

    // Send a chat message
    var message = new ServiceBusMessage($"{userName}>{text}");
    await serviceBusSender.SendMessageAsync(message);
}

// Send a goodbye message
var goodbyeMessage = new ServiceBusMessage($"{userName} has left the room.");
await serviceBusSender.SendMessageAsync(goodbyeMessage);

// Stop the message processor
await processor.StopProcessingAsync();

// Close the processor and sender
await processor.CloseAsync();
await serviceBusSender.CloseAsync();

async Task MessageHandler(ProcessMessageEventArgs args)
{
    // Retrieve and print the message body.
    var test = args.Message.Body.ToString();
    Console.WriteLine(test);

    // Complete the message
    await args.CompleteMessageAsync(args.Message);
}

async Task ErrorHandler(ProcessErrorEventArgs args)
{
    throw new NotImplementedException();
}