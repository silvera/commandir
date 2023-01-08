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

Running Commandir with the `hello` command results in the following output:

```
user@host:~/dev/commandir/src/Commandir/bin/Release/net6.0/linux-x64/publish$ ./Commandir hello
Hello World!
```

This runs `echo "Hello World!"` using the default shell. The default shell on Linux/MacOS is `bash` and  `pwsh` (Powershell) on Windows". The `cmd` (DOS) shell is also supported on Windows. The shell can be changed by specifying the `shell` parameter. The following example runs the `hello` command using the `sh` shell on Linux:

```
---
commands:
   - name: hello
     parameters:
        shell: sh
        run: echo "Hello World!"
```

By default, Commandir does not log any activity so as not to contaminate STDOUT. We can enable verbose logging via the --verbose (-v) flag, which shows the `sh` shell is used:
```
user@host:~/dev/commandir/src/Commandir/bin/Release/net6.0/linux-x64/publish$ ./Commandir -v hello
Commandir.Commands.CommandExecutor: Invoking command: /Commandir/hello
Commandir.Commands.CommandExecutor: Executing command: /Commandir/hello
Commandir.Commands.CommandExecutor: Creating file: /home/user/dev/commandir/src/Commandir/bin/Release/net6.0/linux-x64/publish/Commandir_hello.sh with contents: echo "Hello World!"
Commandir.Commands.CommandExecutor: Process Starting: sh /home/user/dev/commandir/src/Commandir/bin/Release/net6.0/linux-x64/publish/Commandir_hello.sh
Hello World!
Commandir.Commands.CommandExecutor: Process Complete. ExitCode: 0
Commandir.Commands.CommandExecutor: Deleting file: /home/user/dev/commandir/src/Commandir/bin/Release/net6.0/linux-x64/publish/Commandir_hello.sh
```

Running Commandir from a directory containing a Commandir.yaml file, without any command-line arguments, displays the help information:

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
