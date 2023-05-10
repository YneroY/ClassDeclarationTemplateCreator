using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;


namespace MyCompletionProvider
{
    [ExportCompletionProvider(name: nameof(MyCompletionProvider), language: LanguageNames.CSharp), Shared]
    internal class MyCompletionProvider : CompletionProvider
    {
        private const string Receiver = nameof(Receiver);
        private const string Description = nameof(Description);
        private const string Tag = nameof(Tag);
        private const string Snippet = nameof(Snippet);

        private const string CLASSTEMPLATEATTRIBUTE = @"[System.AttributeUsage(AttributeTargets.Class)]
public class ClassTemplateAttribute : System.Attribute {}";

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {

            switch (trigger.Kind)
            {
                //case CompletionTriggerKind.Insertion:
                //    return ShouldTriggerCompletion(text, caretPosition);

                case CompletionTriggerKind.Invoke:
                    return true;

                default:
                    return false;
            }
        }

        private static bool ShouldTriggerCompletion(SourceText text, int position)
        {
            // Provide completion if user typed "." after a whitespace/tab/newline char.
            var insertedCharacterPosition = position - 1;

            if (insertedCharacterPosition <= 0)
            {
                return false;
            }

            var ch = text[insertedCharacterPosition];
            var previousCh = text[insertedCharacterPosition - 1];
            return ch == '.' &&
                (char.IsWhiteSpace(previousCh) || previousCh == '\t' || previousCh == '\r' || previousCh == '\n');
        }

        private static bool IsClassAttributeCompletion(SourceText text, int position)
        {
            // Provide completion if user typed "." after a whitespace/tab/newline char.
            var charStartPosition = position - 3;

            if (charStartPosition <= 0)
            {
                return false;
            }

            string possibleWord = new string(new char[] { text[charStartPosition], text[charStartPosition + 1], text[charStartPosition + 2] });

            var ch = text[charStartPosition];
            var previousCh = text[charStartPosition - 1];
            return possibleWord == "cad";
        }

