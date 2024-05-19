# Running Oakton Commands

To run an Oakton command in this repository,
navigate to the root directory of the project and open a command prompt or terminal.
Then use the following format:

```console
dotnet run -- [command name] [options]
```
For example, to run a command named "mycommand" with an argument named "myarg", you would enter:

```console
dotnet run -- mycommand --myarg "some value"
```

To see a list of all available commands, enter:

```console
dotnet run -- help
```

This will display a list of all the available commands and options, along with a brief description of each.
You can also get help on a specific command by running:

```console
dotnet run -- [command name] --help
```

This will display detailed information about the specified command, including its purpose,
available options, and any arguments it expects.
