# Basic Template
## HelloWorld Game

This is a very simple template that will display "Hello World!".    
Well, not that simple:
- It will first instantiate game systems
- It will also inject configuration data.
- A system will then create an entity with the text to print.
- Another system will catch that entity and print the text, and then destroy it.

### Notes
What is the attribute 'RestrictToApplication' ?
- This attribute will make this system create in the specified application.
- In this case, we are restricting it to ExecutiveEntryApplication, this is the entry point of our game.

What is 'DependencyResolver' ?
- This class resolve dependencies in your system.
- In this case, it will resolve 'PrintConfiguration' since it's a data we injected earlier in 'Program', and will also resolve ILogger for PrintSystem.

### Result
When you run the "game", you should have a result like this:    
`[Information, 11/22/2020 00:13:58, HelloWorldTemplate.Systems.PrintSystem] Hello World!`