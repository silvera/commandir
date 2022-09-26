# commandir
Simple Command Runner 

[![test](https://github.com/silvera/commandir/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/silvera/commandir/actions/workflows/build-and-test.yml)

Commandir lets you define commands in a Commandir.yaml file and execute them via the Commandir CLI. By default, Commandir looks for a Commandir.yaml file in the current directory. 

Here is the Commandir.yaml file for the canonical Hello World example:

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