        private static void Container_TextChanged(object sender, TextChangeEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        public async override Task ProvideCompletionsAsync(CompletionContext context)
        {
            var allDocuments = context.Document.Project.Documents;

            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var text = await model.SyntaxTree.GetTextAsync(context.CancellationToken).ConfigureAwait(false);

            #region Snippet to produce required attribute.

            if (ShouldTriggerCompletion(text, context.Position))
            {
                var properties = ImmutableDictionary<string, string>.Empty
                                    .Add(Tag, TagCategory.CLASS_TEMPLATE_ATTRIBUTE)
                                    .Add(Description, "Attribute to produce class template")
                                    .Add(Snippet, CLASSTEMPLATEATTRIBUTE);

                var rule = CompletionItemRules.Create(formatOnCommit: true);
                var hint = CompletionItem.Create("ClassTemplateAttribute", properties: properties, rules: rule);

                context.AddItem(hint);
                return;
            }

            #endregion

            var classVisitor = new ClassVirtualizationVisitor();

            Dictionary<string, List<string>> namespaceToClassesMapping = new Dictionary<string, List<string>>();
            Dictionary<string, List<PropertyInfo>> classToPropertiesMapping = new Dictionary<string, List<PropertyInfo>>();

            foreach (var item in allDocuments)
            {
                if (!item.SupportsSemanticModel || !item.SupportsSyntaxTree) continue;

                SyntaxNode currentNode = await item.GetSyntaxRootAsync();

                var descendantNodes = currentNode.DescendantNodes();
                var nds = descendantNodes.OfType<NamespaceDeclarationSyntax>();

                if (nds == null || nds.Count() != 1) continue;

                NamespaceDeclarationSyntax namespaceDeclarationSyntax = nds.First();

                classVisitor.Visit(currentNode);

                if (classVisitor.classes.Count() < 1)
                // Check if there are classes found in the current document
                {
                    continue;
                }


                // Use semantic model to extract all the properties of a class
                var currentSemanticModel = await item.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

                if (!namespaceToClassesMapping.ContainsKey(namespaceDeclarationSyntax.Name.ToString()))
                {
                    namespaceToClassesMapping.Add(namespaceDeclarationSyntax.Name.ToString(), new List<string>());
                }

                // Process all the classes to retrieve public properties
                foreach (var individualClass in classVisitor.classes)
                {
                    string className = individualClass.Identifier.ValueText;

                    #region Get comment, if any.

                    //var trivias = individualClass.GetLeadingTrivia();
                    //SyntaxTrivia xmlCommentTrivia = trivias.FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
                    //SyntaxNode xml = xmlCommentTrivia.GetStructure();

                    //var xmlTrivia = individualClass.GetLeadingTrivia()
                    //                               .Select(i => i.GetStructure())
                    //                               .OfType<DocumentationCommentTriviaSyntax>()
                    //                               .FirstOrDefault();

                    //if (xmlTrivia != null)
                    //{
                    //    XmlElementSyntax syntax = xmlTrivia.ChildNodes()
                    //            .OfType<XmlElementSyntax>()
                    //            .FirstOrDefault(i => i.StartTag.Name.ToString().Equals("summary"));

                    //    if (syntax != null)
                    //    {
                    //        classToCommentMapping.Add(className, syntax.Content[0].ToString().Trim(new char[] { ' ', '/' }));
                    //    }
                    //}

                    #endregion

                    namespaceToClassesMapping[namespaceDeclarationSyntax.Name.ToString()].Add(className);

                    var classSymbol = currentSemanticModel.GetDeclaredSymbol(individualClass);

                    // We are only interested in public properties
                    var properties = classSymbol.GetMembers()
                                                .Where(s => s.Kind == SymbolKind.Property && s.DeclaredAccessibility == Accessibility.Public)
                                                .ToList();

                    if (!classToPropertiesMapping.ContainsKey(className))
                    {
                        classToPropertiesMapping.Add(className, new List<PropertyInfo>());
                    }

                    foreach (var property in properties)
                    {
                        IPropertySymbol propertySymbol = (IPropertySymbol)property;

                        //string typeName = propertySymbol.Type.Name;
                        string propertySymbolType = propertySymbol.Type.ToString();
                        //bool isNullable = propertySymbol.Type.NullableAnnotation != null;
                        bool isNullable = propertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated;
                        classToPropertiesMapping[className].Add(new PropertyInfo() { Name = property.Name.ToString(), Type = propertySymbolType, IsNullable = isNullable });
                    }
                }

                classVisitor.classes.Clear();
            }

            foreach (var item in namespaceToClassesMapping)
            {
                string namespaceString = item.Key;

                foreach (var myClass in item.Value)
                {
                    string desc = $"Initialize {myClass} template";

                    var properties = ImmutableDictionary<string, string>.Empty
                                    .Add(Receiver, namespaceString)
                                    .Add(Description, desc);

                    int numOfProperties = classToPropertiesMapping[myClass].Count();

                    StringBuilder sb = new StringBuilder();

                    if (numOfProperties > 0)
                    {
                        sb.AppendLine($"{myClass} {myClass.ToLower()} = new {myClass}()");
                        sb.AppendLine($"{{");

                        List<PropertyInfo> propertyInfos = classToPropertiesMapping[myClass];

                        for (int x = 0; x < numOfProperties; x++)
                        {
                            string comma = x == numOfProperties - 1 ? "" : ",";

                            string propertyValue = "null";

                            if (Helper.PROPERTYTYPE_REFERENCE.ContainsKey(propertyInfos[x].Type))
                            {
                                propertyValue = Helper.PROPERTYTYPE_REFERENCE[propertyInfos[x].Type];
                            }

                            // Nullable property has precedence..
                            if (propertyInfos[x].IsNullable)
                            {
                                sb.AppendLine($"   {propertyInfos[x].Name} = null{comma}");
                            }
                            else
                            {
                                sb.AppendLine($"   {propertyInfos[x].Name} = {propertyValue}{comma}");
                            }

                            
                        }

                        sb.AppendLine($"}};");
                    }
                    else
                    {
                        sb.AppendLine($"{myClass} {myClass.ToLower()} = new {myClass}() {{}};");
                    }

                    var rule = CompletionItemRules.Create(formatOnCommit: true);
                    var hint = CompletionItem.Create(sb.ToString(), properties: properties, rules: rule);

                    context.AddItem(hint);
                }
            }
        }

        private static ImmutableArray<ISymbol> GetAccessibleMembersInThisAndBaseTypes(ITypeSymbol containingType, bool isStatic, int position, SemanticModel model)
        {
            var types = GetBaseTypesAndThis(containingType);
            return types.SelectMany(x => x.GetMembers().Where(m => m.IsStatic == isStatic && model.IsAccessible(position, m)))
                        .ToImmutableArray();
        }

        private static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(ITypeSymbol type)
        {
            var current = type;

            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            return Task.FromResult(CompletionDescription.FromText(item.Properties[Description]));
        }

        public async override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            string tagValue = "";

            if (item.Properties.ContainsKey("Tag"))
            {
                tagValue = item.Properties["Tag"];               
            }

            if(!string.IsNullOrEmpty(tagValue) && tagValue == TagCategory.CLASS_TEMPLATE_ATTRIBUTE)
            {
                var textChange = new TextChange(new TextSpan(item.Span.Start - 1, 1), item.Properties[Snippet]);
                var result = CompletionChange.Create(textChange, includesCommitCharacter: true);
                return result;
            }
            else
            {

                // Get new text replacement and span.
                var receiver = item.Properties[Receiver];
                var newText = $"{receiver}.{item.DisplayText}";
                var newSpan = new TextSpan(item.Span.Start, 1);

                //var newStatement =  SyntaxFactory.ParseStatement(newText).NormalizeWhitespace();
                //var processedStatement = newStatement.WithAdditionalAnnotations(Formatter.Annotation);

                // Return the completion change with the new text change.
                var textChange = new TextChange(newSpan, newText);

                var result = CompletionChange.Create(textChange, includesCommitCharacter: true);

                // format any node with explicit formatter annotation
                //await Formatter.FormatAsync(document, Formatter.Annotation);

                //// format any elastic whitespace
                //await Formatter.FormatAsync(document, SyntaxAnnotation.ElasticAnnotation);

                return result;
            }
        }
    }

    public class ClassVirtualizationVisitor : CSharpSyntaxRewriter
    {
        public List<ClassDeclarationSyntax> classes { get; } = new List<ClassDeclarationSyntax>();

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

            // Only interested in class decorated with specific attribute
            if(node.AttributeLists.Count > 0)
            {
                // Get the attribute name
                foreach(var item in node.AttributeLists)
                {
                    AttributeSyntax attributeSyntax = item.Attributes.FirstOrDefault(x => x.Name.NormalizeWhitespace().ToFullString().ToUpper() == "CLASSTEMPLATE");

                    if(attributeSyntax != null)
                    {
                        classes.Add(node); // Save visited classes
                        break;
                    }
                }
            }

            return node;
        }
    }
}
