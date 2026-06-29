using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UnturnedScriptsDecompiler.Extensions;

namespace UnturnedScriptsDecompiler.Services
{
    internal class ScriptDecompiler
    {
        private static readonly string[] ProjectUnityPackageScripts =
        [
            "SDG.Unturned.ActivationEventHook",
            "SDG.Unturned.AirdropSpawner",
            "SDG.Unturned.BarricadeDestroyerComponent",
            "SDG.Unturned.BarricadeSpawner",
            "SDG.Unturned.BinaryRandomComponent",
            "SDG.Unturned.ClientTextChatMessenger",
            "SDG.Unturned.CollisionDamage",
            "SDG.Unturned.CollisionEventHook",
            "SDG.Unturned.CollisionTeleporter",
            "SDG.Unturned.CommentComponent",
            "SDG.Unturned.CraftingTagModifierComponent",
            "SDG.Unturned.CraftingTagProviderComponent",
            "SDG.Unturned.CustomWeatherEventHook",
            "SDG.Unturned.DestroyEventHook",
            "SDG.Unturned.EffectSpawner",
            "SDG.Unturned.EnableDopplerEffect",
            "SDG.Unturned.ExplosionSpawner",
            "SDG.Unturned.FallDamageOverride",
            "SDG.Unturned.GunAttachmentEventHook",
            "SDG.Unturned.InteractableObjectQuestEventHook",
            "SDG.Unturned.InteractableObjectBinaryStateEventHook",
            "SDG.Unturned.ItemSpawner",
            "SDG.Unturned.LODGroupAdditionalData",
            "SDG.Unturned.LogMessengerComponent",
            "SDG.Unturned.MobAlertSpawner",
            "SDG.Unturned.MusicAudioSource",
            "SDG.Unturned.NpcGlobalEventHook",
            "SDG.Unturned.NpcGlobalEventMessenger",
            "SDG.Unturned.ParticleSystemCollisionAudio",
            "SDG.Unturned.RepeatComponent",
            "SDG.Unturned.ServerTextChatMessenger",
            "SDG.Unturned.TextChatEventHook",
            "SDG.Unturned.TimerEventHook",
            "SDG.Unturned.UseableEventHook",
            "SDG.Unturned.UseableGunEventHook",
            "SDG.Unturned.VehicleEventHook",
            "SDG.Unturned.VehicleGearshiftEventHook",
            "SDG.Unturned.VehicleHealthEventHook",
            "SDG.Unturned.VehicleSpawner",
            "SDG.Unturned.VehicleTurretEventHook",
            "SDG.Unturned.WeatherEventHook",
            "SDG.Unturned.LookAtLocalPlayer",
            "SDG.Unturned.EngineCurvesComponent"
        ];

        private static readonly string[] ProjectUnityPackageDependencies =
        [
            "SDG.Unturned.ENPCLogicType",
            "SDG.Unturned.EDeathCause",
            "SDG.Unturned.EExplosionDamageType"
        ];

        private static readonly string[] IgnoreTwoStepInitialization =
        [
            "init",
            "initialize",
            "setup",
            "customStart",
            "awake",
            "updateAttachments",
            "updateGun",
            "updateState"
        ];

        private const float IgnoreStaticOnlyScript = 0.5f;

        private static readonly string[] IgnoreScripts =
        [
            "SDG.Unturned.LevelObjectRefComponent",
            "SDG.Unturned.BarricadeRefComponent",
            "SDG.Unturned.StructureRefComponent",
            "SDG.Unturned.TreeRefComponent",
            "SDG.Unturned.WeatherComponentBase",
            "SDG.Unturned.TempNodeBase",
            "SDG.Unturned.CustomWeatherComponent",
            "SDG.Unturned.LightningWeatherComponent",
            "SDG.Unturned.MenuStartup",
            "SDG.Unturned.MenuOverridableObjects",
            "SDG.Unturned.MenuSurvivorsClothing",
            "SDG.Unturned.Setup",
            "SDG.Unturned.EditorMovement",
            "SDG.Unturned.EditorArea",
            "SDG.Unturned.RuntimeGizmos",
            "SDG.Unturned.Player",
            "SDG.Unturned.DevkitHierarchyWorldObject",
            "SDG.Unturned.PoolReference",
            "SDG.Unturned.LevelVolume",
            "SDG.Unturned.HumanAnimator",
            "SDG.Unturned.Interactable2HP",
            "LegacyAIPathNoRedist"
        ];

