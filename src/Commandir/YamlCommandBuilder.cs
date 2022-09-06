using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.Hosting;
using YamlDotNet.RepresentationModel;

namespace Commandir
{
    public class YamlException : Exception
    {
        public YamlException(string message)
            : base(message)
        {
        }

        public YamlException(string message, Exception exception)
            : base(message, exception)
        {

        }
    }

    public static class YamlExtensions
    {
        public static TNode? Cast<TNode>(this YamlNode node) where TNode: YamlNode
        {
            return node as TNode;
        }

        public static TNode? GetChildNode<TNode>(this YamlMappingNode node, YamlScalarNode keyNode) where TNode: YamlNode
        {
            if(!node.Children.TryGetValue(keyNode, out YamlNode? childNode))
                return null;

            return childNode.Cast<TNode>();
        }

        public static string? GetChildNodeValue(this YamlMappingNode node, YamlScalarNode keyNode)
        {
            YamlScalarNode? scalarNode = node.GetChildNode<YamlScalarNode>(keyNode);
            if(scalarNode == null)
                return null;

            return scalarNode.Value;
        }
    }

    public class YamlCommandBuilder
    {
        private static readonly YamlScalarNode NameKey = new YamlScalarNode("name");
        private static readonly YamlScalarNode DescriptionKey = new YamlScalarNode("description");
        private static readonly YamlScalarNode CommandsKey = new YamlScalarNode("commands");
        private static readonly YamlScalarNode ActionsKey = new YamlScalarNode("actions");
        private static readonly YamlScalarNode ArgumentsKey = new YamlScalarNode("arguments");
        private static readonly YamlScalarNode OptionsKey = new YamlScalarNode("options");
        private static readonly YamlScalarNode RequiredKey = new YamlScalarNode("required");


        private readonly TextReader _reader;

        public YamlCommandBuilder(TextReader reader)
        {
            _reader = reader;
        }

        public Command Build(Func<IHost, Task> commandHandler)
        {
            YamlStream stream = new YamlStream();
            try
            {
                stream.Load(_reader);
            }
            catch(Exception e)
            {
                throw new YamlException("Failed to load YamlStream", e);
            }

            // TODO: Validate document
            YamlMappingNode? rootNode = stream.Documents[0].RootNode.Cast<YamlMappingNode>();
            if(rootNode == null)
                throw new YamlException("Top-level must be a dictionary.");

            string? rootDescription = rootNode.GetChildNodeValue(DescriptionKey);
            if(string.IsNullOrWhiteSpace(rootDescription))
                throw new YamlException("Top-level dictionary is missing a `description` entry.");

            YamlSequenceNode? commandsNode = rootNode.GetChildNode<YamlSequenceNode>(CommandsKey);
            if(commandsNode == null)
                throw new YamlException("Top-level dictionary is missing a `commands` list.");

            Command rootCommand = new RootCommand(rootDescription);
            
            foreach(YamlMappingNode commandNode in commandsNode)
            {
                string? commandName = commandNode.GetChildNodeValue(NameKey);
                if(string.IsNullOrWhiteSpace(commandName))
                    throw new YamlException("Command is missing a `name` entry.");

                string? commandDescription = commandNode.GetChildNodeValue(DescriptionKey);
                if(string.IsNullOrWhiteSpace(commandDescription))
                    throw new YamlException("Command is missing a `description` entry.");

                YamlSequenceNode? actionsNode = commandNode.GetChildNode<YamlSequenceNode>(ActionsKey);
                if(actionsNode == null)
                    throw new YamlException("Command is missing an `actions` list.");

                ActionCommand command = new ActionCommand(commandName, commandDescription);
                command.Handler = CommandHandler.Create<IHost>(commandHandler);                
                
                foreach(YamlMappingNode actionNode in actionsNode)
                {
                    string? actionName = actionNode.GetChildNodeValue(NameKey);
                    if(string.IsNullOrWhiteSpace(actionName))
                        throw new YamlException("Action is missing a `name` entry.");

                    // Add all key/scalar value pairs to the ActionData
                    ActionData action = new ActionData(actionName);
                    foreach(var pair in actionNode)
                    {
                        // Keys are always scalar values.
                        YamlScalarNode actionKeyNode = pair.Key.Cast<YamlScalarNode>()!;
                        
                        // Keys always have a non-null value.
                        string actionKey = actionKeyNode.Value!;

                        string? actionValue = actionNode.GetChildNodeValue(actionKeyNode);
                        
                        // Permit null/empty string values.
                        action[actionKey] = actionValue;
                    }

                    command.AddAction(action);
                }

                // Arguments are optional.
                YamlSequenceNode? argumentsNode = commandNode.GetChildNode<YamlSequenceNode>(ArgumentsKey);
                if(argumentsNode != null)
                {
                    foreach(YamlMappingNode argumentNode in argumentsNode)
                    {
                        string? argumentName = argumentNode.GetChildNodeValue(NameKey);
                        if(string.IsNullOrWhiteSpace(argumentName))
                            throw new YamlException("Argument is missing a `name` entry.");

                        string? argumentDescription = argumentNode.GetChildNodeValue(DescriptionKey);
                        if(string.IsNullOrWhiteSpace(argumentDescription))
                            throw new YamlException("Argument is missing a `description` entry.");

                        // TODO: Add support for argument types.
                        Argument argument = new Argument<string>(argumentName, argumentDescription);
                        command.AddArgument(argument);
                    }
                }

                // Options are optional.
                YamlSequenceNode? optionsNode = commandNode.GetChildNode<YamlSequenceNode>(OptionsKey);
                if(optionsNode != null)
                {
                    foreach(YamlMappingNode optionNode in optionsNode)
                    {
                        string? optionName = optionNode.GetChildNodeValue(NameKey);
                        if(string.IsNullOrWhiteSpace(optionName))
                            throw new YamlException("Option is missing a `name` entry.");

                        string? optionDescription = optionNode.GetChildNodeValue(DescriptionKey);
                        if(string.IsNullOrWhiteSpace(optionDescription))
                            throw new YamlException("Option is missing a `description` entry.");

                        bool isRequired = false;
                        string? optionRequired = optionNode.GetChildNodeValue(RequiredKey);
                        if(!string.IsNullOrWhiteSpace(optionRequired))
                        {
                            if(!bool.TryParse(optionRequired, out isRequired))
                                throw new YamlException("Option is missing a valid `required` entry.");
                        }

                        Option option = new Option<string>($"--{optionName}", optionDescription);
                        option.IsRequired = isRequired;
                        command.AddOption(option);
                    }
                }

                rootCommand.AddCommand(command);
            }

            return rootCommand;
        }
    }
}