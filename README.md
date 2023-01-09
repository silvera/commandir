# commandir
Simple Command Runner 

[![test](https://github.com/silvera/commandir/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/silvera/commandir/actions/workflows/build-and-test.yml)

Commandir is an application that allows users to define commands in a file and invoke them as commands of the Commandir application itself.

Commandir looks for a Commandir.yaml file in the current directory. Here is the contents of a minimal Commandir.yaml file illustrating the canonical Hello World example:

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

This runs `echo "Hello World!"` using the default shell. The default shell is `bash` on Linux/MacOS and `pwsh` (Powershell) on Windows". The `cmd` (DOS) shell is also supported on Windows. The shell can be changed by specifying the `shell` parameter. The following example runs the `hello` command using the `sh` shell on Linux:

```
---
commands:
   - name: hello
     parameters:
        run: echo "Hello World!"
        shell: sh
```

By default, Commandir does not log any activity so as not to contaminate STDOUT. Verbose logging is enabled via the --verbose (-v) flag, which shows the `sh` shell is used:
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

Commandir supports parameters, arguments, options and subcommands. These are demonstrated via the Commandir.yaml file included with the application:

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
           shortName: g
           description: The greeting
           required: false
```
### Command Execution
Internally, each command is associated with an `executor`. The default executor is `shell`, which runs the commands specified by `run` using the specified shell. There is also a `test` executor, used for unit tests. 

When a command is invoked, the executor receives a single dictionary containing the  values of the parameters, arguments and options.

### Parameters
Parameters are a dictionary that contains user-defined key-value pairs. The value can be of any type, including dictionaries or lists, but are typically scalars. The use and interpretation of parameters is executor-dependent. 

The `shell` executor requires a `run` parameter, whose content is invoked by the specified shell. As shown earlier, the desired shell can be specified by the `shell` parameter. 

Parameters can be set at higher levels e.g. the top-level and overridden at lower levels (e.g. for a particular command) by specifying a different value.

Parameters can also be overridden by arguments or options, by setting the name of argument or option to the name of the parameter. The `greeting` parameter and option above illustrates this. 

### Arguments
Arguments are required. 

### Options
Options are optional by default. They can be made required by adding `required: true` to the option. 

### Subcommands
Commandir supports subcommands by adding a `commands` key to any command, as illustrated by the sample commands above.

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

