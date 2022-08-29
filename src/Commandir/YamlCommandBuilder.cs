using System.CommandLine;
using YamlDotNet.RepresentationModel;
using Commandir.Core;

namespace Commandir
{
    public abstract class CommandBuilder
    {
        public abstract RootCommand Build();
    }

    public class YamlCommandBuilder : CommandBuilder
    {
        private static readonly YamlScalarNode NameKey = new YamlScalarNode("name");
        private static readonly YamlScalarNode DescriptionKey = new YamlScalarNode("description");
        private static readonly YamlScalarNode ActionsKey = new YamlScalarNode("actions");
        private static readonly YamlScalarNode ArgumentsKey = new YamlScalarNode("arguments");
        private static readonly YamlScalarNode OptionsKey = new YamlScalarNode("options");

        private readonly TextReader _reader;
        public YamlCommandBuilder(TextReader reader)
        {
            _reader = reader;
        }

        public override RootCommand Build()
        {
            YamlStream stream = new YamlStream();
            stream.Load(_reader);

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

            RootCommand rootCommand = new RootCommand(rootDescription);
            
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

                if(!(commandNode[ActionsKey] is YamlSequenceNode actionsListNode))
                    throw new Exception();
                
                CommandirCommand command = new CommandirCommand(commandName, commandDescription);                
                
                foreach(YamlMappingNode actionNode in actionsListNode)
                {
                    // Name
                    if(!(actionNode[NameKey] is YamlScalarNode actionNameNode))
                        throw new Exception();

                    string? actionName = actionNameNode.Value;
                    if(string.IsNullOrWhiteSpace(actionName))
                        throw new Exception();

                    ActionContext actionContext = new ActionContext(actionName);
                    foreach(var pair in actionNode)
                    {
                        if(!(pair.Key is YamlScalarNode keyNode))
                            throw new Exception();
                    
                        // Only support scalar values for now.
                        if(!(pair.Value is YamlScalarNode valueNode))
                            throw new Exception();

                        string? value = valueNode.Value;
                        if(string.IsNullOrWhiteSpace(value))
                            throw new Exception();

                        actionContext[keyNode.Value!] = value;
                    }

                    command.AddAction(actionContext);
                }

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