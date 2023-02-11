# commandir
Simple Command Runner 

[![test](https://github.com/silvera/commandir/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/silvera/commandir/actions/workflows/build-and-test.yml)

Commandir is an application that allows users to define commands in a `Commandir.yaml` file and invoke them as commands of the Commandir application itself. Commandir looks for a `Commandir.yaml` file in the current directory.

The `hello` and `greet` directories contain example Commandir.yaml files. 

### Hello World
The `hello` directory contains a Commandir.yaml file illustrating the canonical Hello World example:

```
---
commands:
   - name: hello
     parameters:
        run: echo "Hello World!"
```
Running Commandir from the hello directory, without any command line arguments, displays the help information:

```
user@host:~/dev/commandir/src/Commandir/hello$ ../bin/Debug/net6.0/Commandir 
Required command was not provided.

Description:
  The canonical Hello World example

Usage:
  Commandir [command] [options]

Options:
  -v, --verbose   Enables verbose logging.
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  hello  Prints 'Hello World!'
```

Note how the `hello` command defined in the Commandir.yaml file is a bona fide Commandir command.

Running Commandir from a directory that does not contain a Commandir.yaml file generates an error message:
```
user@host:~/dev/commandir/src/Commandir$ bin/Debug/net6.0/Commandir 
Commandir: FileNotFoundException: Could not find file '~/dev/commandir/src/Commandir/Commandir.yaml'.
```

Running Commandir with the `hello` command on Linux (Ubuntu 22.04 LTS) results in the following output:

```
user@host:~/dev/commandir/src/Commandir/hello$ ../bin/Debug/net6.0/Commandir hello
Hello World!
```

This runs `echo "Hello World!"` using the default shell for the OS. The default shells are:
 - `bash`: (Linux/MacOS)
 - `pwsh`: (Windows, Powershell)

The `cmd` (DOS) shell is also supported on Windows. The shell can be changed by specifying the `shell` parameter. The following example runs the `hello` command using the `sh` shell on Linux:

```
---
commands:
   - name: hello
     parameters:
        run: echo "Hello World!"
        shell: sh
```

### Logging
Commandir does not log any activity by default, so as not to contaminate STDOUT. Verbose logging is enabled via the --verbose (-v) flag, which shows the `sh` shell is used:
```
user@host:~/dev/commandir/src/Commandir/hello$ ../bin/Debug/net6.0/Commandir -v hello
Commandir.Commands.CommandExecutor: Executing command `/Commandir/hello`
Commandir.Commands.SequentialCommandGroup: Adding command `/Commandir/hello` to group `hello`
Commandir.Commands.SequentialCommandGroup: Executing command `/Commandir/hello` on group `hello`
Commandir.Executors.Shell: Creating file: ~/dev/commandir/src/Commandir/hello/Commandir_hello.sh with contents: echo "Hello World!"
Commandir.Executors.Shell: Process Starting: sh ~/dev/commandir/src/Commandir/hello/Commandir_hello.sh
Hello World!
Commandir.Executors.Shell: Process Complete. ExitCode: 0
Commandir.Executors.Shell: Deleting file: ~/dev/commandir/src/Commandir/hello/Commandir_hello.sh
```

### A More Complex Example

Commandir supports parameters, arguments, options and subcommands. These are demonstrated by the Commandir.yaml file in the `greet` folder:

```
---
name: Commandir
description: Greeting commands
commands:
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

Running the `greet` command:

```
user@host:~/dev/commandir/src/Commandir/greet$ ../bin/Debug/net6.0/Commandir greet "John Smith"
Hello John Smith!
```

The `greeting` defaults to "Hello " but can be optionally overridden by the `--greeting` option: 
```
user@host:~/dev/commandir/src/Commandir/greet$ ../bin/Debug/net6.0/Commandir greet "John Smith" --greeting "Hey!"
Hey! John Smith!
```
### Templates
Parameters, arguments and options can be referenced in the `run` parameter via `mustache` template syntax. In the above example, the values of the greeting parameter and name argument are populated with the appropriate values when the command is executed.

### Parameters
Parameters are a dictionary that contains key-value pairs. The value can be of any type, including dictionaries or lists, but are typically scalars. Different executors may require different parameters. In addition, users can define arbitrary key-value pairs and reference them via templates.

The `shell` executor requires a `run` parameter, whose content is executed by the specified shell. As shown earlier, the desired shell can be specified by the `shell` parameter. 

Parameters can be defined at higher levels (e.g. a parent command) and overridden at lower levels (e.g. a child command) by specifying a different value. Parameters can also be defined at the top (root) level.

Parameters can also be overridden by arguments or options, by setting the name of argument or option to the name of the parameter. The `greeting` parameter and option above illustrates this. 

### Arguments
Arguments are required. 

### Options
Options are optional by default. They can be made required by adding `required: true` to the option. A `shortName` can also be specified, which allows options to be invoked via flag syntax, e.g. `-g` instead of `--greeting`.

### Subcommands
Commandir supports subcommands by adding a `commands` dictionary to any command. For example, we can add a `world` subcommand to the `hello` command from earlier:

```
---
name: Commandir
description: The canonical Hello World example
commands:
   - name: hello
     commands:
        - name: world
          description: Prints 'Hello World!'
          parameters:
             run: echo "Hello World!"
```

The command is invoked as follows:
```
user@host:~/dev/commandir/src/Commandir/hello$ ../bin/Debug/net6.0/Commandir hello world
Hello World!
```

### Invocation
Internally, each command is associated with an `executor`. The default executor is `shell`, which runs the commands specified by `run` using the specified shell. There is also a `test` executor, used for unit tests. 

When a command is invoked, the executor receives a single dictionary containing the  values of the parameters, arguments and options.

#### Subcommand Execution
Commands with subcommands (aka `parent` commands) are not executable by default. For example, trying to execute the `hello` command above yields:
```
user@host:~/dev/commandir/src/Commandir/hello$ ../bin/Debug/net6.0/Commandir hello
Required command was not provided.

Description:

Usage:
  Commandir hello [command] [options]

Options:
  -?, -h, --help  Show help and usage information

Commands:
  world  Prints 'Hello World!'
```

An parent command can be used to recursively execute child commands (in serial or parallel) by adding an `executable: true` parameter to the parent command's parameters dictionary. The updated Commandir.yaml file is:
```
---
name: Commandir
description: The canonical Hello World example
commands:
   - name: hello
     parameters:
        executable: true
     commands:
        - name: world
          description: Prints 'Hello World!'
          parameters:
             run: echo "Hello World!"
```
The `hello` command can now be executed, which automatically executes the `world` child command:
```
user@host:~/dev/commandir/src/Commandir/hello$ ../bin/Debug/net6.0/Commandir hello
Hello World!
```

#### Parallel Execution
When an internal command is invoked, its subcommands are invoked sequentially by default. They can be invoked in parallel by adding the `parallel: true` parameter to the parent command's parameters dictionary.

```
---
description: Serial and Parallel Command Unit Test Example
commands:
   - name: group-tests
      parameters:
         parallel: true
         logMessage: true
         delaySeconds: 5
      commands:
         - name: serial
         parameters:
            parallel: false
         commands:
            - name: serial1
               executor: test
               parameters:
                  message: serial1
            - name: serial2
               executor: test
               parameters:
                  message: serial2
            - name: serial3
               executor: test
               parameters:
                  message: serial3
         - name: parallel
         parameters:
            parallel: true
         commands:
            - name: parallel1
               executor: test
               parameters:
                  message: parallel1
            - name: parallel2
               executor: test
               parameters:
                  message: parallel2
            - name: parallel3
               executor: test
               parameters:
                  message: parallel3

```

Invoking the `group-tests` command creates a top-level parallel command group (`group-tests`) containing a serial command group (`serial`) and another parallel command group (`parallel`). The serial and parallel groups are executed in parallel, with the `serial1-serial3` commands executed serially and the `parallel1-parallel3` commands executed in parallel.