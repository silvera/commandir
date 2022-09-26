# commandir
Simple Command Runner 

[![test](https://github.com/silvera/commandir/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/silvera/commandir/actions/workflows/build-and-test.yml)

Commandir lets you define commands in a Commandir.yaml file and execute them via the Commandir CLI. 

Here is the Commandir.yaml file for the canonical Hello World example:
'''
---
commands:
   - name: hello
     description: Prints 'Hello World'
     type: Commandir.Builtins.Shell
     parameters:
        command: echo "Hello World!"
'''

