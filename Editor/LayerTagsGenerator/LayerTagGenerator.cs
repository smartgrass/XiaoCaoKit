using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
///源头: https://github.com/Thundernerd/Unity3D-LayersTagsGenerator
namespace XiaoCaoEditor
{
    public static class LayerTagGenerator
    {
        //生成目录
        public static string GenDirectory = Path.Combine(Application.dataPath, "XiaoCaoKit/Runtime/Modules/GamePlayModoule/LayerTag");

        [MenuItem(XCEditorTools.XiaoCaoGenCode + "Gen LayerTag", false, int.MaxValue)]
        private static void GenerateAll()
        {
            Type typeDefinition = typeof(ICodeGenerator);

            Assembly assembly = Assembly.GetExecutingAssembly();
            List<Type> types = assembly.GetTypes()
                .Where(x => x.GetInterfaces().Contains(typeDefinition))
                .ToList();

            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];
                if (type.IsAbstract)
                    continue;

                ICodeGenerator instance = (ICodeGenerator)Activator.CreateInstance(type);
                instance.Generate();
            }
        }

        public static string GetScreamName(string name)
        {
            string formattedName = "";

            name = FilterSpaces(name);

            for (int i = 0; i < name.Length; i++)
            {
                if (i == 0)
                {
                    formattedName += name[i].ToString();
                    continue;
                }

                char c = name[i];
                char pc = name[i - 1];
                if (char.IsUpper(c) && char.IsLower(pc))
                    formattedName += "_";

                formattedName += c.ToString();
            }

            return formattedName.ToUpper();
        }

        private static string FilterSpaces(string name)
        {
            int index = -1;

            while ((index = name.IndexOf(' ')) != -1)
            {
                if (index == name.Length - 1)
                {
                    name = name.Remove(index, 1);
                    return name;
                }

                string upperChar = char.ToUpper(name[index + 1]).ToString();
                name = name.Remove(index, 2);
                name = name.Insert(index, upperChar);
            }

            return name;
        }

        public static void GenerateToFile(CodeCompileUnit unit, string directory, string filename)
        {
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CodeGeneratorOptions options = new CodeGeneratorOptions
            {
                BracingStyle = "C"
            };

            StringWriter writer = new StringWriter();
            codeProvider.GenerateCodeFromCompileUnit(unit, writer, options);
            writer.Flush();
            string output = writer.ToString();

            string directoryPath = directory;
            string filePath = directoryPath + "/" + filename;
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            File.WriteAllText(filePath, output);
            Debug.Log($"gen {filename}");
            AssetDatabase.Refresh();
        }
    }


    public class LayerGenerator : ICodeGenerator
    {
        //[MenuItem("TNRD/Code Generation/Layers")]
        private static void Execute()
        {
            LayerGenerator generator = new LayerGenerator();
            generator.Generate();
        }

        public void Generate()
        {
            string[] layers = InternalEditorUtility.layers
                .OrderBy(x => x)
                .ToArray();

            CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
            CodeNamespace codeNamespace = new CodeNamespace();
            CodeTypeDeclaration classDeclaration = new CodeTypeDeclaration("Layers")
            {
                IsClass = true,
                TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed
            };

            for (int i = 0; i < layers.Length; i++)
            {
                string layer = layers[i];
                string layerName = LayerTagGenerator.GetScreamName(layer);
                string maskName = layerName + "_MASK";
                int layerValue = LayerMask.NameToLayer(layer);

                CodeMemberField layerField = new CodeMemberField(typeof(int), layerName)
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Const,
                    InitExpression = new CodePrimitiveExpression(layerValue)
                };

                CodeMemberField maskField = new CodeMemberField(typeof(int), maskName)
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Const,
                    InitExpression = new CodePrimitiveExpression(1 << layerValue)
                };

                classDeclaration.Members.Add(layerField);
                classDeclaration.Members.Add(maskField);
            }

            CodeCommentStatement comment = new CodeCommentStatement($"Gen By \"{typeof(LayerTagGenerator)}\"", true);
            classDeclaration.Comments.Add(comment);
            codeNamespace.Types.Add(classDeclaration);
            codeCompileUnit.Namespaces.Add(codeNamespace);

            LayerTagGenerator.GenerateToFile(codeCompileUnit, LayerTagGenerator.GenDirectory, "Layers.cs");
        }
    }


    public class TagGenerator : ICodeGenerator
    {
        //[MenuItem("TNRD/Code Generation/Tags")]
        private static void Execute()
        {
            TagGenerator generator = new TagGenerator();
            generator.Generate();
        }

        public void Generate()
        {
            string[] tags = InternalEditorUtility.tags
                .OrderBy(x => x)
                .ToArray();

            CodeCompileUnit compileUnit = new CodeCompileUnit();
            CodeNamespace codeNamespace = new CodeNamespace();

            CodeTypeDeclaration classDeclaration = new CodeTypeDeclaration("Tags")
            {
                IsClass = true,
                TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed
            };

            foreach (string tag in tags)
            {
                CodeMemberField field = new CodeMemberField
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Const,
                    Name = LayerTagGenerator.GetScreamName(tag),
                    Type = new CodeTypeReference(typeof(string)),
                    InitExpression = new CodePrimitiveExpression(tag)
                };
                classDeclaration.Members.Add(field);
            }
            CodeCommentStatement comment = new CodeCommentStatement($"Gen By \"{typeof(LayerTagGenerator)}\"", true);
            classDeclaration.Comments.Add(comment);
            codeNamespace.Types.Add(classDeclaration);
            compileUnit.Namespaces.Add(codeNamespace);

            LayerTagGenerator.GenerateToFile(compileUnit, LayerTagGenerator.GenDirectory, "Tags.cs");
        }
    }

    public interface ICodeGenerator
    {
        void Generate();
    }
}
