# Game Event Source Generator

This C# source generator automatically creates event classes that call methods decorated with custom event attributes.

## Features

- Generates event classes for any attribute implementing `IEventAttribute`
- Automatically collects all static methods decorated with your custom event attributes
- Creates a single entry point method that calls all collected methods

## Usage

1. **Create an event attribute**:
   ```csharp
   using GameEventGenerator;

   public class MyCustomEventAttribute : Attribute, IEventAttribute { }
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

## Requirements

- .NET Standard 2.0+
- Roslyn 4.0.1+

## Installation

1. Reference this project in your solution
2. Build your project - the generator will automatically create the event classes

## Example

See the included `StartBattleAttribute.cs` for a sample implementation.

## How It Works

1. The generator finds all attributes implementing `IEventAttribute`
2. For each attribute type, it collects all static methods decorated with that attribute
3. It generates a class named `{AttributeName}Events` with a method that calls all collected methods
4. The generated files are placed in the `obj` directory during build
