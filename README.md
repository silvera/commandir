# commandir
Simple Command Runner 

[![test](https://github.com/silvera/commandir/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/silvera/commandir/actions/workflows/build-and-test.yml)

Commandir lets you define commands in a Commandir.yaml file and execute them via the Commandir CLI. Commandir then exposes those commands as commands to Commandir itself.

By default, Commandir looks for a Commandir.yaml file in the current directory. Here is the Commandir.yaml file for the canonical Hello World example:

```
---
commands:
   - name: hello
     description: Prints 'Hello World'
     type: Commandir.Builtins.Shell
     parameters:
        command: echo "Hello World!"
```

Running Commandir from a directory containing this Commandir.yaml file, without any command-line arguments, displays the help information:

```
arisilver@penguin:~/dev/commandir/src/Commandir$ ./bin/Debug/net6.0/Commandir
Required command was not provided.

Description:

Usage:
  Commandir [command] [options]

Options:
  --verbose       Enables verbose logging
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  hello  Prints 'Hello World'
```

Running Commandir with the `hello` command results in the following output:
```
arisilver@penguin:~/dev/commandir/src/Commandir$ ./bin/Debug/net6.0/Commandir hello
Hello World!
```
By default, Commandir prints no information about itself to the console. This can be changed by passing the --verbose option before invoking a command. For example:
```
arisilver@penguin:~/dev/commandir/src/Commandir$ ./bin/Debug/net6.0/Commandir --verbose hello
Commandir.CommandBuilder: Creating Command: hello Arguments: [] Options: [] Commands: []
Commandir.CommandProvider: Adding Command: Commandir.Builtins.Echo
Commandir.CommandProvider: Adding Command: Commandir.Builtins.Shell
Commandir.CommandExecutor: Executing Command: Name: hello Type: Commandir.Builtins.Shell
Commandir.CommandExecutor: Adding Parameter: Name: command Value: echo "Hello World!" IsOverride: False
Commandir.Builtins.Shell: Wrote command: echo "Hello World!" to file: /tmp/tmpBM1LIW.tmp
Commandir.Builtins.Shell: Executing command: echo "Hello World!"
Hello World!
Commandir.Builtins.Shell: Deleting file: /tmp/tmpBM1LIW.tmp
```  

Commandir supports arguments, options and subcommands. Arguments and options are demonstrated via the following Commandir.yaml file:
```
---
commands:
   - name: greet
     description: Greets the user
     type: Commandir.Builtins.Shell
     parameters:
        greeting: "Hello "
        command: "echo {{greeting}} {{name}}"
     arguments:
        - name: name
          description: The user's name
     options:
        -  name: greeting
           description: The greeting
           required: false
```
Arguments are required while options are optional by default. They can be made required by adding `required: true` to the option.  

Running the `greet` command:
```
arisilver@penguin:~/dev/commandir/src/Commandir$ ./bin/Debug/net6.0/Commandir greet "John Smith"
Hello John Smith
```

The `greeting` defaults to "Hello " but can be optionally overridden by the `--greeting` option: 
```
arisilver@penguin:~/dev/commandir/src/Commandir$ ./bin/Debug/net6.0/Commandir greet "John Smith" --greeting "Hey!"
Hey! John Smith
```

This example also demonstrates the relationship between parameters, arguments and options. Named Parameters specify a default value while Arguments and Options with the same name override the default value. Here is the verbose logging for the `greet` command:
```
arisilver@penguin:~/dev/commandir/src/Commandir$ ./bin/Debug/net6.0/Commandir --verbose greet "John
 Smith" --greeting "Hey!"
Commandir.CommandBuilder: Creating Command: greet Arguments: [name] Options: [greeting] Commands: []
Commandir.CommandProvider: Adding Command: Commandir.Builtins.Echo
Commandir.CommandProvider: Adding Command: Commandir.Builtins.Shell
Commandir.CommandExecutor: Executing Command: Name: greet Type: Commandir.Builtins.Shell
Commandir.CommandExecutor: Adding Parameter: Name: greeting Value: Hello  IsOverride: False
Commandir.CommandExecutor: Adding Parameter: Name: command Value: echo {{greeting}} {{name}} IsOverride: False
Commandir.CommandExecutor: Adding Argument: Name: name Value: John Smith IsOverride: False
Commandir.CommandExecutor: Adding Option: Name: greeting Value: Hey! IsOverride: True
Commandir.Builtins.Shell: Wrote command: echo Hey! John Smith to file: /tmp/tmp1U8fCt.tmp
Commandir.Builtins.Shell: Executing command: echo Hey! John Smith
Hey! John Smith
Commandir.Builtins.Shell: Deleting file: /tmp/tmp1U8fCt.tmp
``` 
