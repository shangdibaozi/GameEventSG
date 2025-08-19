# Game Event Source Generator

This C# source generator automatically creates event classes that call methods decorated with custom event attributes.

## Features

- Generates event classes for any attribute implementing `IStaticEventAttribute`
- Automatically collects all static methods decorated with your custom event attributes
- Creates a single entry point method that calls all collected methods

## Usage

1. **Create an event attribute**:
   ```csharp
   public class MyCustomEventAttribute : Attribute, IStaticEventAttribute { }
   ```

2. **Decorate static methods**:
   ```csharp
   public static class ExampleClass
   {
       [MyCustomEvent]
       public static void MyEventHandler()
       {
           // Your event handling logic
       }
   }
   ```

3. **Call the generated event**:
   ```csharp
   // The generator will create:
   // public static class MyCustomEventEvents
   // {
   //     public static void MyCustomEvent()
   //     {
   //         ExampleClass.MyEventHandler();
   //     }
   // }

   // Call the generated event
   MyCustomEventEvents.MyCustomEvent();
   ```