        private static readonly string[] IgnoreScriptInheritance =
        [
            "SDG.Unturned.SteamCaller",
            "SDG.Unturned.GlazierBase",
            "SDG.Unturned.Interactable",
            "SDG.Unturned.Interactable2"
        ];

        private static readonly string[] KeepScriptInheritance =
        [
            "SDG.Unturned.IExplodableThrowable"
        ];

        private struct ScriptCategory(string category, string fullName)
        {
            public string Category = category;
            public string FullName = fullName;
        }

        private static readonly ScriptCategory[] ScriptCategories =
        [
            new("Volumes", "SDG.Unturned.LevelVolume"),
            new("Nodes", "SDG.Unturned.TempNodeBase"),
        ];

        private struct ScriptComment(string fullName, string comment)
        {
            public string FullName = fullName;
            public string Comment = comment;
        }

        private static readonly ScriptComment[] ScriptComments =
        [
            new("SDG.Unturned.LevelVolume",
                "Volumes use the Scale of the GameObject as the Size for the Volume, No Colliders Required but you can still use one by Assigning your Collider to the volumeCollider field\n" +
                "This Volume Script should be attached to an Empty GameObject with Tag and Layer :: Trap\n" +
                "To Visualize the Size of the Volume Temporarily Attach either a BoxCollider or a SphereCollider. Making sure that the Collider Component uses the default Radius ( 0.5 ) or Size ( 1 1 1 )"),
            new("SDG.Unturned.BuiltinAutoShutdown", "Will Shutdown the Server when Enabled\nIf the Server has disabled Update Shutdowns you have to set isScheduledShutdownEnabled to true"),
        ];

        private static readonly string[] UnityMethods =
        [
            "Awake",
            "Start",
            "Update",
            "FixedUpdate",
            "LateUpdate",
            "OnEnable",
            "OnDisable",
            "OnDestroy",
            "OnTriggerEnter"
        ];

        private static readonly DecompilerSettings Settings = new(LanguageVersion.CSharp8_0)
        {
            ThrowOnAssemblyResolveErrors = true,
            RemoveDeadCode = true,
            RemoveDeadStores = true,
            UseNestedDirectoriesForNamespaces = false,
            FileScopedNamespaces = false,
            ShowXmlDocumentation = false,
            CSharpFormattingOptions =
            {
                IndentationString = new string(' ', 4)
            }
        };

        private string OutputPath { get; }

        public ScriptDecompiler(string outputPath) 
        {
            OutputPath = outputPath;
        }

        public void DecompileScripts(string filePath)
        {
            Stopwatch sw = Stopwatch.StartNew();
            using var module = new PEFile(filePath);

            UniversalAssemblyResolver resolver = new(filePath, true, module.DetectTargetFrameworkId());
            resolver.AddSearchDirectory(Path.GetDirectoryName(filePath));

            DecompilerTypeSystem typeSystem = new(module, resolver, Settings);

            Parallel.ForEach(Partitioner.Create(typeSystem.MainModule.TypeDefinitions.Where(IsUnturnedScript).ToList(), loadBalance: true), (t, i) =>
            {
                DecompileScriptType(t, typeSystem);
            });
            Console.WriteLine($"Decompile Complete {sw.Elapsed.TotalSeconds:N1}s");
        }

        private bool IsUnturnedScript(ITypeDefinition type)
        {
            if (IgnoreScripts.Contains(type.FullName)) return false;
            if (ProjectUnityPackageScripts.Contains(type.FullName)) return false;

            foreach (var member in type.Members)
            {
                if (member.SymbolKind != SymbolKind.Method) continue;
                if (IgnoreTwoStepInitialization.Contains(member.Name)) return false;
                // Has public static Get() Instance
                if (member.IsStatic && member.Accessibility == Accessibility.Public && member.ReturnType.FullName == type.FullName) return false;
            }

            bool isMonoBehaviour = false;
            foreach (var baseType in type.GetAllBaseTypes())
            {
                if (IgnoreScriptInheritance.Contains(baseType.FullName)) return false;
                isMonoBehaviour |= baseType.FullName == "UnityEngine.MonoBehaviour";
            }

            return isMonoBehaviour;
        }

