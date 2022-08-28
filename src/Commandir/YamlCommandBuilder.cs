using System.CommandLine;
using System.CommandLine.Invocation;
using YamlDotNet.RepresentationModel;
using Commandir.Core;

namespace Commandir
{
    public interface ICommmandBuilder
    {
        Command Build();
    }

    public class YamlCommandBuilder : ICommmandBuilder
    {
        const string Commands = @"---
        description: Greeting Commands
        commands:
           - name: greet
             description: Greets the user
             handler:
                name: shell
                shell: bash
                run: echo ${{greeting}} ${{name}}
             arguments:
                - name: greeting
                  type: string
                  description: The greeting.
             options:
                - name: name
                  description: The name.
        ";

        private static readonly YamlScalarNode NameKey = new YamlScalarNode("name");
        private static readonly YamlScalarNode DescriptionKey = new YamlScalarNode("description");
        private static readonly YamlScalarNode ArgumentsKey = new YamlScalarNode("arguments");
        private static readonly YamlScalarNode OptionsKey = new YamlScalarNode("options");
        private static readonly YamlScalarNode HandlerKey = new YamlScalarNode("handler");

        private readonly ICommandContextHandler _commandHandler;
        public YamlCommandBuilder(ICommandContextHandler commandHandler)
        {
            _commandHandler = commandHandler;
        }

        public Command Build()
        {
            StringReader reader = new StringReader(Commands);
            YamlStream stream = new YamlStream();
            stream.Load(reader);

            // TODO: Validate document
            YamlMappingNode? rootNode = stream.Documents[0].RootNode as YamlMappingNode;
            if(rootNode == null)
                throw new Exception();

            YamlNode? node = null;
            YamlScalarNode DescriptionKey = new YamlScalarNode("description");
            if(!rootNode.Children.TryGetValue(DescriptionKey, out node))
                throw new Exception();

            string rootDescription = ((YamlScalarNode)node).Value!;
            if(string.IsNullOrWhiteSpace(rootDescription))
                throw new Exception();

            Command rootCommand = new RootCommand(rootDescription);
            
            YamlScalarNode CommandsKey = new YamlScalarNode("commands");
            if(!rootNode.Children.TryGetValue(CommandsKey, out node))
                throw new Exception();
            
            YamlSequenceNode commandsNode = (YamlSequenceNode)node;
            foreach(YamlMappingNode commandNode in commandsNode)
            {
                // TODO: Extract Scalar
                if(!(commandNode[NameKey] is YamlScalarNode commandNameNode))
                    throw new Exception();
               
                string? commandName = commandNameNode.Value;
                if(string.IsNullOrWhiteSpace(commandName))
                    throw new Exception();

                if(!(commandNode[DescriptionKey] is YamlScalarNode commandDescriptionNode))
                    throw new Exception();
               
                string? commandDescription = commandDescriptionNode.Value;
                if(string.IsNullOrWhiteSpace(commandDescription))
                    throw new Exception();

                if(!(commandNode[HandlerKey] is YamlMappingNode handlerNode))
                    throw new Exception();
                
                HandlerContext handlerContext = new HandlerContext { Handler = _commandHandler };
                foreach(var handlerPair in handlerNode)
                {
                    if(!(handlerPair.Key is YamlScalarNode itemNameNode))
                        throw new Exception();
                    
                    // Only support scalar values for now.
                    if(!(handlerPair.Value is YamlScalarNode itemValueNode))
                        throw new Exception();
                    
                    handlerContext.Entries[itemNameNode.Value] = itemValueNode.Value;
                }

                CommandWithHandler command = new CommandWithHandler(commandName, commandDescription, handlerContext);
                
                if(commandNode[ArgumentsKey] is YamlSequenceNode argumentsListNode)
                {
                    foreach(YamlMappingNode argumentsNode in argumentsListNode)
                    {
                        // Name
                        if(!(argumentsNode[NameKey] is YamlScalarNode argumentNameNode))
                            throw new Exception();

                        string? argumentName = argumentNameNode.Value;
                        if(string.IsNullOrWhiteSpace(argumentName))
                            throw new Exception();

                        // Description
                        if(!(argumentsNode[NameKey] is YamlScalarNode argumentDescriptionNode))
                            throw new Exception();

                        string? argumentDescription = argumentDescriptionNode.Value;
                        if(string.IsNullOrWhiteSpace(argumentDescription))
                            throw new Exception();

                        Argument argument = new Argument<string>(argumentName, argumentDescription);
                        command.AddArgument(argument);
                    }
                }

                if(commandNode[OptionsKey] is YamlSequenceNode optionsListNode)
                {
                    foreach(YamlMappingNode optionsNode in optionsListNode)
                    {
                        // Name
                        if(!(optionsNode[NameKey] is YamlScalarNode optionNameNode))
                            throw new Exception();

                        string? optionName = optionNameNode.Value;
                        if(string.IsNullOrWhiteSpace(optionName))
                            throw new Exception();

                        // Description
                        if(!(optionsNode[NameKey] is YamlScalarNode optionDescriptionNode))
                            throw new Exception();

                        string? optionDescription = optionDescriptionNode.Value;
                        if(string.IsNullOrWhiteSpace(optionDescription))
                            throw new Exception();

                        Option option = new Option<string>($"--{optionName}", optionDescription);
                        option.IsRequired = true;
                        command.AddOption(option);
                    }
                }

                rootCommand.AddCommand(command);                
            }

            return rootCommand;
        }
    }
}