# commandir
Simple Command Runner 

[![test](https://github.com/silvera/commandir/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/silvera/commandir/actions/workflows/build-and-test.yml)

Commandir lets you define commands in a Commandir.yaml file and execute them via the Commandir CLI. Commandir then exposes those commands as commands to Commandir itself.

By default, Commandir looks for a Commandir.yaml file in the current directory. Here is the contents of the Commandir.yaml file for the canonical Hello World example:

```
---
commands:
   - name: hello
     parameters:
        run: echo "Hello World!"
```

Running Commandir from a directory containing this Commandir.yaml file, without any command-line arguments, displays the help information:

```
user@host:~/dev/commandir/src/Commandir/bin/Release/net6.0/linux-x64/publish$ ./Commandir 
Required command was not provided.

Description:

Usage:
  Commandir [command] [options]

Options:
  -v, --verbose   Enables verbose logging.
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  hello
```

Running Commandir with the `hello` command results in the following output:
```
user@host:~/dev/commandir/src/Commandir/bin/Release/net6.0/linux-x64/publish$ ./Commandir hello
Hello World!
```
By default, Commandir prints no information about itself to the console. This can be changed by passing the --verbose (-v) option before invoking a command. For example:

```
user@host:~/dev/commandir/src/Commandir/bin/Release/net6.0/linux-x64/publish$ ./Commandir -v hello
Commandir.Commands.CommandExecutor: Invoking command: /Commandir/hello
Commandir.Commands.CommandExecutor: Executing command: /Commandir/hello
Commandir.Commands.CommandExecutor: Creating file: /home/arisilver/dev/commandir/src/Commandir/bin/Release/net6.0/linux-x64/publish/Commandir_hello.sh with contents: echo "Hello World!"
Commandir.Commands.CommandExecutor: Process Starting: bash /home/arisilver/dev/commandir/src/Commandir/bin/Release/net6.0/linux-x64/publish/Commandir_hello.sh
Hello World!
Commandir.Commands.CommandExecutor: Process Complete. ExitCode: 0
Commandir.Commands.CommandExecutor: Deleting file: /home/arisilver/dev/commandir/src/Commandir/bin/Release/net6.0/linux-x64/publish/Commandir_hello.sh
```  

Commandir supports arguments, options and subcommands. These are demonstrated via the Commandir.yaml file included in the output directory:

```
---
description: Sample Commands
commands:
   - name: hello
     parameters:
        executable: true
     commands:
        - name: world
          description: Prints 'Hello World'
          parameters:
             run: echo "Hello World!"
   - name: greet
     description: Greets the user
     parameters:
        greeting: "Hello "
        run: "echo {{greeting}} {{name}}!"
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
user@host:~/dev/commandir/src/Commandir/bin/Release/net6.0/linux-x64/publish$ ./Commandir greet "John Smith"
Hello John Smith!
```

The `greeting` defaults to "Hello " but can be optionally overridden by the `--greeting` option: 
```
user@host:~/dev/commandir/src/Commandir/bin/Release/net6.0/linux-x64/publish$ ./Commandir greet "John Smith" --greeting "Hey!"
Hey! John Smith!
```

This example also demonstrates the relationship between parameters, arguments and options. Parameters can be used to specify a default value. Arguments override parameters and options override arguments or parameters.