        private void DecompileScriptType(ITypeDefinition type, IDecompilerTypeSystem typeSystem)
        {
            CSharpDecompiler decompiler = new(typeSystem, Settings);
            SyntaxTree tree = decompiler.DecompileType(type.FullTypeName);

            if (IsStaticOnlyScript(type, tree)) return;

            List<EntityDeclaration> scriptMembers = [.. GetPublicScriptMembers(tree, type)];
            
            List<ITypeDefinition> baseTypes = [.. type.GetAllBaseTypeDefinitions().Reverse().Skip(1)];
            List<EntityDeclaration> baseMembers = [];
            foreach (var baseType in baseTypes)
            {
                if (baseType.Namespace.StartsWith("UnityEngine", StringComparison.Ordinal)) break;
                if (baseType.Kind == TypeKind.Interface) continue;

                SyntaxTree baseTree = decompiler.DecompileType(baseType.FullTypeName);
                baseMembers.AddRange(GetPublicInheritedScriptMembers(baseTree, baseType));
            }

            if (scriptMembers.Count == 0 && baseMembers.Count == 0) return;

            GetOutputPaths(type, baseTypes, out string dependenciesPath, out string? categoryPath);
            if (!string.IsNullOrEmpty(categoryPath))
                Directory.CreateDirectory(Path.Combine(OutputPath, categoryPath));

            #region UsingDeclarations
            bool hasUsingUnityEngine = false;
            foreach (var usingDeclaration in tree.Children.OfType<UsingDeclaration>())
            {
                if (!usingDeclaration.Namespace.StartsWith("System", StringComparison.Ordinal) &&
                    !usingDeclaration.Namespace.StartsWith("UnityEngine", StringComparison.Ordinal) &&
                    !usingDeclaration.Namespace.Equals("TMPro", StringComparison.Ordinal) &&
                    !usingDeclaration.Namespace.Equals("SDG.Unturned", StringComparison.Ordinal))
                    usingDeclaration.Remove();

                hasUsingUnityEngine |= usingDeclaration.Namespace.Equals("UnityEngine", StringComparison.Ordinal);
            }

            if (!hasUsingUnityEngine)
            {
                tree.InsertChildBefore(tree.FirstChild, new UsingDeclaration("UnityEngine"), SyntaxTree.MemberRole);
            }
            #endregion

            TypeDeclaration typeDeclaration = tree.Descendants.OfType<TypeDeclaration>().First(t => t.Name == type.Name);

            #region Inheritance
            typeDeclaration.BaseTypes.Clear();
            // Add First UnityEngine Type instead of MonoBehaviour to Support Scripts inheriting from UnityEngine.UI.Button for Example
            typeDeclaration.BaseTypes.Add(new SimpleType(baseTypes.First(t => t.Namespace.StartsWith("UnityEngine")).Name));
            foreach (var baseInterface in baseTypes)
            {
                if (baseInterface.Kind != TypeKind.Interface) continue;
                if (!KeepScriptInheritance.Contains(baseInterface.FullName)) continue;

                typeDeclaration.BaseTypes.Add(new SimpleType(baseInterface.Name));
                Directory.CreateDirectory(Path.Combine(OutputPath, dependenciesPath));
                ExportSyntaxTree(baseInterface.Name, decompiler.DecompileType(baseInterface.FullTypeName), dependenciesPath);
            }
            #endregion

            #region Script Comments
            foreach (string comment in ScriptComments.Where(s => s.FullName == type.FullName || baseTypes.Any(t => t.FullName == s.FullName)).Select(s => s.Comment))
            {
                string[] scriptComments = comment.Split('\n', StringSplitOptions.TrimEntries);
                for (int i = 0; i < scriptComments.Length; i++)
                    scriptComments[i] = scriptComments[i].Insert(0, " ");

                Comment previousComment = new(scriptComments[0]);
                typeDeclaration.Parent!.InsertChildBefore(typeDeclaration, previousComment, Roles.Comment);
                for (int i = 1; i < scriptComments.Length; i++)
                {
                    Comment currentComment = new(scriptComments[i]);
                    typeDeclaration.Parent!.InsertChildAfter(previousComment, currentComment, Roles.Comment);
                    previousComment = currentComment;
                }
            }
            #endregion

            #region AddComponentMenu Attribute
            var attribute = new ICSharpCode.Decompiler.CSharp.Syntax.Attribute()
            {
                Type = new SimpleType("AddComponentMenu")
            };
            attribute.Arguments.Add(new PrimitiveExpression("Unturned Scripts/" + (categoryPath != null ? categoryPath + '/' : string.Empty) + TypeFormatter.FormatTypeName(type.Name), LiteralFormat.StringLiteral));

            var attributeSection = new AttributeSection(attribute);
            typeDeclaration.Parent!.InsertChildBefore(typeDeclaration, attributeSection, SyntaxTree.MemberRole);
            #endregion

            #region Members
            typeDeclaration.Members.Clear();
            foreach (var member in scriptMembers)
            {
                // Do not add Unity Methods, They are included in Members to prevent Scripts with Only Private Unity Methods from being ignored
                if (member.SymbolKind == SymbolKind.Method && UnityMethods.Contains(member.Name)) continue;

                typeDeclaration.Members.Add((EntityDeclaration)member.Clone());
                if (member is FieldDeclaration field && IsExportableFieldType(field, type.Members, out ITypeDefinition? fieldType))
                {
                    Directory.CreateDirectory(Path.Combine(OutputPath, dependenciesPath));
                    ExportSyntaxTree(fieldType.Name, decompiler.DecompileType(fieldType.FullTypeName), dependenciesPath);
                }
            }

            bool addedComment = false;
            foreach (var member in baseMembers)
            {
                if (member.SymbolKind == SymbolKind.Method && UnityMethods.Contains(member.Name)) continue;
                
                if (!addedComment)
                {
                    typeDeclaration.AddChild(new Comment(" --- Inherited Members ---\n"), Roles.Comment);
                    addedComment = true;
                }
                typeDeclaration.Members.Add((EntityDeclaration)member.Clone());
                if (member is FieldDeclaration field && IsExportableFieldType(field, baseTypes.SelectMany(t => t.Members), out ITypeDefinition? fieldType))
                {
                    Directory.CreateDirectory(Path.Combine(OutputPath, dependenciesPath));
                    ExportSyntaxTree(fieldType.Name, decompiler.DecompileType(fieldType.FullTypeName), dependenciesPath);
                }
            }
            #endregion

            ExportSyntaxTree(type.Name, tree, categoryPath);
            Console.WriteLine($"Decompiled {(categoryPath != null ? categoryPath + '/' : string.Empty)}{type.Name} : {scriptMembers.Count} + {baseMembers.Count} Members");
        }

