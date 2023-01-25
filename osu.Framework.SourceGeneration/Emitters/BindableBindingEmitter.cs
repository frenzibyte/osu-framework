// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osu.Framework.SourceGeneration.Data;

namespace osu.Framework.SourceGeneration.Emitters
{
    public class BindableBindingEmitter : IStatementEmitter
    {
        private readonly DependenciesFileEmitter fileEmitter;
        private readonly ResolvedAttributeData data;

        public BindableBindingEmitter(DependenciesFileEmitter fileEmitter, ResolvedAttributeData data)
        {
            this.fileEmitter = fileEmitter;
            this.data = data;
        }

        public IEnumerable<StatementSyntax> Emit()
        {
            ExpressionSyntax bindMethodExpression = data.CanBeNull
                ? SyntaxFactory.ConditionalAccessExpression(createMemberAccessor(), SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName("BindTo")))
                : SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, createMemberAccessor(), SyntaxFactory.IdentifierName("BindTo"));

            yield return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    bindMethodExpression,
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Argument(SyntaxHelpers.GetBindableSourceInvocation(
                                data.GlobalPrefixedTypeName,
                                data.CachedName,
                                data.GlobalPrefixedParentTypeName
                            ))
                        }))));
        }

        private ExpressionSyntax createMemberAccessor()
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.ParseTypeName(fileEmitter.Candidate.GlobalPrefixedTypeName),
                        SyntaxFactory.IdentifierName(DependenciesFileEmitter.TARGET_PARAMETER_NAME))),
                SyntaxFactory.IdentifierName(data.PropertyName));
        }
    }
}
