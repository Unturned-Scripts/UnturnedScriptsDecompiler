using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnturnedScriptsDecompiler.Extensions
{
    internal static class ReturnTypeExtensions
    {
        public static bool IsVoid(this AstType type)
        {
            return type is PrimitiveType primitive && primitive.KnownTypeCode == KnownTypeCode.Void;
        }
    }
}