        private static bool IsStaticOnlyScript(ITypeDefinition type, SyntaxTree tree)
        {
            var members = tree.Descendants.OfType<TypeDeclaration>().First(t => t.Name == type.Name).Members;
            return members.Count > 1 && members.Count(m => m.HasModifier(Modifiers.Static) && !m.HasModifier(Modifiers.Const) && !m.HasModifier(Modifiers.Readonly)) >= members.Count * IgnoreStaticOnlyScript;
        }

        private static void GetOutputPaths(ITypeDefinition type, List<ITypeDefinition> baseTypes, out string dependenciesPath, out string? categoryPath)
        {
            dependenciesPath = "Dependencies";

            categoryPath = ScriptCategories.FirstOrDefault(c => c.FullName == type.FullName).Category;
            if (string.IsNullOrEmpty(categoryPath)) categoryPath = ScriptCategories.FirstOrDefault(c => baseTypes.Any(t => t.FullName == c.FullName)).Category;

            if (!string.IsNullOrEmpty(categoryPath))
                dependenciesPath = Path.Combine(categoryPath, dependenciesPath);
        }

        private static bool IsExportableFieldType(FieldDeclaration field, IEnumerable<IMember> members, [NotNullWhen(true)][MaybeNullWhen(false)] out ITypeDefinition? fieldType)
        {
            fieldType = members.First(f => f.Name == field.Variables.First().Name).ReturnType.GetDefinition();
            if (fieldType == null) return false;
            if (fieldType.Kind != TypeKind.Interface && fieldType.Kind != TypeKind.Enum) return false;
            if (fieldType.DeclaringTypeDefinition != null) return false;
            if (ProjectUnityPackageDependencies.Contains(fieldType.FullName)) return false;
            if (IsNonUnturnedValidType(fieldType)) return false;

            return true;
        }

