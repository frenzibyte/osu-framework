// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Emitters
{
    public class DependenciesFileEmitter
    {
        public const string REGISTRY_PARAMETER_NAME = "registry";

        public const string IS_REGISTERED_METHOD_NAME = "IsRegistered";
        public const string REGISTER_FOR_DEPENDENCY_ACTIVATION_METHOD_NAME = "RegisterForDependencyActivation";
        public const string REGISTER_METHOD_NAME = "Register";

        public const string TARGET_PARAMETER_NAME = "t";
        public const string DEPENDENCIES_PARAMETER_NAME = "d";
        public const string CACHE_INFO_PARAMETER_NAME = "i";

        public const string LOCAL_DEPENDENCIES_VAR_NAME = "dependencies";

        private const string headers = @"// <auto-generated/>
#nullable enable
#pragma warning disable CS4014

";

        public readonly GeneratorClassCandidate Candidate;
        // public readonly INamedTypeSymbol ClassType;

        public DependenciesFileEmitter(GeneratorClassCandidate candidate)
        {
            Candidate = candidate;
        }

        public void Emit(AddSourceDelegate addSource)
        {
            if (!Candidate.IsValid)
                return;

            StringBuilder result = new StringBuilder();
            result.Append(headers);

            if (Candidate.ContainingNamespace == null)
            {
                result.Append(
                    emitDependenciesClass().NormalizeWhitespace());
            }
            else
            {
                result.Append(
                    SyntaxFactory.NamespaceDeclaration(
                                     SyntaxFactory.IdentifierName(Candidate.ContainingNamespace))
                                 .WithMembers(
                                     SyntaxFactory.SingletonList(
                                         emitDependenciesClass()))
                                 .NormalizeWhitespace());
            }

            // Fully qualified name, with generics replaced with friendly characters.
            string typeName = Candidate.FullyQualifiedTypeName.Replace('<', '{').Replace('>', '}');
            string filename = $"g_{typeName}_Dependencies.cs";

            addSource(filename, result.ToString());
        }

        private MemberDeclarationSyntax emitDependenciesClass()
        {
            return emitTypeTree(
                cls =>
                    cls.WithBaseList(
                           SyntaxFactory.BaseList(
                               SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                                   SyntaxFactory.SimpleBaseType(
                                       SyntaxFactory.ParseTypeName("global::osu.Framework.Allocation.ISourceGeneratedDependencyActivator")))))
                       .WithMembers(
                           SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                               SyntaxFactory.MethodDeclaration(
                                                SyntaxFactory.PredefinedType(
                                                    SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                                                REGISTER_FOR_DEPENDENCY_ACTIVATION_METHOD_NAME)
                                            .WithModifiers(
                                                emitMethodModifiers())
                                            .WithParameterList(
                                                SyntaxFactory.ParameterList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Parameter(
                                                                         SyntaxFactory.Identifier(REGISTRY_PARAMETER_NAME))
                                                                     .WithType(
                                                                         SyntaxFactory.ParseTypeName("global::osu.Framework.Allocation.IDependencyActivatorRegistry")))))
                                            .WithBody(
                                                SyntaxFactory.Block(
                                                    emitPrecondition(),
                                                    emitBaseCall(),
                                                    emitRegistration())))));
        }

        private ClassDeclarationSyntax emitTypeTree(Func<ClassDeclarationSyntax, ClassDeclarationSyntax> innerClassAction)
        {
            List<ClassDeclarationSyntax> classes = new List<ClassDeclarationSyntax>();

            foreach (string type in Candidate.TypeHierarchy)
                classes.Add(createClassSyntax(type));

            classes[0] = innerClassAction(classes[0]);

            for (int i = 0; i < classes.Count - 1; i++)
                classes[i + 1] = classes[i + 1].WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new[] { classes[i] }));

            return classes.Last();

            static ClassDeclarationSyntax createClassSyntax(string type) =>
                SyntaxFactory.ClassDeclaration(type)
                             .WithModifiers(
                                 SyntaxTokenList.Create(
                                     SyntaxFactory.Token(SyntaxKind.PartialKeyword)));
        }

        private SyntaxTokenList emitMethodModifiers()
        {
            if (Candidate.NeedsOverride)
            {
                return SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
            }

            return SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.VirtualKeyword));
        }

        private StatementSyntax emitPrecondition()
        {
            return SyntaxFactory.IfStatement(
                SyntaxFactory.InvocationExpression(
                                 SyntaxFactory.MemberAccessExpression(
                                     SyntaxKind.SimpleMemberAccessExpression,
                                     SyntaxFactory.IdentifierName(REGISTRY_PARAMETER_NAME),
                                     SyntaxFactory.IdentifierName(IS_REGISTERED_METHOD_NAME)))
                             .WithArgumentList(
                                 SyntaxFactory.ArgumentList(
                                     SyntaxFactory.SingletonSeparatedList(
                                         SyntaxFactory.Argument(
                                             SyntaxFactory.TypeOfExpression(
                                                 SyntaxFactory.ParseTypeName(Candidate.GlobalPrefixedTypeName)))))),
                SyntaxFactory.ReturnStatement());
        }

        private StatementSyntax emitBaseCall()
        {
            if (!Candidate.NeedsOverride)
                return SyntaxFactory.ParseStatement(string.Empty);

            return SyntaxFactory.ExpressionStatement(
                                    SyntaxFactory.InvocationExpression(
                                                     SyntaxFactory.MemberAccessExpression(
                                                         SyntaxKind.SimpleMemberAccessExpression,
                                                         SyntaxFactory.BaseExpression(),
                                                         SyntaxFactory.IdentifierName(REGISTER_FOR_DEPENDENCY_ACTIVATION_METHOD_NAME)))
                                                 .WithArgumentList(
                                                     SyntaxFactory.ArgumentList(
                                                         SyntaxFactory.SingletonSeparatedList(
                                                             SyntaxFactory.Argument(
                                                                 SyntaxFactory.IdentifierName(REGISTRY_PARAMETER_NAME))))))
                                .WithLeadingTrivia(
                                    SyntaxFactory.TriviaList(
                                        SyntaxFactory.LineFeed));
        }

        private StatementSyntax emitRegistration()
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                                 SyntaxFactory.MemberAccessExpression(
                                     SyntaxKind.SimpleMemberAccessExpression,
                                     SyntaxFactory.IdentifierName(REGISTRY_PARAMETER_NAME),
                                     SyntaxFactory.IdentifierName(REGISTER_METHOD_NAME)))
                             .WithArgumentList(
                                 SyntaxFactory.ArgumentList(
                                     SyntaxFactory.SeparatedList(new[]
                                     {
                                         SyntaxFactory.Argument(
                                             SyntaxFactory.TypeOfExpression(
                                                 SyntaxFactory.ParseTypeName(Candidate.GlobalPrefixedTypeName))),
                                         SyntaxFactory.Argument(
                                             emitInjectDependenciesDelegate()),
                                         SyntaxFactory.Argument(
                                             emitCacheDependenciesDelegate()),
                                         SyntaxFactory.Argument(
                                             emitBindBindablesDelegate())
                                     }))));
        }

        private ExpressionSyntax emitInjectDependenciesDelegate()
        {
            if (Candidate.DependencyLoaderMembers.Count == 0 && Candidate.ResolvedMembers.Count == 0)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

            return SyntaxFactory.ParenthesizedLambdaExpression()
                                .WithParameterList(
                                    SyntaxFactory.ParameterList(
                                        SyntaxFactory.SeparatedList(new[]
                                        {
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier(TARGET_PARAMETER_NAME)),
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier(DEPENDENCIES_PARAMETER_NAME)),
                                        })))
                                .WithBlock(
                                    SyntaxFactory.Block(
                                        Candidate.ResolvedMembers.Select(m => (IStatementEmitter)new ResolvedMemberEmitter(this, m))
                                                 .Concat(
                                                     Candidate.DependencyLoaderMembers.Select(m => new BackgroundDependencyLoaderEmitter(this, m)))
                                                 .SelectMany(
                                                     e => e.Emit())));
        }

        private ExpressionSyntax emitCacheDependenciesDelegate()
        {
            if (Candidate.CachedMembers.Count == 0 && Candidate.CachedInterfaces.Count == 0 && Candidate.CachedClasses.Count == 0)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

            return SyntaxFactory.ParenthesizedLambdaExpression()
                                .WithParameterList(
                                    SyntaxFactory.ParameterList(
                                        SyntaxFactory.SeparatedList(new[]
                                        {
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier(TARGET_PARAMETER_NAME)),
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier(DEPENDENCIES_PARAMETER_NAME)),
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier(CACHE_INFO_PARAMETER_NAME))
                                        })))
                                .WithBlock(
                                    SyntaxFactory.Block(
                                        Candidate.CachedMembers.Select(m => (IStatementEmitter)new CachedMemberEmitter(this, m))
                                                 .Concat(
                                                     Candidate.CachedClasses.Select(m => new CachedClassEmitter(this, m)))
                                                 .Concat(
                                                     Candidate.CachedInterfaces.Select(m => new CachedInterfaceEmitter(this, m)))
                                                 .SelectMany(
                                                     e => e.Emit())
                                                 .Prepend(createPrologue())
                                                 .Append(createEpilogue())));

            static StatementSyntax createPrologue() =>
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("var"))
                                 .WithVariables(
                                     SyntaxFactory.SingletonSeparatedList(
                                         SyntaxFactory.VariableDeclarator(
                                                          SyntaxFactory.Identifier(LOCAL_DEPENDENCIES_VAR_NAME))
                                                      .WithInitializer(
                                                          SyntaxFactory.EqualsValueClause(
                                                              SyntaxFactory.ObjectCreationExpression(
                                                                               SyntaxFactory.ParseTypeName("global::osu.Framework.Allocation.DependencyContainer"))
                                                                           .WithArgumentList(
                                                                               SyntaxFactory.ArgumentList(
                                                                                   SyntaxFactory.SingletonSeparatedList(
                                                                                       SyntaxFactory.Argument(
                                                                                           SyntaxFactory.IdentifierName(DEPENDENCIES_PARAMETER_NAME))))))))));

            static StatementSyntax createEpilogue() =>
                SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(LOCAL_DEPENDENCIES_VAR_NAME));
        }

        private ExpressionSyntax emitBindBindablesDelegate()
        {
            if (!Candidate.ResolvedMembers.Any(r => r.IsBindable))
                return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

            return SyntaxFactory.ParenthesizedLambdaExpression()
                                .WithParameterList(
                                    SyntaxFactory.ParameterList(
                                        SyntaxFactory.SeparatedList(new[]
                                        {
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier(TARGET_PARAMETER_NAME)),
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier(DEPENDENCIES_PARAMETER_NAME)),
                                        })))
                                .WithBlock(
                                    SyntaxFactory.Block(
                                        Candidate.ResolvedMembers.Where(r => r.IsBindable)
                                                 .Select(m => (IStatementEmitter)new BindableBindingEmitter(this, m))
                                                 .SelectMany(
                                                     e => e.Emit())));
        }
    }

    public delegate void AddSourceDelegate(string filename, string sourceText);
}