        private void ExportSyntaxTree(string fileName, SyntaxTree tree, string? categoryPath)
        {
            try
            {
                string path;
                if (!string.IsNullOrEmpty(categoryPath)) path = Path.Combine(OutputPath, categoryPath, fileName + ".cs");
                else path = Path.Combine(OutputPath, fileName + ".cs");

                using var exportWriter = new StreamWriter(path);
                tree.AcceptVisitor(new CSharpOutputVisitor(exportWriter, Settings.CSharpFormattingOptions));
            }
            catch (IOException) { }
        }

        private static IEnumerable<EntityDeclaration> GetPublicScriptMembers(SyntaxTree syntaxTree, ITypeDefinition type)
        {
            return syntaxTree.Descendants.OfType<TypeDeclaration>().First(t => t.Name == type.Name).Members.Where(m => 
            {
                switch (m)
                {
                    case TypeDeclaration enumType:
                        return enumType.ClassType == ClassType.Enum;
                    case FieldDeclaration field:
                        return ValidField(field) && ValidType(type.Members.First(f => f.Name == field.Variables.First().Name).ReturnType);
                    case MethodDeclaration method:
                        method.Body = new BlockStatement();
                        return ValidMethod(method);
                    default:
                        return false;
                }
            });
        }

        private static IEnumerable<EntityDeclaration> GetPublicInheritedScriptMembers(SyntaxTree syntaxTree, ITypeDefinition type)
        {
            return syntaxTree.Descendants.OfType<TypeDeclaration>().First(t => t.Name == type.Name).Members.Where(m =>
            {
                switch (m)
                {
                    case FieldDeclaration field:
                        return ValidField(field) && ValidType(type.Members.First(f => f.Name == field.Variables.First().Name).ReturnType);
                    case MethodDeclaration method:
                        method.Body = new BlockStatement();
                        return UnityMethods.Contains(method.Name);
                    default:
                        return false;
                }
            });
        }

        private static bool ValidField(FieldDeclaration field)
        {
            return  !field.HasModifier(Modifiers.Static) &&
                    !field.HasModifier(Modifiers.Readonly) &&
                    !field.HasModifier(Modifiers.Const) &&
                    (field.HasModifier(Modifiers.Public) || HasSerializeFieldAttribute(field.Attributes));
        }

        private static bool IsNonUnturnedValidType(IType type)
        {
            return type is PrimitiveType || 
                type.Namespace.StartsWith("UnityEngine", StringComparison.Ordinal) ||
                type.Namespace.Equals("TMPro", StringComparison.Ordinal) ||
                type.Namespace.StartsWith("System", StringComparison.Ordinal);
        }

        private static bool ValidType(IType type)
        {
            return type.Kind == TypeKind.Interface || type.Kind == TypeKind.Enum || IsNonUnturnedValidType(type);
        }

        private static bool HasSerializeFieldAttribute(AstNodeCollection<AttributeSection> attributeSection)
        {
            if (attributeSection.Count == 0) return false;
            return attributeSection
                .SelectMany(static a => a.Attributes)
                .SelectMany(static a => a.Descendants)
                .Any(a => a is Identifier identifier && identifier.Name == "SerializeField");
        }

        private static bool ValidMethod(MethodDeclaration method)
        {
            return  (method.HasModifier(Modifiers.Public) || UnityMethods.Contains(method.Name)) &&
                    !method.HasModifier(Modifiers.Static) && !method.HasModifier(Modifiers.Override) && method.ReturnType.IsVoid() &&
                    (method.Parameters.Count == 0 || method.Parameters.Count == 1 && method.Parameters.First().Type is PrimitiveType);
        }
    }
}
